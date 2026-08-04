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
    public class SiteSettingsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ISiteContentService _siteContent;
        private readonly IToastNotification _toast;
        private readonly IAppLocalizer _localizer;
        private readonly IImageUploadService _images;
        private readonly ILogger<SiteSettingsController> _logger;

        public SiteSettingsController(
            ApplicationDbContext db,
            ISiteContentService siteContent,
            IToastNotification toast,
            IAppLocalizer localizer,
            IImageUploadService images,
            ILogger<SiteSettingsController> logger)
        {
            _db = db;
            _siteContent = siteContent;
            _toast = toast;
            _localizer = localizer;
            _images = images;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            SiteSettings settings;
            try
            {
                settings = await _db.SiteSettings.AsNoTracking().OrderBy(s => s.Id).FirstOrDefaultAsync()
                           ?? new SiteSettings();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed loading SiteSettings");
                settings = new SiteSettings();
            }

            try
            {
                ViewBag.QuickLinks = await _db.QuickLinks.AsNoTracking().OrderBy(q => q.SortOrder).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed loading QuickLinks");
                ViewBag.QuickLinks = Enumerable.Empty<QuickLink>();
            }

            try
            {
                ViewBag.BankAccounts = await _db.BankAccounts.AsNoTracking()
                    .OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed loading BankAccounts");
                ViewBag.BankAccounts = Enumerable.Empty<BankAccount>();
            }

            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Index(SiteSettings model, IFormFile? logoFile)
        {
            ModelState.Remove(nameof(SiteSettings.Id));

            string? newLogo = null;
            if (logoFile is { Length: > 0 })
            {
                try
                {
                    var (fileName, _) = await _images.SaveProductImageAsync(logoFile, "logo");
                    newLogo = fileName;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Logo upload failed");
                    _toast.AddErrorToastMessage(ex.Message);
                    return RedirectToAction(nameof(Index));
                }
            }

            var settings = await _db.SiteSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (settings == null)
            {
                model.Id = 0;
                Normalize(model);
                ApplyBranding(model, model, newLogo);
                _db.SiteSettings.Add(model);
            }
            else
            {
                ApplyBranding(settings, model, newLogo);
                settings.HeroTitleEn = model.HeroTitleEn ?? "";
                settings.HeroTitleAr = model.HeroTitleAr ?? "";
                settings.HeroSubtitleEn = model.HeroSubtitleEn ?? "";
                settings.HeroSubtitleAr = model.HeroSubtitleAr ?? "";
                settings.AboutTitleEn = model.AboutTitleEn ?? "";
                settings.AboutTitleAr = model.AboutTitleAr ?? "";
                settings.AboutBodyEn = model.AboutBodyEn ?? "";
                settings.AboutBodyAr = model.AboutBodyAr ?? "";
                settings.FooterTextEn = model.FooterTextEn ?? "";
                settings.FooterTextAr = model.FooterTextAr ?? "";
                settings.ContactEmail = model.ContactEmail ?? "";
                settings.ContactPhone = model.ContactPhone ?? "";
                settings.ContactAddressEn = model.ContactAddressEn ?? "";
                settings.ContactAddressAr = model.ContactAddressAr ?? "";
                settings.ContactHoursEn = model.ContactHoursEn ?? "";
                settings.ContactHoursAr = model.ContactHoursAr ?? "";
                settings.FacebookUrl = SocialLinks.Normalize(model.FacebookUrl ?? "#");
                settings.InstagramUrl = SocialLinks.Normalize(model.InstagramUrl ?? "#");
                settings.TwitterUrl = SocialLinks.Normalize(model.TwitterUrl ?? "#");
                settings.WhatsAppUrl = SocialLinks.WhatsApp(model.WhatsAppUrl ?? "#");
                settings.TermsTitleEn = model.TermsTitleEn ?? "";
                settings.TermsTitleAr = model.TermsTitleAr ?? "";
                settings.TermsBodyEn = model.TermsBodyEn ?? "";
                settings.TermsBodyAr = model.TermsBodyAr ?? "";
                settings.RefundTitleEn = model.RefundTitleEn ?? "";
                settings.RefundTitleAr = model.RefundTitleAr ?? "";
                settings.RefundBodyEn = model.RefundBodyEn ?? "";
                settings.RefundBodyAr = model.RefundBodyAr ?? "";
                settings.PrivacyTitleEn = model.PrivacyTitleEn ?? "";
                settings.PrivacyTitleAr = model.PrivacyTitleAr ?? "";
                settings.PrivacyBodyEn = model.PrivacyBodyEn ?? "";
                settings.PrivacyBodyAr = model.PrivacyBodyAr ?? "";
            }

            try
            {
                await SyncBankTransferTextAsync();
                await _db.SaveChangesAsync();
                _siteContent.Refresh();
                _toast.AddSuccessToastMessage(_localizer["ToastSiteSaved"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed saving SiteSettings");
                _toast.AddErrorToastMessage(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBankAccount(BankAccount account)
        {
            if (string.IsNullOrWhiteSpace(account.BankNameEn) || string.IsNullOrWhiteSpace(account.AccountNumber))
            {
                _toast.AddErrorToastMessage(_localizer["BankAccountRequired"]);
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(account.BankNameAr))
                account.BankNameAr = account.BankNameEn;
            if (string.IsNullOrWhiteSpace(account.BeneficiaryEn))
                account.BeneficiaryEn = "SagerShop";
            if (string.IsNullOrWhiteSpace(account.BeneficiaryAr))
                account.BeneficiaryAr = account.BeneficiaryEn;

            account.IsActive = true;
            if (account.SortOrder <= 0)
                account.SortOrder = (await _db.BankAccounts.MaxAsync(b => (int?)b.SortOrder) ?? 0) + 1;

            try
            {
                _db.BankAccounts.Add(account);
                await _db.SaveChangesAsync();
                await SyncBankTransferTextAsync();
                await _db.SaveChangesAsync();
                _siteContent.Refresh();
                _toast.AddSuccessToastMessage(_localizer["ToastBankAccountAdded"]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed adding bank account");
                _toast.AddErrorToastMessage(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBankAccount(int id)
        {
            try
            {
                var account = await _db.BankAccounts.FindAsync(id);
                if (account != null)
                {
                    _db.BankAccounts.Remove(account);
                    await _db.SaveChangesAsync();
                    await SyncBankTransferTextAsync();
                    await _db.SaveChangesAsync();
                    _siteContent.Refresh();
                    _toast.AddSuccessToastMessage(_localizer["ToastBankAccountDeleted"]);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed deleting bank account {Id}", id);
                _toast.AddErrorToastMessage(ex.Message);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuickLink(QuickLink link)
        {
            link.IsActive = true;
            _db.QuickLinks.Add(link);
            await _db.SaveChangesAsync();
            _siteContent.Refresh();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteQuickLink(int id)
        {
            var link = await _db.QuickLinks.FindAsync(id);
            if (link != null)
            {
                _db.QuickLinks.Remove(link);
                await _db.SaveChangesAsync();
                _siteContent.Refresh();
            }
            return RedirectToAction(nameof(Index));
        }

        private static void ApplyBranding(SiteSettings target, SiteSettings model, string? newLogo)
        {
            target.SiteNameEn = string.IsNullOrWhiteSpace(model.SiteNameEn) ? "Sager" : model.SiteNameEn.Trim();
            target.SiteNameHighlightEn = model.SiteNameHighlightEn?.Trim() ?? "";
            target.SiteNameAr = string.IsNullOrWhiteSpace(model.SiteNameAr) ? target.SiteNameEn : model.SiteNameAr.Trim();
            target.SiteNameHighlightAr = string.IsNullOrWhiteSpace(model.SiteNameHighlightAr)
                ? target.SiteNameHighlightEn
                : model.SiteNameHighlightAr.Trim();
            target.ThemePrimary = ThemeHelper.NormalizeHex(model.ThemePrimary, "#151528");
            target.ThemeAccent = ThemeHelper.NormalizeHex(model.ThemeAccent, "#e23b58");
            if (!string.IsNullOrEmpty(newLogo))
                target.LogoPath = newLogo;
            else if (target.LogoPath == null)
                target.LogoPath = "";
        }

        private static void Normalize(SiteSettings model)
        {
            model.SiteNameEn ??= "Sager";
            model.SiteNameHighlightEn ??= "Shop";
            model.SiteNameAr ??= "";
            model.SiteNameHighlightAr ??= "";
            model.LogoPath ??= "";
            model.ThemePrimary = ThemeHelper.NormalizeHex(model.ThemePrimary, "#151528");
            model.ThemeAccent = ThemeHelper.NormalizeHex(model.ThemeAccent, "#e23b58");
            model.HeroTitleEn ??= "";
            model.HeroTitleAr ??= "";
            model.HeroSubtitleEn ??= "";
            model.HeroSubtitleAr ??= "";
            model.AboutTitleEn ??= "";
            model.AboutTitleAr ??= "";
            model.AboutBodyEn ??= "";
            model.AboutBodyAr ??= "";
            model.FooterTextEn ??= "";
            model.FooterTextAr ??= "";
            model.ContactEmail ??= "";
            model.ContactPhone ??= "";
            model.ContactAddressEn ??= "";
            model.ContactAddressAr ??= "";
            model.ContactHoursEn ??= "";
            model.ContactHoursAr ??= "";
            model.FacebookUrl ??= "#";
            model.InstagramUrl ??= "#";
            model.TwitterUrl ??= "#";
            model.WhatsAppUrl ??= "#";
            model.TermsTitleEn ??= "";
            model.TermsTitleAr ??= "";
            model.TermsBodyEn ??= "";
            model.TermsBodyAr ??= "";
            model.RefundTitleEn ??= "";
            model.RefundTitleAr ??= "";
            model.RefundBodyEn ??= "";
            model.RefundBodyAr ??= "";
            model.PrivacyTitleEn ??= "";
            model.PrivacyTitleAr ??= "";
            model.PrivacyBodyEn ??= "";
            model.PrivacyBodyAr ??= "";
            model.BankTransferInfoEn ??= "";
            model.BankTransferInfoAr ??= "";
        }

        private async Task SyncBankTransferTextAsync()
        {
            try
            {
                var accounts = await _db.BankAccounts
                    .Where(b => b.IsActive)
                    .OrderBy(b => b.SortOrder)
                    .ThenBy(b => b.Id)
                    .ToListAsync();

                var settings = await _db.SiteSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
                if (settings == null || accounts.Count == 0) return;

                settings.BankTransferInfoEn = LocalizedContent.FormatBankAccounts(accounts, arabic: false);
                settings.BankTransferInfoAr = LocalizedContent.FormatBankAccounts(accounts, arabic: true);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "SyncBankTransferText skipped");
            }
        }
    }
}
