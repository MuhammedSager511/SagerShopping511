using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NToastNotify;
using System.Security.Claims;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IToastNotification toast;
        private readonly ICurrencyService _currency;
        private readonly IAppLocalizer _localizer;
        private readonly IIyzipayCheckoutService _iyzipay;
        private readonly IShippingService _shipping;
        private readonly IOrderNotificationService _notifications;
        private readonly IPayPalCheckoutService _paypal;
        private readonly IMemoryCache _cache;

        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; } = new();

        [BindProperty]
        public bool AcceptTerms { get; set; }

        public CartController(
            ApplicationDbContext db,
            IToastNotification toast,
            ICurrencyService currency,
            IAppLocalizer localizer,
            IIyzipayCheckoutService iyzipay,
            IShippingService shipping,
            IOrderNotificationService notifications,
            IPayPalCheckoutService paypal,
            IMemoryCache cache)
        {
            this.db = db;
            this.toast = toast;
            _currency = currency;
            _localizer = localizer;
            _iyzipay = iyzipay;
            _shipping = shipping;
            _notifications = notifications;
            _paypal = paypal;
            _cache = cache;
        }

        public IActionResult Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var listCart = db.ShoppingCarts
                .AsNoTracking()
                .Where(i => i.ApplicationUserId == userId)
                .Include(i => i.Product)
                .ToList();

            ShoppingCartVM = new ShoppingCartVM
            {
                OrderHeader = new OrderHeader
                {
                    orderTotal = listCart.Sum(i => i.Count * PriceHelper.EffectiveUnitPrice(i.Product)),
                    ApplicationUser = db.Users.AsNoTracking().FirstOrDefault(i => i.Id == userId)
                },
                ListCart = listCart
            };

            HttpContext.Session.SetInt32(Diger.ssShoppingCart, listCart.Count);
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Add(int cartId)
        {
            var cart = GetOwnedCart(cartId);
            if (cart == null) return RedirectToAction(nameof(Index));

            if (cart.Product == null || cart.Product.StockQuantity <= 0)
            {
                toast.AddWarningToastMessage(_localizer["ToastProductUnavailable"]);
                return RedirectToAction(nameof(Index));
            }

            if (cart.Count + 1 > cart.Product.StockQuantity)
            {
                toast.AddWarningToastMessage(_localizer["ToastInsufficientStock"]);
                return RedirectToAction(nameof(Index));
            }

            cart.Count += 1;
            db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Decrease(int cartId)
        {
            var cart = GetOwnedCart(cartId);
            if (cart == null) return RedirectToAction(nameof(Index));

            var userId = cart.ApplicationUserId;
            if (cart.Count <= 1)
                db.ShoppingCarts.Remove(cart);
            else
                cart.Count -= 1;

            db.SaveChanges();
            HttpContext.Session.SetInt32(Diger.ssShoppingCart, db.ShoppingCarts.Count(i => i.ApplicationUserId == userId));
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int cartId)
        {
            var cart = GetOwnedCart(cartId);
            if (cart == null) return RedirectToAction(nameof(Index));

            var userId = cart.ApplicationUserId;
            db.ShoppingCarts.Remove(cart);
            db.SaveChanges();
            HttpContext.Session.SetInt32(Diger.ssShoppingCart, db.ShoppingCarts.Count(i => i.ApplicationUserId == userId));
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            var user = db.Users.AsNoTracking().FirstOrDefault(i => i.Id == userId);
            var listCart = db.ShoppingCarts
                .Where(i => i.ApplicationUserId == userId)
                .Include(i => i.Product)
                .ToList();

            if (!listCart.Any())
                return RedirectToAction(nameof(Index));

            var header = new OrderHeader
            {
                Name = user?.Name ?? "",
                LastName = user?.LastName ?? "",
                Addres = user?.Addres ?? "",
                sehir = user?.City ?? "",
                PostKodu = user?.PostaKodu ?? "",
                PhoneNumber = user?.PhoneNumber ?? "",
                Country = "Syria"
            };

            ApplyTotals(header, listCart);
            ViewBag.ShippingCountries = GetShippingCountries();
            ViewBag.OrderSubmitToken = CreateOrderSubmitToken(userId);
            ShoppingCartVM = new ShoppingCartVM { OrderHeader = header, ListCart = listCart };
            return View(ShoppingCartVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Summary(ShoppingCartVM model, string? orderSubmitToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Challenge();

            if (!ConsumeOrderSubmitToken(userId, orderSubmitToken))
            {
                toast.AddWarningToastMessage(_localizer["ToastOrderAlreadySubmitted"]);
                return RedirectToAction("Index", "Order");
            }

            if (!model.AcceptTerms && !AcceptTerms)
                ModelState.AddModelError(nameof(AcceptTerms), _localizer["MustAcceptTerms"]);

            RemoveCardValidation();

            var listCart = db.ShoppingCarts
                .Where(i => i.ApplicationUserId == userId)
                .Include(i => i.Product)
                .ThenInclude(p => p!.categoty)
                .ToList();

            if (!listCart.Any())
            {
                toast.AddWarningToastMessage(_localizer["ToastCartEmpty"]);
                return RedirectToAction(nameof(Index));
            }

            if (listCart.Any(i => i.Product == null || i.Product.StockQuantity <= 0 || i.Count > i.Product.StockQuantity))
            {
                ModelState.AddModelError(string.Empty, _localizer["ToastInsufficientStock"]);
            }

            // Prevent accidental duplicate orders within a short window.
            var recentCutoff = DateTime.UtcNow.AddSeconds(-45);
            var duplicate = db.OrderHeaders.Any(o =>
                o.ApplicationUserId == userId &&
                o.orderDate >= recentCutoff &&
                o.orderStatus == Diger.status_awaiting_payment);
            if (duplicate)
            {
                toast.AddWarningToastMessage(_localizer["ToastOrderAlreadySubmitted"]);
                return RedirectToAction("Index", "Order");
            }

            if (string.IsNullOrWhiteSpace(model.OrderHeader.Country))
                model.OrderHeader.Country = "Syria";

            ApplyTotals(model.OrderHeader, listCart);

            if (!ModelState.IsValid)
            {
                model.ListCart = listCart;
                ViewBag.ShippingCountries = GetShippingCountries();
                ViewBag.OrderSubmitToken = CreateOrderSubmitToken(userId);
                return View(model);
            }

            var rateTry = _currency.GetRate("TRY");
            model.OrderHeader.orderStatus = Diger.status_awaiting_payment;
            model.OrderHeader.ApplicationUserId = userId;
            model.OrderHeader.orderDate = DateTime.UtcNow;
            model.OrderHeader.PaymentCurrency = _currency.CurrentCurrency;
            model.OrderHeader.ExchangeRateToTry = rateTry;
            model.OrderHeader.TotalPaidTry = _currency.ConvertFromUsd(model.OrderHeader.orderTotal, "TRY");
            model.OrderHeader.PaymentMethod = Diger.Payment_BankTransfer;

            await using var tx = await db.Database.BeginTransactionAsync();
            try
            {
                // Reload products with tracking and decrement stock atomically within this transaction.
                foreach (var item in listCart)
                {
                    var product = await db.Products.FirstOrDefaultAsync(p => p.Id == item.ProductId);
                    if (product == null || !StockHelper.CanFulfill(product, item.Count))
                        throw new InvalidOperationException(_localizer["ToastInsufficientStock"]);

                    product.StockQuantity -= item.Count;
                    product.SyncStockFlag();
                }

                db.OrderHeaders.Add(model.OrderHeader);
                await db.SaveChangesAsync();

                foreach (var item in listCart)
                {
                    db.orderDetailses.Add(new orderDetails
                    {
                        productId = item.ProductId,
                        OrederId = model.OrderHeader.Id,
                        Price = PriceHelper.EffectiveUnitPrice(item.Product),
                        count = item.Count,
                    });
                }

                await db.SaveChangesAsync();

                if (!string.IsNullOrWhiteSpace(model.OrderHeader.CouponCode))
                {
                    var used = await db.Coupons.FirstOrDefaultAsync(c => c.Code == model.OrderHeader.CouponCode);
                    if (used != null)
                    {
                        used.UsedCount += 1;
                        await db.SaveChangesAsync();
                    }
                }

                db.ShoppingCarts.RemoveRange(listCart);
                await db.SaveChangesAsync();
                await tx.CommitAsync();
                CatalogCache.Invalidate(_cache);
            }
            catch
            {
                await tx.RollbackAsync();
                toast.AddErrorToastMessage(_localizer["ToastOrderFailed"]);
                ViewBag.OrderSubmitToken = CreateOrderSubmitToken(userId);
                model.ListCart = listCart;
                ViewBag.ShippingCountries = GetShippingCountries();
                return View(model);
            }

            await _notifications.SendOrderPlacedAsync(model.OrderHeader);
            HttpContext.Session.SetInt32(Diger.ssShoppingCart, 0);
            toast.AddSuccessToastMessage(_localizer["ToastOrderPlaced"]);
            return RedirectToAction(nameof(Success), new { id = model.OrderHeader.Id });
        }

        [AllowAnonymous]
        [HttpGet]
        [HttpPost]
        public async Task<IActionResult> PaymentCallback([FromForm] string? token)
        {
            token ??= Request.Query["token"].FirstOrDefault();
            if (string.IsNullOrEmpty(token))
            {
                toast.AddErrorToastMessage(_localizer["ToastPaymentFailed"]);
                return RedirectToAction(nameof(Index), "Home");
            }

            var order = db.OrderHeaders.FirstOrDefault(o => o.IyzipayToken == token);
            if (order == null)
            {
                toast.AddErrorToastMessage(_localizer["ToastPaymentFailed"]);
                return RedirectToAction(nameof(Index), "Home");
            }

            var result = _iyzipay.RetrievePayment(token);
            if (result?.Success == true)
            {
                order.orderStatus = Diger.status_confirmed;
                order.IyzipayPaymentId = result.PaymentId;
                db.OrderHeaders.Update(order);

                var cartItems = db.ShoppingCarts.Where(c => c.ApplicationUserId == order.ApplicationUserId);
                db.ShoppingCarts.RemoveRange(cartItems);
                db.SaveChanges();

                await _notifications.SendOrderConfirmedAsync(order);
                HttpContext.Session.SetInt32(Diger.ssShoppingCart, 0);
                toast.AddSuccessToastMessage(_localizer["ToastPaymentSuccess"]);
                return RedirectToAction(nameof(Success), new { id = order.Id });
            }

            RollbackOrder(order.Id);
            toast.AddErrorToastMessage(result?.ErrorMessage ?? _localizer["ToastPaymentFailed"]);
            return RedirectToAction(nameof(Summary));
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> PayPalCallback(int orderId, string token)
        {
            var order = db.OrderHeaders.FirstOrDefault(o => o.Id == orderId);
            if (order == null || string.IsNullOrEmpty(order.PayPalOrderId))
            {
                toast.AddErrorToastMessage(_localizer["ToastPaymentFailed"]);
                return RedirectToAction(nameof(Index), "Home");
            }

            var result = await _paypal.CaptureOrderAsync(token);
            if (result.Success)
            {
                order.orderStatus = Diger.status_confirmed;
                order.IyzipayPaymentId = result.CaptureId;
                db.OrderHeaders.Update(order);

                var cartItems = db.ShoppingCarts.Where(c => c.ApplicationUserId == order.ApplicationUserId);
                db.ShoppingCarts.RemoveRange(cartItems);
                db.SaveChanges();

                await _notifications.SendOrderConfirmedAsync(order);
                HttpContext.Session.SetInt32(Diger.ssShoppingCart, 0);
                toast.AddSuccessToastMessage(_localizer["ToastPaymentSuccess"]);
                return RedirectToAction(nameof(Success), new { id = order.Id });
            }

            RollbackOrder(order.Id);
            toast.AddErrorToastMessage(result.ErrorMessage ?? _localizer["ToastPaymentFailed"]);
            return RedirectToAction(nameof(Summary));
        }

        public IActionResult Success(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = db.OrderHeaders
                .AsNoTracking()
                .Include(o => o.ApplicationUser)
                .FirstOrDefault(o => o.Id == id && (userId == null || o.ApplicationUserId == userId));

            if (order == null)
                return RedirectToAction("Index", "Order");

            return View(order);
        }

        [HttpGet]
        public IActionResult QuoteShipping(string country, string? city)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return Unauthorized();

            var listCart = db.ShoppingCarts
                .AsNoTracking()
                .Where(i => i.ApplicationUserId == userId)
                .Include(i => i.Product)
                .ToList();

            if (!listCart.Any())
                return Json(new { error = "empty" });

            country = string.IsNullOrWhiteSpace(country) ? "Syria" : country.Trim();
            city ??= "";

            var subtotal = listCart.Sum(i => i.Count * PriceHelper.EffectiveUnitPrice(i.Product));
            var shipping = (double)_shipping.GetShippingCostUsd(country, city, (decimal)subtotal);
            var vat = subtotal * (double)_shipping.GetVatRate(country, city);
            var total = subtotal + shipping + vat;

            return Json(new
            {
                subtotalUsd = subtotal,
                shippingUsd = shipping,
                vatUsd = vat,
                totalUsd = total,
                deliveryMin = _shipping.GetDeliveryDaysMin(country, city),
                deliveryMax = _shipping.GetDeliveryDaysMax(country, city),
                subtotalFormatted = _currency.Format(subtotal),
                shippingFormatted = shipping <= 0 ? _localizer["FreeShipping"] : _currency.Format(shipping),
                vatFormatted = _currency.Format(vat),
                totalFormatted = _currency.Format(total),
                totalTryFormatted = _currency.FormatInCurrency(total, "TRY")
            });
        }

        private ShoppingCart? GetOwnedCart(int cartId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null) return null;

            return db.ShoppingCarts
                .Include(c => c.Product)
                .FirstOrDefault(i => i.Id == cartId && i.ApplicationUserId == userId);
        }

        private List<string> GetShippingCountries() =>
            db.ShippingZones.AsNoTracking()
                .Where(z => z.IsActive)
                .Select(z => z.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

        private string CreateOrderSubmitToken(string userId)
        {
            var token = Guid.NewGuid().ToString("N");
            HttpContext.Session.SetString($"order_submit:{userId}", token);
            return token;
        }

        private bool ConsumeOrderSubmitToken(string userId, string? token)
        {
            var key = $"order_submit:{userId}";
            var expected = HttpContext.Session.GetString(key);
            HttpContext.Session.Remove(key);
            return !string.IsNullOrEmpty(token) &&
                   !string.IsNullOrEmpty(expected) &&
                   string.Equals(expected, token, StringComparison.Ordinal);
        }

        private void ApplyTotals(OrderHeader header, IEnumerable<ShoppingCart> listCart)
        {
            var subtotal = listCart.Sum(i => i.Count * PriceHelper.EffectiveUnitPrice(i.Product));
            var discount = 0d;
            var couponCode = HttpContext.Session.GetString(Diger.ssCouponCode);
            if (!string.IsNullOrWhiteSpace(couponCode))
            {
                var coupon = db.Coupons.AsNoTracking().FirstOrDefault(c =>
                    c.IsActive && c.Code == couponCode &&
                    (c.StartsAt == null || c.StartsAt <= DateTime.UtcNow) &&
                    (c.EndsAt == null || c.EndsAt >= DateTime.UtcNow) &&
                    (c.MaxUses <= 0 || c.UsedCount < c.MaxUses) &&
                    subtotal >= c.MinOrderUsd);
                if (coupon != null)
                {
                    discount = coupon.IsPercent
                        ? Math.Round(subtotal * (coupon.Value / 100d), 2)
                        : Math.Min(coupon.Value, subtotal);
                    header.CouponCode = coupon.Code;
                }
                else
                {
                    HttpContext.Session.Remove(Diger.ssCouponCode);
                    header.CouponCode = null;
                }
            }

            var afterDiscount = Math.Max(0, subtotal - discount);
            var shipping = (double)_shipping.GetShippingCostUsd(header.Country, header.sehir, (decimal)afterDiscount);
            var vat = afterDiscount * (double)_shipping.GetVatRate(header.Country, header.sehir);

            header.SubtotalUsd = subtotal;
            header.DiscountUsd = discount;
            header.ShippingUsd = shipping;
            header.VatUsd = vat;
            header.orderTotal = afterDiscount + shipping + vat;

            ViewBag.DeliveryMin = _shipping.GetDeliveryDaysMin(header.Country, header.sehir);
            ViewBag.DeliveryMax = _shipping.GetDeliveryDaysMax(header.Country, header.sehir);
            ViewBag.ShippingUsd = shipping;
            ViewBag.VatUsd = vat;
            ViewBag.SubtotalUsd = subtotal;
            ViewBag.DiscountUsd = discount;
            ViewBag.CouponCode = header.CouponCode;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult ApplyCoupon(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                toast.AddErrorToastMessage(_localizer["ToastCouponInvalid"]);
                return RedirectToAction(nameof(Summary));
            }

            code = code.Trim().ToUpperInvariant();
            var coupon = db.Coupons.AsNoTracking().FirstOrDefault(c => c.Code.ToUpper() == code && c.IsActive);
            if (coupon == null ||
                (coupon.StartsAt != null && coupon.StartsAt > DateTime.UtcNow) ||
                (coupon.EndsAt != null && coupon.EndsAt < DateTime.UtcNow) ||
                (coupon.MaxUses > 0 && coupon.UsedCount >= coupon.MaxUses))
            {
                toast.AddErrorToastMessage(_localizer["ToastCouponInvalid"]);
                return RedirectToAction(nameof(Summary));
            }

            HttpContext.Session.SetString(Diger.ssCouponCode, coupon.Code);
            toast.AddSuccessToastMessage(_localizer["ToastCouponApplied"]);
            return RedirectToAction(nameof(Summary));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.Remove(Diger.ssCouponCode);
            return RedirectToAction(nameof(Summary));
        }

        private void RollbackOrder(int orderId)
        {
            var details = db.orderDetailses.Where(d => d.OrederId == orderId);
            db.orderDetailses.RemoveRange(details);
            var header = db.OrderHeaders.Find(orderId);
            if (header != null)
                db.OrderHeaders.Remove(header);
            db.SaveChanges();
        }

        private void RemoveCardValidation()
        {
            foreach (var key in new[] { "CartName", "CartNumber", "ExpirationMonth", "ExpiratioYear", "CVC" })
                ModelState.Remove($"OrderHeader.{key}");
        }
    }
}
