using System.Text.Json;
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
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;
        private readonly IMemoryCache _cache;

        public AdminController(
            ApplicationDbContext db,
            IToastNotification toast,
            IAppLocalizer localizer,
            IMemoryCache cache)
        {
            _db = db;
            _toast = toast;
            _localizer = localizer;
            _cache = cache;
        }

        public IActionResult Index()
        {
            ViewBag.TotalProducts = _db.Products.Count();
            ViewBag.TotalCategories = _db.Categoties.Count();
            ViewBag.TotalUsers = _db.Users.Count();
            ViewBag.TotalOrders = _db.OrderHeaders.Count();
            ViewBag.PendingOrders = _db.OrderHeaders.Count(o =>
                o.orderStatus == Diger.status_pending ||
                o.orderStatus == Diger.status_awaiting_payment ||
                o.orderStatus == Diger.status_payment_review);
            ViewBag.ConfirmedOrders = _db.OrderHeaders.Count(o => o.orderStatus == Diger.status_confirmed);
            ViewBag.CargoOrders = _db.OrderHeaders.Count(o => o.orderStatus == Diger.status_cargo);
            ViewBag.LowStock = _db.Products.Count(p => p.StockQuantity <= StockHelper.LimitedThreshold);
            ViewBag.Revenue = _db.OrderHeaders
                .Where(o => o.orderStatus == Diger.status_confirmed
                    || o.orderStatus == Diger.status_cargo
                    || o.orderStatus == Diger.status_preparing)
                .Sum(o => (double?)o.orderTotal) ?? 0;

            ViewBag.RecentOrders = _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.orderDate)
                .Take(5)
                .ToList();

            ViewBag.TopProducts = _db.orderDetailses
                .GroupBy(d => d.productId)
                .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.count) })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToList()
                .Select(x => new
                {
                    Product = _db.Products.Find(x.ProductId),
                    x.Sold
                })
                .Where(x => x.Product != null)
                .ToList();

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ExportBackup(CancellationToken cancellationToken)
        {
            var products = await _db.Products.AsNoTracking()
                .Include(p => p.Images)
                .OrderBy(p => p.Id)
                .ToListAsync(cancellationToken);
            var categories = await _db.Categoties.AsNoTracking().OrderBy(c => c.Id).ToListAsync(cancellationToken);
            var orders = await _db.OrderHeaders.AsNoTracking()
                .OrderByDescending(o => o.Id)
                .Take(2000)
                .ToListAsync(cancellationToken);
            var orderIds = orders.Select(o => o.Id).ToList();
            var orderLines = await _db.orderDetailses.AsNoTracking()
                .Where(d => orderIds.Contains(d.OrederId))
                .ToListAsync(cancellationToken);
            var settings = await _db.SiteSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            var banks = await _db.BankAccounts.AsNoTracking().OrderBy(b => b.SortOrder).ToListAsync(cancellationToken);
            var links = await _db.QuickLinks.AsNoTracking().OrderBy(l => l.SortOrder).ToListAsync(cancellationToken);

            var payload = new
            {
                exportedAtUtc = DateTime.UtcNow,
                app = "SagerShop",
                version = 1,
                note = "Application data export. Passwords and identity secrets are not included. Use host SQL tools for full DB restore.",
                categories,
                products = products.Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.NameAr,
                    p.Description,
                    p.DescriptionAr,
                    p.Price,
                    p.SalePrice,
                    p.Colors,
                    p.Sizes,
                    p.BrandId,
                    p.Path,
                    p.IsHome,
                    p.IsStock,
                    p.StockQuantity,
                    p.CategoryId,
                    Images = p.Images?.Select(i => new { i.Id, i.Path, i.SortOrder })
                }),
                orders = orders.Select(o => new
                {
                    o.Id,
                    o.Name,
                    o.LastName,
                    o.PhoneNumber,
                    o.Addres,
                    o.sehir,
                    o.Semt,
                    o.Country,
                    o.PostKodu,
                    o.orderDate,
                    o.orderTotal,
                    o.orderStatus,
                    o.PaymentMethod,
                    o.TrackingNumber,
                    o.SubtotalUsd,
                    o.ShippingUsd,
                    o.VatUsd,
                    o.DiscountUsd,
                    o.CouponCode
                }),
                orderDetails = orderLines.Select(d => new
                {
                    d.Id,
                    OrderId = d.OrederId,
                    d.productId,
                    d.count,
                    d.Price
                }),
                settings,
                bankAccounts = banks,
                quickLinks = links
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            var fileName = $"sagershop-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json";
            return File(bytes, "application/json", fileName);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(32 * 1024 * 1024)]
        public async Task<IActionResult> ImportBackup(IFormFile? file, CancellationToken cancellationToken)
        {
            if (file == null || file.Length == 0 || !file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                _toast.AddErrorToastMessage(_localizer["BackupImportFailed"]);
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await using var stream = file.OpenReadStream();
                using var doc = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
                var root = doc.RootElement;

                if (root.TryGetProperty("app", out var appEl) &&
                    appEl.GetString() is { } appName &&
                    !appName.Equals("SagerShop", StringComparison.OrdinalIgnoreCase))
                {
                    _toast.AddErrorToastMessage(_localizer["BackupImportFailed"]);
                    return RedirectToAction(nameof(Index));
                }

                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);

                if (root.TryGetProperty("categories", out var catsEl) && catsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var c in catsEl.EnumerateArray())
                    {
                        var name = c.TryGetProperty("Name", out var n) ? n.GetString() ?? "" : "";
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var id = c.TryGetProperty("Id", out var idEl) ? idEl.GetInt32() : 0;
                        var existing = id > 0
                            ? await _db.Categoties.FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                            : await _db.Categoties.FirstOrDefaultAsync(x => x.Name == name, cancellationToken);

                        if (existing == null)
                        {
                            existing = new Categoty { Name = name };
                            _db.Categoties.Add(existing);
                        }

                        existing.Name = name;
                        existing.NameAr = c.TryGetProperty("NameAr", out var na) ? na.GetString() ?? "" : existing.NameAr;
                        existing.ImagePath = c.TryGetProperty("ImagePath", out var ip) ? ip.GetString() ?? "" : existing.ImagePath;
                        existing.SortOrder = c.TryGetProperty("SortOrder", out var so) ? so.GetInt32() : existing.SortOrder;
                    }
                    await _db.SaveChangesAsync(cancellationToken);
                }

                if (root.TryGetProperty("products", out var productsEl) && productsEl.ValueKind == JsonValueKind.Array)
                {
                    foreach (var p in productsEl.EnumerateArray())
                    {
                        var name = p.TryGetProperty("Name", out var pn) ? pn.GetString() ?? "" : "";
                        if (string.IsNullOrWhiteSpace(name)) continue;
                        var categoryId = p.TryGetProperty("CategoryId", out var cid) ? cid.GetInt32() : 0;
                        if (categoryId <= 0 || !await _db.Categoties.AnyAsync(c => c.Id == categoryId, cancellationToken))
                        {
                            categoryId = await _db.Categoties.Select(c => c.Id).FirstOrDefaultAsync(cancellationToken);
                            if (categoryId == 0) continue;
                        }

                        var id = p.TryGetProperty("Id", out var pid) ? pid.GetInt32() : 0;
                        var existing = id > 0
                            ? await _db.Products.Include(x => x.Images).FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                            : null;

                        if (existing == null)
                        {
                            existing = new Product { Name = name, CategoryId = categoryId, Price = 0.01 };
                            _db.Products.Add(existing);
                        }

                        existing.Name = name;
                        existing.NameAr = p.TryGetProperty("NameAr", out var pna) ? pna.GetString() ?? "" : existing.NameAr;
                        existing.Description = p.TryGetProperty("Description", out var pd) ? pd.GetString() ?? "" : existing.Description;
                        existing.DescriptionAr = p.TryGetProperty("DescriptionAr", out var pda) ? pda.GetString() ?? "" : existing.DescriptionAr;
                        if (p.TryGetProperty("Price", out var pr) && pr.TryGetDouble(out var price)) existing.Price = price;
                        if (p.TryGetProperty("SalePrice", out var sp) && sp.ValueKind != JsonValueKind.Null && sp.TryGetDouble(out var sale))
                            existing.SalePrice = sale;
                        existing.Colors = p.TryGetProperty("Colors", out var col) ? col.GetString() ?? "" : existing.Colors;
                        existing.Sizes = p.TryGetProperty("Sizes", out var sz) ? sz.GetString() ?? "" : existing.Sizes;
                        if (p.TryGetProperty("BrandId", out var bid) && bid.ValueKind != JsonValueKind.Null && bid.TryGetInt32(out var brandId))
                            existing.BrandId = brandId;
                        existing.Path = p.TryGetProperty("Path", out var path) ? path.GetString() : existing.Path;
                        existing.IsHome = p.TryGetProperty("IsHome", out var ih) && ih.ValueKind == JsonValueKind.True;
                        if (p.TryGetProperty("StockQuantity", out var sq) && sq.TryGetInt32(out var stock))
                            existing.StockQuantity = Math.Max(0, stock);
                        existing.CategoryId = categoryId;
                        existing.SyncStockFlag();

                        if (p.TryGetProperty("Images", out var imgs) && imgs.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var img in imgs.EnumerateArray())
                            {
                                var imgPath = img.TryGetProperty("Path", out var ipath) ? ipath.GetString() : null;
                                if (string.IsNullOrWhiteSpace(imgPath)) continue;
                                if (existing.Images.Any(x => x.Path == imgPath)) continue;
                                var sort = img.TryGetProperty("SortOrder", out var iso) && iso.TryGetInt32(out var isort) ? isort : existing.Images.Count;
                                existing.Images.Add(new ProductImage { Path = imgPath, SortOrder = sort });
                            }
                        }
                    }
                    await _db.SaveChangesAsync(cancellationToken);
                }

                if (root.TryGetProperty("settings", out var settingsEl) && settingsEl.ValueKind == JsonValueKind.Object)
                {
                    var settings = await _db.SiteSettings.FirstOrDefaultAsync(cancellationToken);
                    if (settings == null)
                    {
                        settings = new SiteSettings();
                        _db.SiteSettings.Add(settings);
                    }

                    void Set(string prop, Action<string> assign)
                    {
                        if (settingsEl.TryGetProperty(prop, out var el) && el.ValueKind == JsonValueKind.String)
                            assign(el.GetString() ?? "");
                    }

                    Set(nameof(SiteSettings.HeroTitleEn), v => settings.HeroTitleEn = v);
                    Set(nameof(SiteSettings.HeroTitleAr), v => settings.HeroTitleAr = v);
                    Set(nameof(SiteSettings.HeroSubtitleEn), v => settings.HeroSubtitleEn = v);
                    Set(nameof(SiteSettings.HeroSubtitleAr), v => settings.HeroSubtitleAr = v);
                    Set(nameof(SiteSettings.AboutTitleEn), v => settings.AboutTitleEn = v);
                    Set(nameof(SiteSettings.AboutTitleAr), v => settings.AboutTitleAr = v);
                    Set(nameof(SiteSettings.AboutBodyEn), v => settings.AboutBodyEn = v);
                    Set(nameof(SiteSettings.AboutBodyAr), v => settings.AboutBodyAr = v);
                    Set(nameof(SiteSettings.FooterTextEn), v => settings.FooterTextEn = v);
                    Set(nameof(SiteSettings.FooterTextAr), v => settings.FooterTextAr = v);
                    Set(nameof(SiteSettings.ContactEmail), v => settings.ContactEmail = v);
                    Set(nameof(SiteSettings.ContactPhone), v => settings.ContactPhone = v);
                    Set(nameof(SiteSettings.ContactAddressEn), v => settings.ContactAddressEn = v);
                    Set(nameof(SiteSettings.ContactAddressAr), v => settings.ContactAddressAr = v);
                    Set(nameof(SiteSettings.ContactHoursEn), v => settings.ContactHoursEn = v);
                    Set(nameof(SiteSettings.ContactHoursAr), v => settings.ContactHoursAr = v);
                    Set(nameof(SiteSettings.TermsTitleEn), v => settings.TermsTitleEn = v);
                    Set(nameof(SiteSettings.TermsTitleAr), v => settings.TermsTitleAr = v);
                    Set(nameof(SiteSettings.TermsBodyEn), v => settings.TermsBodyEn = v);
                    Set(nameof(SiteSettings.TermsBodyAr), v => settings.TermsBodyAr = v);
                    Set(nameof(SiteSettings.RefundTitleEn), v => settings.RefundTitleEn = v);
                    Set(nameof(SiteSettings.RefundTitleAr), v => settings.RefundTitleAr = v);
                    Set(nameof(SiteSettings.RefundBodyEn), v => settings.RefundBodyEn = v);
                    Set(nameof(SiteSettings.RefundBodyAr), v => settings.RefundBodyAr = v);
                    Set(nameof(SiteSettings.PrivacyTitleEn), v => settings.PrivacyTitleEn = v);
                    Set(nameof(SiteSettings.PrivacyTitleAr), v => settings.PrivacyTitleAr = v);
                    Set(nameof(SiteSettings.PrivacyBodyEn), v => settings.PrivacyBodyEn = v);
                    Set(nameof(SiteSettings.PrivacyBodyAr), v => settings.PrivacyBodyAr = v);
                    Set(nameof(SiteSettings.BankTransferInfoEn), v => settings.BankTransferInfoEn = v);
                    Set(nameof(SiteSettings.BankTransferInfoAr), v => settings.BankTransferInfoAr = v);
                    Set(nameof(SiteSettings.SiteNameEn), v => settings.SiteNameEn = v);
                    Set(nameof(SiteSettings.SiteNameHighlightEn), v => settings.SiteNameHighlightEn = v);
                    Set(nameof(SiteSettings.SiteNameAr), v => settings.SiteNameAr = v);
                    Set(nameof(SiteSettings.SiteNameHighlightAr), v => settings.SiteNameHighlightAr = v);
                    Set(nameof(SiteSettings.LogoPath), v => settings.LogoPath = v);
                    Set(nameof(SiteSettings.ThemePrimary), v => settings.ThemePrimary = ThemeHelper.NormalizeHex(v, "#151528"));
                    Set(nameof(SiteSettings.ThemeAccent), v => settings.ThemeAccent = ThemeHelper.NormalizeHex(v, "#e23b58"));
                    await _db.SaveChangesAsync(cancellationToken);
                }

                if (root.TryGetProperty("bankAccounts", out var banksEl) && banksEl.ValueKind == JsonValueKind.Array)
                {
                    _db.BankAccounts.RemoveRange(_db.BankAccounts);
                    await _db.SaveChangesAsync(cancellationToken);
                    foreach (var b in banksEl.EnumerateArray())
                    {
                        var bank = new BankAccount
                        {
                            BankNameEn = b.TryGetProperty("BankNameEn", out var bne) ? bne.GetString() ?? "" : "",
                            BankNameAr = b.TryGetProperty("BankNameAr", out var bna) ? bna.GetString() ?? "" : "",
                            AccountNumber = b.TryGetProperty("AccountNumber", out var an) ? an.GetString() ?? "" : "",
                            Iban = b.TryGetProperty("Iban", out var ib) ? ib.GetString() : null,
                            BeneficiaryEn = b.TryGetProperty("BeneficiaryEn", out var be) ? be.GetString() ?? "SagerShop" : "SagerShop",
                            BeneficiaryAr = b.TryGetProperty("BeneficiaryAr", out var ba) ? ba.GetString() ?? "SagerShop" : "SagerShop",
                            NotesEn = b.TryGetProperty("NotesEn", out var ne) ? ne.GetString() : null,
                            NotesAr = b.TryGetProperty("NotesAr", out var nar) ? nar.GetString() : null,
                            SortOrder = b.TryGetProperty("SortOrder", out var bso) && bso.TryGetInt32(out var sort) ? sort : 0,
                            IsActive = !b.TryGetProperty("IsActive", out var ia) || ia.ValueKind != JsonValueKind.False
                        };
                        if (string.IsNullOrWhiteSpace(bank.BankNameEn) || string.IsNullOrWhiteSpace(bank.AccountNumber))
                            continue;
                        _db.BankAccounts.Add(bank);
                    }
                    await _db.SaveChangesAsync(cancellationToken);
                }

                if (root.TryGetProperty("quickLinks", out var linksEl) && linksEl.ValueKind == JsonValueKind.Array)
                {
                    _db.QuickLinks.RemoveRange(_db.QuickLinks);
                    await _db.SaveChangesAsync(cancellationToken);
                    foreach (var l in linksEl.EnumerateArray())
                    {
                        var link = new QuickLink
                        {
                            TitleEn = l.TryGetProperty("TitleEn", out var te) ? te.GetString() ?? "" : "",
                            TitleAr = l.TryGetProperty("TitleAr", out var ta) ? ta.GetString() ?? "" : "",
                            Url = l.TryGetProperty("Url", out var u) ? u.GetString() ?? "/" : "/",
                            SortOrder = l.TryGetProperty("SortOrder", out var lso) && lso.TryGetInt32(out var ls) ? ls : 0,
                            IsActive = !l.TryGetProperty("IsActive", out var lia) || lia.ValueKind != JsonValueKind.False
                        };
                        if (string.IsNullOrWhiteSpace(link.TitleEn)) continue;
                        _db.QuickLinks.Add(link);
                    }
                    await _db.SaveChangesAsync(cancellationToken);
                }

                await tx.CommitAsync(cancellationToken);
                CatalogCache.Invalidate(_cache);
                _toast.AddSuccessToastMessage(_localizer["BackupImported"]);
            }
            catch
            {
                _toast.AddErrorToastMessage(_localizer["BackupImportFailed"]);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Health(CancellationToken cancellationToken)
        {
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            return Json(new
            {
                status = canConnect ? "ok" : "degraded",
                utc = DateTime.UtcNow,
                database = canConnect,
                products = canConnect ? await _db.Products.CountAsync(cancellationToken) : -1
            });
        }
    }
}
