using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using Microsoft.EntityFrameworkCore;

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

            IPayPalCheckoutService paypal)

        {

            this.db = db;

            this.toast = toast;

            _currency = currency;

            _localizer = localizer;

            _iyzipay = iyzipay;

            _shipping = shipping;

            _notifications = notifications;

            _paypal = paypal;

        }



        public IActionResult Index()

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Challenge();



            ShoppingCartVM = new ShoppingCartVM

            {

                OrderHeader = new OrderHeader(),

                ListCart = db.ShoppingCarts

                    .Where(i => i.ApplicationUserId == userId)

                    .Include(i => i.Product)

            };

            ShoppingCartVM.OrderHeader.orderTotal = 0;

            ShoppingCartVM.OrderHeader.ApplicationUser = db.Users.FirstOrDefault(i => i.Id == userId);



            foreach (var item in ShoppingCartVM.ListCart)

                ShoppingCartVM.OrderHeader.orderTotal += item.Count * item.Product.Price;



            HttpContext.Session.SetInt32(Diger.ssShoppingCart, ShoppingCartVM.ListCart.Count());

            return View(ShoppingCartVM);

        }



        public IActionResult Add(int cartId)

        {

            var cart = db.ShoppingCarts.FirstOrDefault(i => i.Id == cartId);

            if (cart == null) return RedirectToAction(nameof(Index));



            cart.Count += 1;

            db.SaveChanges();

            return RedirectToAction(nameof(Index));

        }



        public IActionResult Decrease(int cartId)

        {

            var cart = db.ShoppingCarts.FirstOrDefault(i => i.Id == cartId);

            if (cart == null) return RedirectToAction(nameof(Index));



            if (cart.Count == 1)

                db.ShoppingCarts.Remove(cart);

            else

                cart.Count -= 1;



            db.SaveChanges();

            var userId = cart.ApplicationUserId;

            HttpContext.Session.SetInt32(Diger.ssShoppingCart, db.ShoppingCarts.Count(i => i.ApplicationUserId == userId));

            return RedirectToAction(nameof(Index));

        }



        public IActionResult Remove(int cartId)

        {

            var cart = db.ShoppingCarts.FirstOrDefault(i => i.Id == cartId);

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



            var user = db.Users.FirstOrDefault(i => i.Id == userId);

            var listCart = db.ShoppingCarts.Where(i => i.ApplicationUserId == userId).Include(i => i.Product).ToList();

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

            ViewBag.ShippingCountries = db.ShippingZones
                .Where(z => z.IsActive)
                .Select(z => z.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            ShoppingCartVM = new ShoppingCartVM { OrderHeader = header, ListCart = listCart };

            return View(ShoppingCartVM);

        }



        [HttpPost]

        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Summary(ShoppingCartVM model)

        {

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null) return Challenge();



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



            if (string.IsNullOrWhiteSpace(model.OrderHeader.Country))

                model.OrderHeader.Country = "Syria";



            ApplyTotals(model.OrderHeader, listCart);



            if (!ModelState.IsValid)

            {

                model.ListCart = listCart;

                ViewBag.ShippingCountries = db.ShippingZones
                    .Where(z => z.IsActive)
                    .Select(z => z.Country)
                    .Distinct()
                    .OrderBy(c => c)
                    .ToList();

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



            db.OrderHeaders.Add(model.OrderHeader);

            db.SaveChanges();



            foreach (var item in listCart)

            {

                db.orderDetailses.Add(new orderDetails

                {

                    productId = item.ProductId,

                    OrederId = model.OrderHeader.Id,

                    Price = item.Product.Price,

                    count = item.Count,

                });

            }

            db.SaveChanges();



            var cartItems = db.ShoppingCarts.Where(c => c.ApplicationUserId == userId);

            db.ShoppingCarts.RemoveRange(cartItems);

            db.SaveChanges();



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

                .Where(i => i.ApplicationUserId == userId)

                .Include(i => i.Product)

                .ToList();



            if (!listCart.Any())

                return Json(new { error = "empty" });



            country = string.IsNullOrWhiteSpace(country) ? "Syria" : country.Trim();

            city ??= "";



            var subtotal = listCart.Sum(i => i.Count * i.Product.Price);

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



        private void ApplyTotals(OrderHeader header, IEnumerable<ShoppingCart> listCart)

        {

            var subtotal = listCart.Sum(i => i.Count * i.Product.Price);

            var shipping = (double)_shipping.GetShippingCostUsd(header.Country, header.sehir, (decimal)subtotal);

            var vat = subtotal * (double)_shipping.GetVatRate(header.Country, header.sehir);



            header.SubtotalUsd = subtotal;

            header.ShippingUsd = shipping;

            header.VatUsd = vat;

            header.orderTotal = subtotal + shipping + vat;



            ViewBag.DeliveryMin = _shipping.GetDeliveryDaysMin(header.Country, header.sehir);

            ViewBag.DeliveryMax = _shipping.GetDeliveryDaysMax(header.Country, header.sehir);

            ViewBag.ShippingUsd = shipping;

            ViewBag.VatUsd = vat;

            ViewBag.SubtotalUsd = subtotal;

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


