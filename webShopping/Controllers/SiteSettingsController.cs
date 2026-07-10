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

        public SiteSettingsController(
            ApplicationDbContext db,
            ISiteContentService siteContent,
            IToastNotification toast,
            IAppLocalizer localizer)
        {
            _db = db;
            _siteContent = siteContent;
            _toast = toast;
            _localizer = localizer;
        }

        public async Task<IActionResult> Index()
        {
            var settings = await _db.SiteSettings.OrderBy(s => s.Id).FirstOrDefaultAsync() ?? new SiteSettings();
            ViewBag.QuickLinks = await _db.QuickLinks.OrderBy(q => q.SortOrder).ToListAsync();
            return View(settings);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(SiteSettings model)
        {
            var settings = await _db.SiteSettings.OrderBy(s => s.Id).FirstOrDefaultAsync();
            if (settings == null)
            {
                _db.SiteSettings.Add(model);
            }
            else
            {
                settings.HeroTitleEn = model.HeroTitleEn;
                settings.HeroTitleAr = model.HeroTitleAr;
                settings.HeroSubtitleEn = model.HeroSubtitleEn;
                settings.HeroSubtitleAr = model.HeroSubtitleAr;
                settings.AboutTitleEn = model.AboutTitleEn;
                settings.AboutTitleAr = model.AboutTitleAr;
                settings.AboutBodyEn = model.AboutBodyEn;
                settings.AboutBodyAr = model.AboutBodyAr;
                settings.FooterTextEn = model.FooterTextEn;
                settings.FooterTextAr = model.FooterTextAr;
                settings.ContactEmail = model.ContactEmail;
                settings.ContactPhone = model.ContactPhone;
                settings.ContactAddressEn = model.ContactAddressEn;
                settings.ContactAddressAr = model.ContactAddressAr;
                settings.ContactHoursEn = model.ContactHoursEn;
                settings.ContactHoursAr = model.ContactHoursAr;
                settings.FacebookUrl = model.FacebookUrl;
                settings.InstagramUrl = model.InstagramUrl;
                settings.TwitterUrl = model.TwitterUrl;
                settings.WhatsAppUrl = model.WhatsAppUrl;
                settings.TermsTitleEn = model.TermsTitleEn;
                settings.TermsTitleAr = model.TermsTitleAr;
                settings.TermsBodyEn = model.TermsBodyEn;
                settings.TermsBodyAr = model.TermsBodyAr;
                settings.RefundTitleEn = model.RefundTitleEn;
                settings.RefundTitleAr = model.RefundTitleAr;
                settings.RefundBodyEn = model.RefundBodyEn;
                settings.RefundBodyAr = model.RefundBodyAr;
                settings.PrivacyTitleEn = model.PrivacyTitleEn;
                settings.PrivacyTitleAr = model.PrivacyTitleAr;
                settings.PrivacyBodyEn = model.PrivacyBodyEn;
                settings.PrivacyBodyAr = model.PrivacyBodyAr;
                settings.BankTransferInfoEn = model.BankTransferInfoEn;
                settings.BankTransferInfoAr = model.BankTransferInfoAr;
            }

            await _db.SaveChangesAsync();
            _siteContent.Refresh();
            _toast.AddSuccessToastMessage(_localizer["ToastSiteSaved"]);
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
    }
}
