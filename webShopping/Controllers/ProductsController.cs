using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NToastNotify;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;
        private readonly IImageUploadService _images;
        private readonly IMemoryCache _cache;

        public ProductsController(
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
            var products = await _context.Products.AsNoTracking().Include(f => f.categoty).ToListAsync();
            return View(products);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .AsNoTracking()
                .Include(f => f.categoty)
                .Include(f => f.Images)
                .FirstOrDefaultAsync(m => m.Id == id);
            return product == null ? NotFound() : View(product);
        }

        public IActionResult Create()
        {
            if (!_context.Categoties.Any())
            {
                _toast.AddWarningToastMessage(_localizer["ToastCategoryRequired"]);
                return RedirectToAction("Index", "Categoties");
            }
            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name");
            ViewData["BrandId"] = new SelectList(_context.Brands.Where(b => b.IsActive), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product fileDetails, List<IFormFile>? galleryFiles)
        {
            ModelState.Remove(nameof(Product.FileType));
            ModelState.Remove(nameof(Product.File));
            ModelState.Remove(nameof(Product.categoty));
            ModelState.Remove(nameof(Product.Path));
            ModelState.Remove(nameof(Product.Images));
            ModelState.Remove(nameof(Product.GalleryFiles));
            ModelState.Remove(nameof(Product.Brand));

            if (fileDetails.CategoryId <= 0 || !_context.Categoties.Any(c => c.Id == fileDetails.CategoryId))
                ModelState.AddModelError(nameof(Product.CategoryId), _localizer["SelectCategory"]);

            if (ModelState.IsValid)
            {
                try
                {
                    await SaveProductImagesAsync(fileDetails, galleryFiles);
                    fileDetails.NameAr ??= "";
                    fileDetails.Description ??= "";
                    fileDetails.DescriptionAr ??= "";
                    fileDetails.SyncStockFlag();
                    _context.Add(fileDetails);
                    await _context.SaveChangesAsync();
                    CatalogCache.Invalidate(_cache);
                    _toast.AddSuccessToastMessage(_localizer["ToastProductSaved"]);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }

            _toast.AddErrorToastMessage(_localizer["ToastProductError"]);
            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", fileDetails.CategoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands.Where(b => b.IsActive), "Id", "Name", fileDetails.BrandId);
            return View(fileDetails);
        }

        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product == null) return NotFound();
            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", product.CategoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands.Where(b => b.IsActive), "Id", "Name", product.BrandId);
            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Product fileDetails, List<IFormFile>? galleryFiles)
        {
            if (id != fileDetails.Id) return NotFound();

            ModelState.Remove(nameof(Product.FileType));
            ModelState.Remove(nameof(Product.File));
            ModelState.Remove(nameof(Product.categoty));
            ModelState.Remove(nameof(Product.Path));
            ModelState.Remove(nameof(Product.Images));
            ModelState.Remove(nameof(Product.GalleryFiles));
            ModelState.Remove(nameof(Product.Brand));

            if (fileDetails.CategoryId <= 0 || !_context.Categoties.Any(c => c.Id == fileDetails.CategoryId))
                ModelState.AddModelError(nameof(Product.CategoryId), _localizer["SelectCategory"]);

            if (ModelState.IsValid)
            {
                try
                {
                    var tracked = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
                    if (tracked == null) return NotFound();

                    tracked.Name = fileDetails.Name;
                    tracked.NameAr = fileDetails.NameAr ?? "";
                    tracked.Description = fileDetails.Description ?? "";
                    tracked.DescriptionAr = fileDetails.DescriptionAr ?? "";
                    tracked.Price = fileDetails.Price;
                    tracked.SalePrice = fileDetails.SalePrice;
                    tracked.Colors = fileDetails.Colors ?? "";
                    tracked.Sizes = fileDetails.Sizes ?? "";
                    tracked.BrandId = fileDetails.BrandId;
                    tracked.CategoryId = fileDetails.CategoryId;
                    tracked.IsHome = fileDetails.IsHome;
                    tracked.StockQuantity = Math.Max(0, fileDetails.StockQuantity);
                    tracked.SyncStockFlag();

                    if (fileDetails.File != null && fileDetails.File.Length > 0)
                    {
                        tracked.File = fileDetails.File;
                        await SaveProductImagesAsync(tracked, null);
                    }

                    await SaveProductImagesAsync(tracked, galleryFiles, tracked.Images.ToList());
                    await _context.SaveChangesAsync();
                    CatalogCache.Invalidate(_cache);
                    _toast.AddSuccessToastMessage(_localizer["ToastProductUpdated"]);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", ex.Message);
                    _toast.AddErrorToastMessage(_localizer["ToastProductError"]);
                }
            }

            ViewData["CategoryId"] = new SelectList(_context.Categoties, "Id", "Name", fileDetails.CategoryId);
            ViewData["BrandId"] = new SelectList(_context.Brands.Where(b => b.IsActive), "Id", "Name", fileDetails.BrandId);
            return View(fileDetails);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();
            var product = await _context.Products
                .Include(f => f.categoty)
                .FirstOrDefaultAsync(m => m.Id == id);
            return product == null ? NotFound() : View(product);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var product = await _context.Products.Include(p => p.Images).FirstOrDefaultAsync(p => p.Id == id);
            if (product != null)
            {
                DeleteFile(product.Path);
                foreach (var img in product.Images)
                    DeleteFile(img.Path);
                _context.Products.Remove(product);
                await _context.SaveChangesAsync();
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["ToastProductDeleted"]);
            }
            return RedirectToAction(nameof(Index));
        }

        private async Task SaveProductImagesAsync(Product product, List<IFormFile>? galleryFiles, List<ProductImage>? existingImages = null)
        {
            if (product.File != null && product.File.Length > 0)
            {
                var (fileName, contentType) = await _images.SaveProductImageAsync(product.File);
                product.Path = fileName;
                product.FileType = contentType.Replace("image/", "");
            }

            if (galleryFiles == null || galleryFiles.Count == 0)
                return;

            var startOrder = existingImages?.Count ?? product.Images?.Count ?? 0;
            foreach (var file in galleryFiles.Where(f => f.Length > 0))
            {
                var (fileName, _) = await _images.SaveProductImageAsync(file, "gallery");
                if (product.Id > 0)
                {
                    _context.ProductImages.Add(new ProductImage
                    {
                        ProductId = product.Id,
                        Path = fileName,
                        SortOrder = startOrder++
                    });
                }
                else
                {
                    product.Images.Add(new ProductImage { Path = fileName, SortOrder = startOrder++ });
                }
            }
        }

        private void DeleteFile(string? path)
        {
            if (string.IsNullOrEmpty(path)) return;
            var filePath = Path.Combine(_env.WebRootPath, "files", path);
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImage(int imageId, int productId)
        {
            var img = await _context.ProductImages.FindAsync(imageId);
            if (img == null || img.ProductId != productId)
                return NotFound();

            DeleteFile(img.Path);
            _context.ProductImages.Remove(img);
            await _context.SaveChangesAsync();
            CatalogCache.Invalidate(_cache);
            _toast.AddSuccessToastMessage(_localizer["ToastImageDeleted"]);
            return RedirectToAction(nameof(Edit), new { id = productId });
        }
    }
}
