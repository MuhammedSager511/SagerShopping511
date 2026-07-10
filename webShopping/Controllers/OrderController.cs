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



        [BindProperty]

        public OrderDetailsVM orderMv { get; set; } = new();



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

            var claimIdentity = (ClaimsIdentity)User.Identity!;

            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);



            IEnumerable<OrderHeader> orderHeadersList;

            if (User.IsInRole(Diger.Role_Admin))

            {

                orderHeadersList = db.OrderHeaders

                    .Include(o => o.orderDetails)

                    .ThenInclude(d => d.product)

                    .ToList();

            }

            else

            {

                orderHeadersList = db.OrderHeaders

                    .Where(i => i.ApplicationUserId == claim!.Value)

                    .Include(i => i.ApplicationUser)

                    .Include(o => o.orderDetails)

                    .ThenInclude(d => d.product)

                    .ToList();

            }



            return View(orderHeadersList);

        }



        [HttpPost]
        [Authorize(Roles = Diger.Role_Admin)]
        public async Task<IActionResult> Approved()
        {
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == orderMv.OrderHeader.Id);
            if (orderHeader == null) return NotFound();

            if (orderHeader.orderStatus != Diger.status_payment_review &&
                orderHeader.orderStatus != Diger.status_pending)
            {
                toast.AddWarningToastMessage(_localizer["PaymentVerifyNotAllowed"]);
                return RedirectToAction("Details", new { id = orderHeader.Id });
            }

            orderHeader.orderStatus = Diger.status_confirmed;
            db.SaveChanges();
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

        [Authorize(Roles = Diger.Role_Admin)]

        public async Task<IActionResult> ShipIt()

        {

            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == orderMv.OrderHeader.Id);

            if (orderHeader == null) return NotFound();



            orderHeader.orderStatus = Diger.status_cargo;

            if (!string.IsNullOrWhiteSpace(orderMv.OrderHeader.TrackingNumber))

                orderHeader.TrackingNumber = orderMv.OrderHeader.TrackingNumber.Trim();

            else if (string.IsNullOrEmpty(orderHeader.TrackingNumber))

                orderHeader.TrackingNumber = $"TRK-{orderHeader.Id:D6}-{DateTime.UtcNow:yyyyMMdd}";



            db.SaveChanges();

            await _notifications.SendOrderShippedAsync(orderHeader);

            toast.AddSuccessToastMessage(_localizer["ToastOrderShipped"]);

            return RedirectToAction("Details", new { id = orderHeader.Id });

        }



        public IActionResult pending()

        {

            var claimIdentity = (ClaimsIdentity)User.Identity!;

            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);



            IEnumerable<OrderHeader> orderHeadersList;

            if (User.IsInRole(Diger.Role_Admin))

                orderHeadersList = db.OrderHeaders.Where(i =>
                    i.orderStatus == Diger.status_pending ||
                    i.orderStatus == Diger.status_awaiting_payment ||
                    i.orderStatus == Diger.status_payment_review);

            else

                orderHeadersList = db.OrderHeaders.Where(i =>
                    i.ApplicationUserId == claim!.Value &&
                    (i.orderStatus == Diger.status_pending ||
                     i.orderStatus == Diger.status_awaiting_payment ||
                     i.orderStatus == Diger.status_payment_review))

                    .Include(i => i.ApplicationUser);



            return View(orderHeadersList);

        }



        public IActionResult confirmed()

        {

            var claimIdentity = (ClaimsIdentity)User.Identity!;

            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);



            IEnumerable<OrderHeader> orderHeadersList;

            if (User.IsInRole(Diger.Role_Admin))

                orderHeadersList = db.OrderHeaders.Where(i => i.orderStatus == Diger.status_confirmed);

            else

                orderHeadersList = db.OrderHeaders.Where(i => i.ApplicationUserId == claim!.Value && i.orderStatus == Diger.status_confirmed)

                    .Include(i => i.ApplicationUser);



            return View(orderHeadersList);

        }



        public IActionResult cargo()

        {

            var claimIdentity = (ClaimsIdentity)User.Identity!;

            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);



            IEnumerable<OrderHeader> orderHeadersList;

            if (User.IsInRole(Diger.Role_Admin))

                orderHeadersList = db.OrderHeaders.Where(i => i.orderStatus == Diger.status_cargo);

            else

                orderHeadersList = db.OrderHeaders.Where(i => i.ApplicationUserId == claim!.Value && i.orderStatus == Diger.status_cargo)

                    .Include(i => i.ApplicationUser);



            return View(orderHeadersList);

        }



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
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);

            if (orderHeader == null)
                return NotFound();

            if (!User.IsInRole(Diger.Role_Admin) && orderHeader.ApplicationUserId != claim)
                return Forbid();

            orderMv = new OrderDetailsVM
            {
                OrderHeader = orderHeader,
                orderDetails = db.orderDetailses.Where(x => x.OrederId == id).Include(x => x.product)
            };

            return View(orderMv);
        }

        [AllowAnonymous]
        public IActionResult Track() => View();

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Track(int orderId, string email)
        {
            if (orderId <= 0 || string.IsNullOrWhiteSpace(email))
            {
                ModelState.AddModelError(string.Empty, _localizer["TrackOrderInvalid"]);
                return View();
            }

            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email.Trim());
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["OrderNotFound"]);
                return View();
            }

            var order = await db.OrderHeaders
                .Include(o => o.orderDetails)
                .ThenInclude(d => d.product)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.ApplicationUserId == user.Id);

            if (order == null)
            {
                ModelState.AddModelError(string.Empty, _localizer["OrderNotFound"]);
                return View();
            }

            ViewBag.TrackedOrder = order;
            return View();
        }
    }
}
