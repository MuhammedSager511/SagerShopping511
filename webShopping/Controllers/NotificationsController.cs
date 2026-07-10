using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notifications;
        private readonly IAppLocalizer _localizer;

        public NotificationsController(INotificationService notifications, IAppLocalizer localizer)
        {
            _notifications = notifications;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _notifications.GetAllAsync(userId);
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id, string? returnUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notifications.MarkAsReadAsync(id, userId);
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Open(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notification = await _notifications.GetAsync(id, userId);
            if (notification == null) return NotFound();

            await _notifications.MarkAsReadAsync(id, userId);
            if (!string.IsNullOrEmpty(notification.LinkUrl) && Url.IsLocalUrl(notification.LinkUrl))
                return Redirect(notification.LinkUrl);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _notifications.MarkAllAsReadAsync(userId);
            return RedirectToAction(nameof(Index));
        }
    }
}
