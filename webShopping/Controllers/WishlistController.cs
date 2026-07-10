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
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public WishlistController(ApplicationDbContext db, IToastNotification toast, IAppLocalizer localizer)
        {
            _db = db;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _db.WishlistItems
                .Where(w => w.UserId == userId)
                .Include(w => w.Product)
                .ThenInclude(p => p!.categoty)
                .OrderByDescending(w => w.AddedAt)
                .ToListAsync();
            return View(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(int productId, string? returnUrl)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var existing = await _db.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
            if (existing != null)
            {
                _db.WishlistItems.Remove(existing);
                _toast.AddInfoToastMessage(_localizer["ToastWishlistRemoved"]);
            }
            else
            {
                _db.WishlistItems.Add(new WishlistItem { UserId = userId, ProductId = productId });
                _toast.AddSuccessToastMessage(_localizer["ToastWishlistAdded"]);
            }
            await _db.SaveChangesAsync();
            return Redirect(returnUrl ?? Url.Action("Details", "Home", new { id = productId })!);
        }
    }
}
