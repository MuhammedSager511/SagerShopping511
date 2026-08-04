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
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IToastNotification toast;
        private readonly IAppLocalizer _localizer;
        private readonly IOrderNotificationService _notifications;

        public OrderController(
            ApplicationDbContext db,
            IToastNotification toast,
            IAppLocalizer localizer,
            IOrderNotificationService notifications)
        {
            this.db = db;
            this.toast = toast;
            _localizer = localizer;
            _notifications = notifications;
        }

        public IActionResult Index()
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<OrderHeader> query = db.OrderHeaders
                .AsNoTracking()
                .Include(o => o.ApplicationUser)
                .Include(o => o.orderDetails)
                .ThenInclude(d => d.product);

            if (!User.IsInRole(Diger.Role_Admin))
                query = query.Where(i => i.ApplicationUserId == claim);

            var orderHeadersList = query
                .OrderByDescending(o => o.orderDate)
                .ThenByDescending(o => o.Id)
                .ToList();

            return View(orderHeadersList);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Diger.Role_Admin)]
        public async Task<IActionResult> Approved([Bind(Prefix = "")] OrderDetailsVM orderMv)
        {
            var id = orderMv.OrderHeader?.Id ?? 0;
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);
            if (orderHeader == null) return NotFound();

            if (orderHeader.orderStatus != Diger.status_payment_review &&
                orderHeader.orderStatus != Diger.status_pending)
            {
                toast.AddWarningToastMessage(_localizer["PaymentVerifyNotAllowed"]);
                return RedirectToAction("Details", new { id = orderHeader.Id });
            }

            orderHeader.orderStatus = Diger.status_confirmed;
            await db.SaveChangesAsync();
            await _notifications.SendOrderConfirmedAsync(orderHeader);
            toast.AddSuccessToastMessage(_localizer["ToastPaymentVerified"]);
            return RedirectToAction("Details", new { id = orderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitPayment(int id, string paymentReference, string? paymentBankName)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var order = await db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == id && o.ApplicationUserId == userId);
            if (order == null) return NotFound();

            if (order.orderStatus != Diger.status_awaiting_payment)
            {
                toast.AddWarningToastMessage(_localizer["PaymentAlreadySubmitted"]);
                return RedirectToAction("Details", new { id });
            }

            if (string.IsNullOrWhiteSpace(paymentReference))
            {
                toast.AddErrorToastMessage(_localizer["PaymentReferenceRequired"]);
                return RedirectToAction("Details", new { id });
            }

            order.PaymentReference = paymentReference.Trim();
            order.PaymentBankName = string.IsNullOrWhiteSpace(paymentBankName) ? null : paymentBankName.Trim();
            order.PaymentSubmittedAt = DateTime.UtcNow;
            order.orderStatus = Diger.status_payment_review;
            await db.SaveChangesAsync();

            await _notifications.SendPaymentSubmittedAsync(order);

            toast.AddSuccessToastMessage(_localizer["ToastPaymentSubmitted"]);
            return RedirectToAction("Details", new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Diger.Role_Admin)]
        public async Task<IActionResult> ShipIt([Bind(Prefix = "")] OrderDetailsVM orderMv)
        {
            var id = orderMv.OrderHeader?.Id ?? 0;
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);
            if (orderHeader == null) return NotFound();

            // Only after order is confirmed — not from payment-review
            if (!OrderStatusHelper.CanShip(orderHeader.orderStatus))
            {
                toast.AddWarningToastMessage(_localizer["ShipNotAllowed"]);
                return RedirectToAction("Details", new { id = orderHeader.Id });
            }

            orderHeader.orderStatus = Diger.status_cargo;
            if (!string.IsNullOrWhiteSpace(orderMv.OrderHeader?.TrackingNumber))
                orderHeader.TrackingNumber = orderMv.OrderHeader.TrackingNumber.Trim();
            else if (string.IsNullOrEmpty(orderHeader.TrackingNumber))
                orderHeader.TrackingNumber = $"TRK-{orderHeader.Id:D6}-{DateTime.UtcNow:yyyyMMdd}";

            await db.SaveChangesAsync();
            await _notifications.SendOrderShippedAsync(orderHeader);
            toast.AddSuccessToastMessage(_localizer["ToastOrderShipped"]);
            return RedirectToAction("Details", new { id = orderHeader.Id });
        }

        // Prepare step removed from customer flow — kept only to map legacy rows to confirmed.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Diger.Role_Admin)]
        public async Task<IActionResult> Prepare([Bind(Prefix = "")] OrderDetailsVM orderMv)
        {
            var id = orderMv.OrderHeader?.Id ?? 0;
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);
            if (orderHeader == null) return NotFound();

            if (orderHeader.orderStatus != Diger.status_confirmed && orderHeader.orderStatus != Diger.status_preparing)
            {
                toast.AddWarningToastMessage(_localizer["ToastProductUnavailable"]);
                return RedirectToAction("Details", new { id = orderHeader.Id });
            }

            // Collapse old "preparing" into confirmed pipeline
            orderHeader.orderStatus = Diger.status_confirmed;
            await db.SaveChangesAsync();
            toast.AddSuccessToastMessage(_localizer["OrderStageConfirmed"]);
            return RedirectToAction("Details", new { id = orderHeader.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Diger.Role_Admin)]
        public async Task<IActionResult> Cancel([Bind(Prefix = "")] OrderDetailsVM orderMv)
        {
            var id = orderMv.OrderHeader?.Id ?? 0;
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);
            if (orderHeader == null) return NotFound();

            if (orderHeader.orderStatus == Diger.status_cancelled || orderHeader.orderStatus == Diger.status_cargo)
            {
                toast.AddWarningToastMessage(_localizer["ToastProductUnavailable"]);
                return RedirectToAction("Details", new { id = orderHeader.Id });
            }

            var lines = db.orderDetailses.Where(d => d.OrederId == orderHeader.Id).ToList();
            if (orderHeader.orderStatus is Diger.status_confirmed or Diger.status_preparing or Diger.status_payment_review or Diger.status_awaiting_payment)
            {
                foreach (var line in lines)
                {
                    var product = db.Products.FirstOrDefault(p => p.Id == line.productId);
                    if (product == null) continue;
                    product.StockQuantity += line.count;
                    product.SyncStockFlag();
                }
            }

            orderHeader.orderStatus = Diger.status_cancelled;
            await db.SaveChangesAsync();
            toast.AddSuccessToastMessage(_localizer["Cancelled"]);
            return RedirectToAction("Details", new { id = orderHeader.Id });
        }

        public IActionResult Invoice(int id)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderHeader = db.OrderHeaders.AsNoTracking().FirstOrDefault(i => i.Id == id);
            if (orderHeader == null) return NotFound();
            if (!User.IsInRole(Diger.Role_Admin) && orderHeader.ApplicationUserId != claim)
                return Forbid();

            ViewData["Title"] = $"{_localizer["Invoice"]} #{id}";
            var vm = new OrderDetailsVM
            {
                OrderHeader = orderHeader,
                orderDetails = db.orderDetailses.AsNoTracking()
                    .Where(x => x.OrederId == id)
                    .Include(x => x.product)
            };
            return View(vm);
        }

        public IActionResult pending() => StatusList(
            Diger.status_pending, Diger.status_awaiting_payment, Diger.status_payment_review);

        public IActionResult confirmed() => StatusList(Diger.status_confirmed, Diger.status_preparing);

        public IActionResult cargo() => StatusList(Diger.status_cargo);

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Diger.Role_Admin)]
        public IActionResult Delete(int id)
        {
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);
            if (orderHeader == null) return NotFound();

            db.OrderHeaders.Remove(orderHeader);
            db.SaveChanges();
            toast.AddSuccessToastMessage(_localizer["ToastOrderDeleted"]);
            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var orderHeader = db.OrderHeaders.AsNoTracking().FirstOrDefault(i => i.Id == id);

            if (orderHeader == null)
                return NotFound();

            if (!User.IsInRole(Diger.Role_Admin) && orderHeader.ApplicationUserId != claim)
                return Forbid();

            var vm = new OrderDetailsVM
            {
                OrderHeader = orderHeader,
                orderDetails = db.orderDetailses
                    .AsNoTracking()
                    .Where(x => x.OrederId == id)
                    .Include(x => x.product)
            };

            return View(vm);
        }

        [AllowAnonymous]
        public IActionResult Track() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(string? orderId, string? email, string? phone)
        {
            ViewBag.InputOrderId = orderId;
            ViewBag.InputEmail = email;
            ViewBag.InputPhone = phone;
            ModelState.Clear();

            if (!TryParseOrderId(orderId, out var id))
            {
                ModelState.AddModelError(string.Empty, _localizer["TrackOrderInvalid"]);
                return View();
            }

            var emailNorm = (email ?? "").Trim();
            var phoneRaw = (phone ?? "").Trim();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Guest must provide email or phone; signed-in users can track their own order with id only
            if (string.IsNullOrWhiteSpace(emailNorm) && string.IsNullOrWhiteSpace(phoneRaw) && string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError(string.Empty, _localizer["TrackOrderInvalid"]);
                return View();
            }

            var order = await db.OrderHeaders
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["OrderNotFound"]);
                return View();
            }

            var matched = false;

            // Owner already signed in
            if (!string.IsNullOrEmpty(userId) &&
                string.Equals(order.ApplicationUserId, userId, StringComparison.Ordinal))
            {
                matched = true;
            }

            // Admin can look up any order
            if (!matched && User.IsInRole(Diger.Role_Admin))
                matched = true;

            if (!matched && !string.IsNullOrWhiteSpace(emailNorm))
            {
                var emailLower = emailNorm.ToLowerInvariant();
                var emailUpper = emailNorm.ToUpperInvariant();

                matched = await db.Users.AsNoTracking().AnyAsync(u =>
                    u.Id == order.ApplicationUserId &&
                    (
                        (u.Email != null && u.Email.ToLower() == emailLower) ||
                        (u.NormalizedEmail != null && u.NormalizedEmail == emailUpper) ||
                        (u.UserName != null && u.UserName.ToLower() == emailLower) ||
                        (u.NormalizedUserName != null && u.NormalizedUserName == emailUpper)
                    ));
            }

            if (!matched && !string.IsNullOrWhiteSpace(phoneRaw))
            {
                matched = PhonesMatch(order.PhoneNumber, phoneRaw);

                if (!matched && !string.IsNullOrEmpty(order.ApplicationUserId))
                {
                    var userPhone = await db.Users.AsNoTracking()
                        .Where(u => u.Id == order.ApplicationUserId)
                        .Select(u => u.PhoneNumber)
                        .FirstOrDefaultAsync();
                    matched = PhonesMatch(userPhone, phoneRaw);
                }
            }

            if (!matched)
            {
                ModelState.AddModelError(string.Empty, _localizer["OrderNotFound"]);
                return View();
            }

            var lines = await db.orderDetailses
                .AsNoTracking()
                .Where(d => d.OrederId == id)
                .Include(d => d.product)
                .ToListAsync();

            ViewBag.TrackedOrder = order;
            ViewBag.TrackedLines = lines;
            return View();
        }

        private static bool TryParseOrderId(string? raw, out int id)
        {
            id = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            var s = raw.Trim().TrimStart('#').Trim();
            if (int.TryParse(s, out id) && id > 0) return true;
            var digits = new string(s.Where(char.IsDigit).ToArray());
            return int.TryParse(digits, out id) && id > 0;
        }

        private static string DigitsOnly(string? phone)
        {
            if (string.IsNullOrWhiteSpace(phone)) return "";
            return new string(phone.Where(char.IsDigit).ToArray());
        }

        private static bool PhonesMatch(string? a, string? b)
        {
            var da = DigitsOnly(a);
            var db = DigitsOnly(b);
            if (string.IsNullOrEmpty(da) || string.IsNullOrEmpty(db)) return false;
            if (da == db) return true;

            // Compare last 9 digits (handles leading 0 / country code differences)
            static string Tail(string d)
            {
                if (d.Length > 9) return d[^9..];
                if (d.Length == 10 && d.StartsWith('0')) return d[1..];
                return d;
            }

            return Tail(da) == Tail(db);
        }

        private IActionResult StatusList(params string[] statuses)
        {
            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            IQueryable<OrderHeader> query = db.OrderHeaders
                .AsNoTracking()
                .Include(i => i.ApplicationUser)
                .Where(i => statuses.Contains(i.orderStatus));

            if (!User.IsInRole(Diger.Role_Admin))
                query = query.Where(i => i.ApplicationUserId == claim);

            return View(query.OrderByDescending(o => o.orderDate).ThenByDescending(o => o.Id).ToList());
        }
    }
}
