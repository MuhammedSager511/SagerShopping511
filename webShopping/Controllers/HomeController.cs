using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using NToastNotify;
using System.Diagnostics;
using System.Security.Claims;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext db;
        private readonly IToastNotification toast;
        private readonly IAppLocalizer _localizer;
        private readonly IMemoryCache _cache;
        private readonly INotificationService _notifications;

        public HomeController(
            ILogger<HomeController> logger,
            ApplicationDbContext context,
            IToastNotification toast,
            IAppLocalizer localizer,
            IMemoryCache cache,
            INotificationService notifications)
        {
            _logger = logger;
            db = context;
            this.toast = toast;
            _localizer = localizer;
            _cache = cache;
            _notifications = notifications;
        }

        public IActionResult Index(int page = 1, int pageSize = 8)
        {
            var cacheKey = $"featured_products_p{page}_s{pageSize}";
            if (!_cache.TryGetValue(cacheKey, out List<Product>? products))
            {
                products = db.Products
                    .Include(p => p.categoty)
                    .Where(i => i.IsHome)
                    .OrderByDescending(p => p.Id)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();
                _cache.Set(cacheKey, products, TimeSpan.FromMinutes(5));
            }

            var totalProducts = db.Products.Count(i => i.IsHome);
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalProducts / pageSize));

            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            return View(products);
        }

        public IActionResult Shop(int page = 1, int pageSize = 12, string sort = "newest", int? categoryId = null, string? q = null)
        {
            var cacheKey = $"shop_p{page}_s{pageSize}_{sort}_c{categoryId}_q{q ?? ""}";
            if (!_cache.TryGetValue(cacheKey, out (List<Product> products, int totalProducts) cached))
            {
                var query = db.Products.Include(p => p.categoty).AsQueryable();

                if (categoryId.HasValue)
                    query = query.Where(p => p.CategoryId == categoryId);

                if (!string.IsNullOrWhiteSpace(q))
                    query = query.Where(p =>
                        p.Name.Contains(q) || p.NameAr.Contains(q) ||
                        p.Description.Contains(q) || p.DescriptionAr.Contains(q));

                query = sort switch
                {
                    "price_asc" => query.OrderBy(p => p.Price),
                    "price_desc" => query.OrderByDescending(p => p.Price),
                    "name" => query.OrderBy(p => p.Name),
                    _ => query.OrderByDescending(p => p.Id)
                };

                var totalProducts = query.Count();
                var products = query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                cached = (products, totalProducts);
                _cache.Set(cacheKey, cached, TimeSpan.FromMinutes(5));
            }

            var totalPages = Math.Max(1, (int)Math.Ceiling((double)cached.totalProducts / pageSize));

            ViewBag.KategoryId = categoryId;
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["Sort"] = sort;
            ViewData["Query"] = q;
            return View(cached.products);
        }

        public IActionResult Search(string q)
        {
            if (!string.IsNullOrWhiteSpace(q))
            {
                var results = db.Products
                    .Include(p => p.categoty)
                    .Where(i => i.Name.Contains(q) || i.NameAr.Contains(q) || i.Description.Contains(q) || i.DescriptionAr.Contains(q))
                    .ToList();
                ViewData["Query"] = q;
                return View(results);
            }

            return View(Enumerable.Empty<Product>());
        }

        public IActionResult CategoryDetails(int id)
        {
            var product = db.Products
                .Include(p => p.categoty)
                .Where(i => i.CategoryId == id)
                .ToList();
            ViewBag.KategoryId = id;
            return View(product);
        }

        public IActionResult Details(int Id)
        {
            var product = db.Products
                .Include(p => p.categoty)
                .Include(p => p.Images.OrderBy(i => i.SortOrder))
                .FirstOrDefault(i => i.Id == Id);

            if (product == null)
                return NotFound();

            var reviews = db.ProductReviews
                .Where(r => r.ProductId == Id && r.IsApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();

            ViewBag.Reviews = reviews;
            ViewBag.AvgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            ViewData["MetaDescription"] = LocalizedContent.ProductDescription(product, _localizer);

            var cart = new ShoppingCart
            {
                Product = product,
                ProductId = product.Id,
                Count = 1
            };

            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart Scart)
        {
            ModelState.Remove(nameof(Scart.ApplicationUser));
            ModelState.Remove(nameof(Scart.Product));
            ModelState.Remove(nameof(Scart.ApplicationUserId));
            ModelState.Remove(nameof(Scart.Price));

            if (Scart.ProductId <= 0)
            {
                toast.AddErrorToastMessage(_localizer["ToastProductUnavailable"]);
                return RedirectToAction(nameof(Shop));
            }

            if (Scart.Count < 1)
                Scart.Count = 1;

            var product = db.Products.Find(Scart.ProductId);
            if (product == null || !product.IsStock)
            {
                toast.AddErrorToastMessage(_localizer["ToastProductUnavailable"]);
                return RedirectToAction(nameof(Details), new { id = Scart.ProductId });
            }

            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claim == null)
                return Challenge();

            var existing = db.ShoppingCarts.FirstOrDefault(
                u => u.ApplicationUserId == claim && u.ProductId == Scart.ProductId);

            if (existing == null)
            {
                db.ShoppingCarts.Add(new ShoppingCart
                {
                    ApplicationUserId = claim,
                    ProductId = Scart.ProductId,
                    Count = Scart.Count
                });
            }
            else
            {
                existing.Count += Scart.Count;
            }

            db.SaveChanges();
            toast.AddSuccessToastMessage(_localizer["ToastProductAdded"]);

            var count = db.ShoppingCarts.Count(i => i.ApplicationUserId == claim);
            HttpContext.Session.SetInt32(Diger.ssShoppingCart, count);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> AddReview(int productId, int rating, string comment)
        {
            rating = Math.Clamp(rating, 1, 5);
            if (string.IsNullOrWhiteSpace(comment))
            {
                toast.AddErrorToastMessage(_localizer["ReviewCommentRequired"]);
                return RedirectToAction(nameof(Details), new { id = productId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var product = db.Products.AsNoTracking().FirstOrDefault(p => p.Id == productId);
            db.ProductReviews.Add(new ProductReview
            {
                ProductId = productId,
                UserId = userId,
                UserName = User.Identity?.Name ?? "User",
                Rating = rating,
                Comment = comment.Trim(),
                IsApproved = false
            });
            db.SaveChanges();

            var productName = product?.Name ?? productId.ToString();
            await _notifications.NotifyAdminsAsync(
                "NotifAdminNewReviewTitle",
                "NotifAdminNewReviewMessage",
                new Dictionary<string, string>
                {
                    ["ProductName"] = productName,
                    ["UserName"] = User.Identity?.Name ?? "User"
                },
                "/Reviews",
                "review");

            toast.AddSuccessToastMessage(_localizer["ToastReviewPending"]);
            return RedirectToAction(nameof(Details), new { id = productId });
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        public IActionResult About() => View();

        public IActionResult Terms()
        {
            ViewData["Title"] = _localizer["TermsOfUse"];
            return View();
        }

        public IActionResult Refund()
        {
            ViewData["Title"] = _localizer["RefundPolicy"];
            return View();
        }

        public IActionResult Privacy()
        {
            ViewData["Title"] = _localizer["PrivacyPolicy"];
            return View();
        }
    }
}
