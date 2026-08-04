using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NToastNotify;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class BrandsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;
        private readonly IImageUploadService _images;
        private readonly IMemoryCache _cache;

        public BrandsController(
            ApplicationDbContext context,
            IWebHostEnvironment env,
            IToastNotification toast,
            IAppLocalizer localizer,
            IImageUploadService images,
            IMemoryCache cache)
        {
            _context = context;
            _env = env;
            _toast = toast;
            _localizer = localizer;
            _images = images;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            var brands = await _context.Brands.AsNoTracking()
                .OrderBy(b => b.SortOrder).ThenBy(b => b.Name)
                .ToListAsync();
            return View(brands);
        }

        public IActionResult Create() => View(new Brand { IsActive = true });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Brand brand)
        {
            ModelState.Remove(nameof(Brand.Image));
            ModelState.Remove(nameof(Brand.ImagePath));

            brand.NameAr ??= "";
            brand.ImagePath = "";

            if (brand.Image != null && brand.Image.Length > 0)
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(brand.Image, "brand");
                    brand.ImagePath = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Brand.Image), ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(brand);
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryAdded"]);
                return RedirectToAction(nameof(Index));
            }

            return View(brand);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var brand = await _context.Brands.FindAsync(id);
            return brand == null ? NotFound() : View(brand);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Brand brand)
        {
            if (id != brand.Id) return NotFound();

            ModelState.Remove(nameof(Brand.Image));
            ModelState.Remove(nameof(Brand.ImagePath));

            var tracked = await _context.Brands.FindAsync(id);
            if (tracked == null) return NotFound();

            if (brand.Image != null && brand.Image.Length > 0)
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(brand.Image, "brand");
                    tracked.ImagePath = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Brand.Image), ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                tracked.Name = brand.Name;
                tracked.NameAr = brand.NameAr ?? "";
                tracked.SortOrder = brand.SortOrder;
                tracked.IsActive = brand.IsActive;
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryUpdated"]);
                return RedirectToAction(nameof(Index));
            }

            brand.ImagePath = tracked.ImagePath;
            return View(brand);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var brand = await _context.Brands.FirstOrDefaultAsync(m => m.Id == id);
            return brand == null ? NotFound() : View(brand);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var brand = await _context.Brands.FindAsync(id);
            if (brand != null)
            {
                if (!string.IsNullOrEmpty(brand.ImagePath))
                {
                    var filePath = Path.Combine(_env.WebRootPath, "files", brand.ImagePath);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                _context.Brands.Remove(brand);
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
