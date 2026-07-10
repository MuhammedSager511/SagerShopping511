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
    public class ReviewsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public ReviewsController(ApplicationDbContext db, IToastNotification toast, IAppLocalizer localizer)
        {
            _db = db;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index(string filter = "pending")
        {
            var query = _db.ProductReviews.Include(r => r.Product).AsQueryable();
            query = filter switch
            {
                "approved" => query.Where(r => r.IsApproved),
                "all" => query,
                _ => query.Where(r => !r.IsApproved)
            };

            ViewBag.Filter = filter;
            ViewBag.PendingCount = await _db.ProductReviews.CountAsync(r => !r.IsApproved);
            return View(await query.OrderByDescending(r => r.CreatedAt).ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var review = await _db.ProductReviews.FindAsync(id);
            if (review == null) return NotFound();

            review.IsApproved = true;
            await _db.SaveChangesAsync();
            _toast.AddSuccessToastMessage(_localizer["ToastReviewApproved"]);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _db.ProductReviews.FindAsync(id);
            if (review == null) return NotFound();

            _db.ProductReviews.Remove(review);
            await _db.SaveChangesAsync();
            _toast.AddSuccessToastMessage(_localizer["ToastReviewDeleted"]);
            return RedirectToAction(nameof(Index));
        }
    }
}
