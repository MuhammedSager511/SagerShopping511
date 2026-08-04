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

        public IActionResult Index()
        {
            const int take = 12;
            var cacheKey = CatalogCache.HomeShowcase;
            if (!_cache.TryGetValue(cacheKey, out HomePageVm? vm) || vm == null)
            {
                var allProducts = db.Products
                    .Include(p => p.categoty)
                    .Include(p => p.Brand)
                    .OrderByDescending(p => p.Id)
                    .ToList();

                var featured = allProducts.Where(i => i.IsHome).Take(take).ToList();
                if (featured.Count == 0)
                    featured = allProducts.Take(take).ToList();

                var newest = allProducts.Take(take).ToList();

                var bestIds = db.orderDetailses
                    .GroupBy(d => d.productId)
                    .Select(g => new { Id = g.Key, Sold = g.Sum(x => x.count) })
                    .OrderByDescending(x => x.Sold)
                    .Take(take)
                    .Select(x => x.Id)
                    .ToList();
                var bestSellers = allProducts
                    .Where(p => bestIds.Contains(p.Id))
                    .OrderBy(p => bestIds.IndexOf(p.Id))
                    .ToList();
                if (bestSellers.Count == 0)
                    bestSellers = newest.Take(Math.Min(8, newest.Count)).ToList();

                var offers = allProducts
                    .Where(p => p.SalePrice != null && p.SalePrice > 0 && p.SalePrice < p.Price)
                    .Take(take)
                    .ToList();

                var categories = db.Categoties.OrderBy(c => c.SortOrder).ThenBy(c => c.Name).Take(12).ToList();
                var brands = db.Brands.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ThenBy(b => b.Name).Take(12).ToList();
                var banners = db.Banners.Where(b => b.IsActive).OrderBy(b => b.SortOrder).Take(6).ToList();
                var reviews = db.ProductReviews.Where(r => r.IsApproved).OrderByDescending(r => r.CreatedAt).Take(6).ToList();

                vm = new HomePageVm
                {
                    AllProducts = allProducts,
                    Featured = featured,
                    Newest = newest,
                    BestSellers = bestSellers,
                    Offers = offers,
                    Categories = categories,
                    Brands = brands,
                    Banners = banners,
                    Reviews = reviews
                };
                _cache.Set(cacheKey, vm, TimeSpan.FromMinutes(5));
            }

            ViewBag.Categories = vm.Categories;
            ViewBag.Brands = vm.Brands;
            ViewBag.Banners = vm.Banners;
            ViewBag.AllProducts = vm.AllProducts;
            ViewBag.Featured = vm.Featured;
            ViewBag.Newest = vm.Newest;
            ViewBag.BestSellers = vm.BestSellers;
            ViewBag.Offers = vm.Offers;
            ViewBag.HomeReviews = vm.Reviews;
            return View(vm.AllProducts);
        }

        public IActionResult Shop(int page = 1, int pageSize = 12, string sort = "newest", int? categoryId = null, string? q = null, int? brandId = null, double? minPrice = null, double? maxPrice = null, bool? inStock = null, bool? onSale = null)
        {
            page = Math.Max(1, page);
            var query = db.Products.AsNoTracking().Include(p => p.categoty).Include(p => p.Brand).AsQueryable();

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);
            if (brandId.HasValue)
                query = query.Where(p => p.BrandId == brandId);
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(p =>
                    p.Name.Contains(q) || p.NameAr.Contains(q) ||
                    p.Description.Contains(q) || p.DescriptionAr.Contains(q));
            if (minPrice.HasValue)
                query = query.Where(p => (p.SalePrice ?? p.Price) >= minPrice.Value);
            if (maxPrice.HasValue)
                query = query.Where(p => (p.SalePrice ?? p.Price) <= maxPrice.Value);
            if (inStock == true)
                query = query.Where(p => p.StockQuantity > 0);
            if (onSale == true)
                query = query.Where(p => p.SalePrice != null && p.SalePrice > 0 && p.SalePrice < p.Price);

            query = sort switch
            {
                "price_asc" => query.OrderBy(p => p.SalePrice ?? p.Price),
                "price_desc" => query.OrderByDescending(p => p.SalePrice ?? p.Price),
                "name" => query.OrderBy(p => p.Name),
                "discount" => query.OrderByDescending(p => p.Price - (p.SalePrice ?? p.Price)),
                _ => query.OrderByDescending(p => p.Id)
            };

            var totalProducts = query.Count();
            var products = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
            var totalPages = Math.Max(1, (int)Math.Ceiling((double)totalProducts / pageSize));

            ViewBag.KategoryId = categoryId;
            ViewBag.BrandId = brandId;
            ViewBag.Brands = db.Brands.Where(b => b.IsActive).OrderBy(b => b.SortOrder).ToList();
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            ViewData["Sort"] = sort;
            ViewData["Query"] = q;
            ViewData["MinPrice"] = minPrice;
            ViewData["MaxPrice"] = maxPrice;
            ViewData["InStock"] = inStock;
            ViewData["OnSale"] = onSale;
            return View(products);
        }

        public IActionResult Search(string q, int? categoryId = null, string sort = "newest")
            => RedirectToAction(nameof(Shop), new { q, categoryId, sort });

        [HttpGet]
        public IActionResult Suggest(string q)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Trim().Length < 2)
                return Json(Array.Empty<object>());

            var term = q.Trim();
            var items = db.Products.AsNoTracking()
                .Where(p => p.Name.Contains(term) || p.NameAr.Contains(term))
                .OrderByDescending(p => p.Id)
                .Take(8)
                .Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    nameAr = p.NameAr,
                    price = p.SalePrice ?? p.Price,
                    path = p.Path
                })
                .ToList();
            return Json(items);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SubscribeNewsletter(string email)
        {
            if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            {
                toast.AddErrorToastMessage(_localizer["ToastInvalidEmail"]);
                return RedirectToAction(nameof(Index));
            }

            email = email.Trim().ToLowerInvariant();
            if (!db.NewsletterSubscribers.Any(s => s.Email == email))
            {
                db.NewsletterSubscribers.Add(new NewsletterSubscriber { Email = email });
                db.SaveChanges();
            }
            toast.AddSuccessToastMessage(_localizer["ToastSubscribed"]);
            CatalogCache.Invalidate(_cache);
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public IActionResult Contact()
        {
            ViewData["Title"] = _localizer["Contact"];
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Contact(string name, string email, string subject, string message)
        {
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(message))
            {
                toast.AddErrorToastMessage(_localizer["ToastFormIncomplete"]);
                return View();
            }

            db.ContactMessages.Add(new ContactMessage
            {
                Name = name.Trim(),
                Email = email.Trim(),
                Subject = (subject ?? "").Trim(),
                Message = message.Trim()
            });
            db.SaveChanges();
            toast.AddSuccessToastMessage(_localizer["ToastMessageSent"]);
            return RedirectToAction(nameof(Contact));
        }

        public IActionResult Faq()
        {
            ViewData["Title"] = _localizer["Faqs"];
            var items = db.FaqItems.Where(f => f.IsActive).OrderBy(f => f.SortOrder).ToList();
            return View(items);
        }

        public IActionResult CategoryDetails(int id)
            => RedirectToAction(nameof(Shop), new { categoryId = id });

        public IActionResult Details(int Id)
        {
            var product = db.Products
                .Include(p => p.categoty)
                .Include(p => p.Brand)
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
            ViewBag.Similar = db.Products.Include(p => p.categoty)
                .Where(p => p.CategoryId == product.CategoryId && p.Id != product.Id)
                .OrderByDescending(p => p.Id).Take(4).ToList();
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
            if (product == null || product.StockQuantity <= 0)
            {
                toast.AddErrorToastMessage(_localizer["ToastProductUnavailable"]);
                return RedirectToAction(nameof(Details), new { id = Scart.ProductId });
            }

            var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (claim == null)
                return Challenge();

            var existing = db.ShoppingCarts.FirstOrDefault(
                u => u.ApplicationUserId == claim && u.ProductId == Scart.ProductId);

            var alreadyInCart = existing?.Count ?? 0;
            var requestedTotal = alreadyInCart + Scart.Count;
            if (requestedTotal > product.StockQuantity)
            {
                toast.AddWarningToastMessage(_localizer["ToastInsufficientStock"]);
                return RedirectToAction(nameof(Details), new { id = Scart.ProductId });
            }

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
        public IActionResult Error(string? rid, [FromServices] IConfiguration config)
        {
            var requestId = rid ?? Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            var cached = RecentErrors.Get(requestId) ?? RecentErrors.Get(rid);

            var showDetails = config.GetValue("Diagnostics:ShowErrorDetails", false);
            return View(new ErrorViewModel
            {
                RequestId = requestId,
                Path = cached?.Path,
                ExceptionType = cached?.ExceptionType,
                ExceptionMessage = cached?.ExceptionMessage,
                StackTrace = cached?.StackTrace,
                ShowDetails = showDetails && !string.IsNullOrWhiteSpace(cached?.ExceptionMessage)
            });
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
