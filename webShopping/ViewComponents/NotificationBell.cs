using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webShopping.Services;

namespace webShopping.ViewComponents
{
    public class NotificationBell : ViewComponent
    {
        private readonly INotificationService _notifications;
        private readonly ILogger<NotificationBell> _logger;

        public NotificationBell(INotificationService notifications, ILogger<NotificationBell> logger)
        {
            _notifications = notifications;
            _logger = logger;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            try
            {
                if (User.Identity?.IsAuthenticated != true)
                    return Content("");

                var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Content("");

                var unread = await _notifications.GetUnreadCountAsync(userId);
                var recent = await _notifications.GetRecentAsync(userId, 6);
                return View((Unread: unread, Recent: recent));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "NotificationBell failed");
                return Content("");
            }
        }
    }
}
