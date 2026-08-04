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
    public class BannersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;
        private readonly IImageUploadService _images;
        private readonly IMemoryCache _cache;

        public BannersController(
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
            var banners = await _context.Banners.AsNoTracking()
                .OrderBy(b => b.SortOrder)
                .ToListAsync();
            return View(banners);
        }

        public IActionResult Create() => View(new Banner { IsActive = true, LinkUrl = "/Home/Shop" });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Banner banner)
        {
            ModelState.Remove(nameof(Banner.Image));
            ModelState.Remove(nameof(Banner.ImagePath));

            banner.TitleAr ??= "";
            banner.SubtitleEn ??= "";
            banner.SubtitleAr ??= "";
            banner.LinkUrl = string.IsNullOrWhiteSpace(banner.LinkUrl) ? "/Home/Shop" : banner.LinkUrl;
            banner.ImagePath = "";

            if (banner.Image != null && banner.Image.Length > 0)
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(banner.Image, "banner");
                    banner.ImagePath = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Banner.Image), ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError(nameof(Banner.Image), _localizer["ImageOptional"]);
            }

            if (ModelState.IsValid)
            {
                _context.Add(banner);
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryAdded"]);
                return RedirectToAction(nameof(Index));
            }

            return View(banner);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FindAsync(id);
            return banner == null ? NotFound() : View(banner);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Banner banner)
        {
            if (id != banner.Id) return NotFound();

            ModelState.Remove(nameof(Banner.Image));
            ModelState.Remove(nameof(Banner.ImagePath));

            var tracked = await _context.Banners.FindAsync(id);
            if (tracked == null) return NotFound();

            if (banner.Image != null && banner.Image.Length > 0)
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(banner.Image, "banner");
                    tracked.ImagePath = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Banner.Image), ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                tracked.TitleEn = banner.TitleEn;
                tracked.TitleAr = banner.TitleAr ?? "";
                tracked.SubtitleEn = banner.SubtitleEn ?? "";
                tracked.SubtitleAr = banner.SubtitleAr ?? "";
                tracked.LinkUrl = string.IsNullOrWhiteSpace(banner.LinkUrl) ? "/Home/Shop" : banner.LinkUrl;
                tracked.SortOrder = banner.SortOrder;
                tracked.IsActive = banner.IsActive;
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryUpdated"]);
                return RedirectToAction(nameof(Index));
            }

            banner.ImagePath = tracked.ImagePath;
            return View(banner);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var banner = await _context.Banners.FirstOrDefaultAsync(m => m.Id == id);
            return banner == null ? NotFound() : View(banner);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var banner = await _context.Banners.FindAsync(id);
            if (banner != null)
            {
                if (!string.IsNullOrEmpty(banner.ImagePath))
                {
                    var filePath = Path.Combine(_env.WebRootPath, "files", banner.ImagePath);
                    if (System.IO.File.Exists(filePath))
                        System.IO.File.Delete(filePath);
                }
                _context.Banners.Remove(banner);
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
