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
    public class CouponsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public CouponsController(ApplicationDbContext context, IToastNotification toast, IAppLocalizer localizer)
        {
            _context = context;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var coupons = await _context.Coupons.AsNoTracking()
                .OrderByDescending(c => c.Id)
                .ToListAsync();
            return View(coupons);
        }

        public IActionResult Create() => View(new Coupon { IsActive = true, IsPercent = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Coupon coupon)
        {
            coupon.TitleEn ??= "";
            coupon.TitleAr ??= "";
            coupon.Code = (coupon.Code ?? "").Trim().ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(coupon.Code))
                ModelState.AddModelError(nameof(Coupon.Code), "Coupon code is required.");
            else if (await _context.Coupons.AnyAsync(c => c.Code == coupon.Code))
                ModelState.AddModelError(nameof(Coupon.Code), "This coupon code already exists.");

            if (ModelState.IsValid)
            {
                _context.Add(coupon);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryAdded"]);
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var coupon = await _context.Coupons.FindAsync(id);
            return coupon == null ? NotFound() : View(coupon);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Coupon coupon)
        {
            if (id != coupon.Id) return NotFound();

            var tracked = await _context.Coupons.FindAsync(id);
            if (tracked == null) return NotFound();

            var code = (coupon.Code ?? "").Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(code))
                ModelState.AddModelError(nameof(Coupon.Code), "Coupon code is required.");
            else if (await _context.Coupons.AnyAsync(c => c.Code == code && c.Id != id))
                ModelState.AddModelError(nameof(Coupon.Code), "This coupon code already exists.");

            if (ModelState.IsValid)
            {
                tracked.Code = code;
                tracked.TitleEn = coupon.TitleEn ?? "";
                tracked.TitleAr = coupon.TitleAr ?? "";
                tracked.Value = coupon.Value;
                tracked.IsPercent = coupon.IsPercent;
                tracked.MinOrderUsd = coupon.MinOrderUsd;
                tracked.MaxUses = coupon.MaxUses;
                tracked.StartsAt = coupon.StartsAt;
                tracked.EndsAt = coupon.EndsAt;
                tracked.IsActive = coupon.IsActive;
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryUpdated"]);
                return RedirectToAction(nameof(Index));
            }

            return View(coupon);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var coupon = await _context.Coupons.FirstOrDefaultAsync(m => m.Id == id);
            return coupon == null ? NotFound() : View(coupon);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var coupon = await _context.Coupons.FindAsync(id);
            if (coupon != null)
            {
                _context.Coupons.Remove(coupon);
                await _context.SaveChangesAsync();
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
