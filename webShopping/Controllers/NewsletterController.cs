using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class NewsletterController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public NewsletterController(ApplicationDbContext context, IToastNotification toast, IAppLocalizer localizer)
        {
            _context = context;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var subscribers = await _context.NewsletterSubscribers.AsNoTracking()
                .OrderByDescending(s => s.SubscribedAt)
                .ToListAsync();
            return View(subscribers);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subscriber = await _context.NewsletterSubscribers.FindAsync(id);
            if (subscriber != null)
            {
                _context.NewsletterSubscribers.Remove(subscriber);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
