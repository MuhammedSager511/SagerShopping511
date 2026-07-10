using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
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
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public ProfileController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            IToastNotification toast,
            IAppLocalizer localizer)
        {
            _db = db;
            _userManager = userManager;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            ViewBag.OrderCount = _db.OrderHeaders.Count(o => o.ApplicationUserId == user.Id);
            ViewBag.CartCount = _db.ShoppingCarts.Count(c => c.ApplicationUserId == user.Id);
            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ApplicationUser model)
        {
            var user = await GetCurrentUserAsync();
            if (user == null) return Challenge();

            user.Name = model.Name;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Addres = model.Addres;
            user.City = model.City;
            user.Country = model.Country;
            user.PostaKodu = model.PostaKodu;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _toast.AddSuccessToastMessage(_localizer["ToastProfileUpdated"]);
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            ViewBag.OrderCount = _db.OrderHeaders.Count(o => o.ApplicationUserId == user.Id);
            ViewBag.CartCount = _db.ShoppingCarts.Count(c => c.ApplicationUserId == user.Id);
            return View(user);
        }

        private async Task<ApplicationUser?> GetCurrentUserAsync()
        {
            var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return id == null ? null : await _userManager.FindByIdAsync(id);
        }
    }
}
