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
    public class MessagesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public MessagesController(ApplicationDbContext context, IToastNotification toast, IAppLocalizer localizer)
        {
            _context = context;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var messages = await _context.ContactMessages.AsNoTracking()
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
            return View(messages);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var message = await _context.ContactMessages.FirstOrDefaultAsync(m => m.Id == id);
            if (message == null) return NotFound();

            if (!message.IsRead)
            {
                message.IsRead = true;
                await _context.SaveChangesAsync();
            }

            return View(message);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reply(int id, string adminReply)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.AdminReply = adminReply?.Trim();
            message.RepliedAt = DateTime.UtcNow;
            message.IsRead = true;
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage(_localizer["ToastCategoryUpdated"]);
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkRead(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message == null) return NotFound();

            message.IsRead = true;
            await _context.SaveChangesAsync();

            _toast.AddSuccessToastMessage(_localizer["MarkRead"]);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var message = await _context.ContactMessages.FindAsync(id);
            if (message != null)
            {
                _context.ContactMessages.Remove(message);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
