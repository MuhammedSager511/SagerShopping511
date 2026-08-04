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
    public class CategotiesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IToastNotification toast;
        private readonly IAppLocalizer _localizer;
        private readonly IImageUploadService _images;
        private readonly IMemoryCache _cache;

        public CategotiesController(
            ApplicationDbContext context,
            IToastNotification toast,
            IAppLocalizer localizer,
            IImageUploadService images,
            IMemoryCache cache)
        {
            _context = context;
            this.toast = toast;
            _localizer = localizer;
            _images = images;
            _cache = cache;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _context.Categoties.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).ToListAsync());
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var categoty = await _context.Categoties.FirstOrDefaultAsync(m => m.Id == id);
            return categoty == null ? NotFound() : View(categoty);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Categoty categoty)
        {
            ModelState.Remove(nameof(Categoty.Image));
            ModelState.Remove(nameof(Categoty.ImagePath));

            categoty.NameAr ??= "";
            categoty.ImagePath = "";

            if (categoty.Image != null && categoty.Image.Length > 0)
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(categoty.Image, "category");
                    categoty.ImagePath = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Categoty.Image), ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                _context.Add(categoty);
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                toast.AddSuccessToastMessage(_localizer["ToastCategoryAdded"]);
                return RedirectToAction(nameof(Index));
            }

            return View(categoty);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();

            var categoty = await _context.Categoties.FindAsync(id);
            return categoty == null ? NotFound() : View(categoty);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Categoty categoty)
        {
            if (id != categoty.Id)
                return NotFound();

            ModelState.Remove(nameof(Categoty.Image));
            ModelState.Remove(nameof(Categoty.ImagePath));

            var tracked = await _context.Categoties.FindAsync(id);
            if (tracked == null)
                return NotFound();

            if (categoty.Image != null && categoty.Image.Length > 0)
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(categoty.Image, "category");
                    tracked.ImagePath = fileName;
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError(nameof(Categoty.Image), ex.Message);
                }
            }

            if (ModelState.IsValid)
            {
                tracked.Name = categoty.Name;
                tracked.NameAr = categoty.NameAr ?? "";
                tracked.SortOrder = categoty.SortOrder;
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                toast.AddSuccessToastMessage(_localizer["ToastCategoryUpdated"]);
                return RedirectToAction(nameof(Index));
            }

            categoty.ImagePath = tracked.ImagePath;
            return View(categoty);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();

            var categoty = await _context.Categoties.FirstOrDefaultAsync(m => m.Id == id);
            return categoty == null ? NotFound() : View(categoty);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var categoty = await _context.Categoties.FindAsync(id);
            if (categoty != null)
                _context.Categoties.Remove(categoty);

            await _context.SaveChangesAsync();
            CatalogCache.Invalidate(_cache);
            toast.AddSuccessToastMessage(_localizer["ToastCategoryDeleted"]);
            return RedirectToAction(nameof(Index));
        }
    }
}
