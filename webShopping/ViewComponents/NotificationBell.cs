using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webShopping.Services;

namespace webShopping.ViewComponents
{
    public class NotificationBell : ViewComponent
    {
        private readonly INotificationService _notifications;

        public NotificationBell(INotificationService notifications)
        {
            _notifications = notifications;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            if (!User.Identity?.IsAuthenticated == true)
                return Content("");

            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Content("");

            var unread = await _notifications.GetUnreadCountAsync(userId);
            var recent = await _notifications.GetRecentAsync(userId, 6);
            return View((Unread: unread, Recent: recent));
        }
    }
}
