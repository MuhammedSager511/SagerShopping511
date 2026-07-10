using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Services
{
    public interface IOrderNotificationService
    {
        Task SendOrderPlacedAsync(OrderHeader order);
        Task SendOrderConfirmedAsync(OrderHeader order);
        Task SendOrderShippedAsync(OrderHeader order);
        Task SendPaymentSubmittedAsync(OrderHeader order);
    }

    public class OrderNotificationService : IOrderNotificationService
    {
        private readonly ILogger<OrderNotificationService> _logger;
        private readonly IAppLocalizer _localizer;
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private readonly ISmsService _sms;
        private readonly ISiteContentService _siteContent;
        private readonly INotificationService _inApp;

        public OrderNotificationService(
            ILogger<OrderNotificationService> logger,
            IAppLocalizer localizer,
            ApplicationDbContext db,
            IEmailSender emailSender,
            ISmsService sms,
            ISiteContentService siteContent,
            INotificationService inApp)
        {
            _logger = logger;
            _localizer = localizer;
            _db = db;
            _emailSender = emailSender;
            _sms = sms;
            _siteContent = siteContent;
            _inApp = inApp;
        }

        public async Task SendOrderPlacedAsync(OrderHeader order)
        {
            var user = order.ApplicationUser ?? await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId);
            var culture = NotificationCulture.FromCountry(order.Country);
            var email = user?.Email;
            var bankInfo = LocalizedContent.BankTransferInfo(_siteContent.Settings, _localizer);

            if (!string.IsNullOrEmpty(email))
            {
                var subject = OrderEmailBuilder.PlacedSubject(_localizer, culture, order);
                var body = WrapHtml(OrderEmailBuilder.PlacedBody(_localizer, culture, order, bankInfo), culture);
                try
                {
                    await _emailSender.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order placed email for order #{Id}", order.Id);
                }
            }

            var smsText = _localizer.Get("SmsOrderPlaced", culture)
                .Replace("{OrderId}", order.Id.ToString())
                .Replace("{Total}", order.orderTotal.ToString("N2"));
            await _sms.SendAsync(order.PhoneNumber, smsText);

            var orderReplacements = OrderReplacements(order);
            if (!string.IsNullOrEmpty(order.ApplicationUserId))
            {
                await _inApp.NotifyUserAsync(
                    order.ApplicationUserId,
                    "NotifOrderPlacedTitle",
                    "NotifOrderPlacedMessage",
                    orderReplacements,
                    $"/Order/Details/{order.Id}",
                    "order",
                    order.Id);
            }

            await _inApp.NotifyAdminsAsync(
                "NotifAdminNewOrderTitle",
                "NotifAdminNewOrderMessage",
                orderReplacements,
                $"/Order/Details/{order.Id}",
                "order",
                order.Id);
        }

        public async Task SendOrderConfirmedAsync(OrderHeader order)
        {
            var user = order.ApplicationUser ?? await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId);
            var culture = NotificationCulture.FromCountry(order.Country);
            var email = user?.Email;

            if (!string.IsNullOrEmpty(email))
            {
                var subject = OrderEmailBuilder.ConfirmedSubject(_localizer, culture, order);
                var body = WrapHtml(OrderEmailBuilder.ConfirmedBody(_localizer, culture, order), culture);
                try
                {
                    await _emailSender.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send order confirmation email for order #{Id}", order.Id);
                }
            }

            var smsText = _localizer.Get("SmsOrderConfirmed", culture)
                .Replace("{OrderId}", order.Id.ToString())
                .Replace("{Total}", order.orderTotal.ToString("N2"));
            await _sms.SendAsync(order.PhoneNumber, smsText);

            if (!string.IsNullOrEmpty(order.ApplicationUserId))
            {
                await _inApp.NotifyUserAsync(
                    order.ApplicationUserId,
                    "NotifOrderConfirmedTitle",
                    "NotifOrderConfirmedMessage",
                    OrderReplacements(order),
                    $"/Order/Details/{order.Id}",
                    "order",
                    order.Id);
            }
        }

        public async Task SendOrderShippedAsync(OrderHeader order)
        {
            var user = order.ApplicationUser ?? await _db.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId);
            var culture = NotificationCulture.FromCountry(order.Country);
            var email = user?.Email;

            if (!string.IsNullOrEmpty(email))
            {
                var subject = OrderEmailBuilder.ShippedSubject(_localizer, culture, order);
                var body = WrapHtml(OrderEmailBuilder.ShippedBody(_localizer, culture, order), culture);
                try
                {
                    await _emailSender.SendEmailAsync(email, subject, body);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send shipping email for order #{Id}", order.Id);
                }
            }

            var smsText = _localizer.Get("SmsOrderShipped", culture)
                .Replace("{OrderId}", order.Id.ToString())
                .Replace("{Tracking}", order.TrackingNumber ?? "—");
            await _sms.SendAsync(order.PhoneNumber, smsText);

            if (!string.IsNullOrEmpty(order.ApplicationUserId))
            {
                var replacements = OrderReplacements(order);
                replacements["Tracking"] = order.TrackingNumber ?? "—";
                await _inApp.NotifyUserAsync(
                    order.ApplicationUserId,
                    "NotifOrderShippedTitle",
                    "NotifOrderShippedMessage",
                    replacements,
                    $"/Order/Details/{order.Id}",
                    "order",
                    order.Id);
            }
        }

        public async Task SendPaymentSubmittedAsync(OrderHeader order)
        {
            var replacements = OrderReplacements(order);
            replacements["Reference"] = string.IsNullOrWhiteSpace(order.PaymentReference)
                ? "—"
                : order.PaymentReference;

            await _inApp.NotifyAdminsAsync(
                "NotifAdminPaymentSubmittedTitle",
                "NotifAdminPaymentSubmittedMessage",
                replacements,
                $"/Order/Details/{order.Id}",
                "payment",
                order.Id);
        }

        private static Dictionary<string, string> OrderReplacements(OrderHeader order) =>
            new()
            {
                ["OrderId"] = order.Id.ToString(),
                ["Total"] = order.orderTotal.ToString("N2")
            };

        private static string WrapHtml(string content, string culture)
        {
            var dir = culture == "ar" ? "rtl" : "ltr";
            var align = culture == "ar" ? "right" : "left";
            return $"""
                <!DOCTYPE html>
                <html lang="{culture}" dir="{dir}">
                <body style="font-family:Segoe UI,Arial,sans-serif;line-height:1.6;color:#222;max-width:560px;margin:0 auto;padding:24px">
                <div style="text-align:center;margin-bottom:20px">
                  <strong style="font-size:20px">Sager<span style="color:#e94560">Shop</span></strong>
                </div>
                <div style="background:#f8f9fa;border-radius:12px;padding:20px;text-align:{align}">
                {content.Replace("\n", "<br/>")}
                </div>
                <p style="font-size:12px;color:#888;text-align:center;margin-top:24px">© SagerShop</p>
                </body></html>
                """;
        }
    }
}
