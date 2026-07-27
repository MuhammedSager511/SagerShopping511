using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public UserController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IToastNotification toast,
            IAppLocalizer localizer)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _toast = toast;
            _localizer = localizer;
        }

        public IActionResult Index()
        {
            var users = _context.Users.ToList();
            var roles = _context.Roles.ToList();
            var userRoles = _context.UserRoles.ToList();

            foreach (var item in users)
            {
                var userRoleEntry = userRoles.FirstOrDefault(i => i.UserId == item.Id);
                if (userRoleEntry != null)
                    item.Role = roles.FirstOrDefault(u => u.Id == userRoleEntry.RoleId)?.Name;
            }

            return View(users);
        }

        public async Task<IActionResult> EditRole(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            ViewBag.UserId = id;
            ViewBag.UserEmail = user.Email;
            ViewBag.Roles = new SelectList(
                _roleManager.Roles.Where(r => r.Name != Diger.Role_Birey).Select(r => r.Name),
                currentRoles.FirstOrDefault());

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRole(string id, string role)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);

            if (!string.IsNullOrEmpty(role))
                await _userManager.AddToRoleAsync(user, role);

            _toast.AddSuccessToastMessage(_localizer["ToastUserRoleUpdated"]);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(string id)
        {
            if (id == null) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(m => m.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user != null)
                _context.Users.Remove(user);

            await _context.SaveChangesAsync();
            _toast.AddSuccessToastMessage(_localizer["ToastUserDeleted"]);
            return RedirectToAction(nameof(Index));
        }
    }
}
