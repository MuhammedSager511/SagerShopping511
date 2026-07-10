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
    public class ShippingAdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IShippingService _shipping;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;

        public ShippingAdminController(
            ApplicationDbContext db,
            IShippingService shipping,
            IToastNotification toast,
            IAppLocalizer localizer)
        {
            _db = db;
            _shipping = shipping;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _db.ShippingSettings.OrderBy(s => s.Id).FirstOrDefaultAsync() ?? new ShippingSettings();
            var zones = await _db.ShippingZones.OrderBy(z => z.SortOrder).ThenBy(z => z.Country).ToListAsync();
            ViewBag.Zones = zones;
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveSettings(ShippingSettings model)
        {
            var settings = await _db.ShippingSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (settings == null)
            {
                _db.ShippingSettings.Add(model);
            }
            else
            {
                settings.StoreCountry = model.StoreCountry;
                settings.StoreCity = model.StoreCity;
                settings.FreeShippingSameCity = model.FreeShippingSameCity;
                settings.FreeShippingMinOrderUsd = model.FreeShippingMinOrderUsd;
                settings.DefaultCostUsd = model.DefaultCostUsd;
                settings.DefaultVatRate = model.DefaultVatRate;
                settings.DefaultMinDays = model.DefaultMinDays;
                settings.DefaultMaxDays = model.DefaultMaxDays;
            }

            await _db.SaveChangesAsync();
            _shipping.InvalidateCache();
            _toast.AddSuccessToastMessage(_localizer["ToastShippingSaved"]);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddZone(ShippingZone zone)
        {
            if (string.IsNullOrWhiteSpace(zone.Country))
            {
                _toast.AddErrorToastMessage(_localizer["ShippingZoneCountryRequired"]);
                return RedirectToAction(nameof(Index));
            }

            zone.Country = zone.Country.Trim();
            zone.City = string.IsNullOrWhiteSpace(zone.City) ? null : zone.City.Trim();
            zone.IsActive = true;
            _db.ShippingZones.Add(zone);
            await _db.SaveChangesAsync();
            _shipping.InvalidateCache();
            _toast.AddSuccessToastMessage(_localizer["ToastShippingZoneAdded"]);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> EditZone(int id)
        {
            var zone = await _db.ShippingZones.FindAsync(id);
            if (zone == null) return NotFound();
            return View(zone);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditZone(ShippingZone model)
        {
            var zone = await _db.ShippingZones.FindAsync(model.Id);
            if (zone == null) return NotFound();

            zone.Country = model.Country.Trim();
            zone.City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim();
            zone.ShippingCostUsd = model.ShippingCostUsd;
            zone.VatRate = model.VatRate;
            zone.DeliveryMinDays = model.DeliveryMinDays;
            zone.DeliveryMaxDays = model.DeliveryMaxDays;
            zone.IsFreeShipping = model.IsFreeShipping;
            zone.IsActive = model.IsActive;
            zone.SortOrder = model.SortOrder;

            await _db.SaveChangesAsync();
            _shipping.InvalidateCache();
            _toast.AddSuccessToastMessage(_localizer["ToastShippingZoneSaved"]);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteZone(int id)
        {
            var zone = await _db.ShippingZones.FindAsync(id);
            if (zone != null)
            {
                _db.ShippingZones.Remove(zone);
                await _db.SaveChangesAsync();
                _shipping.InvalidateCache();
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
