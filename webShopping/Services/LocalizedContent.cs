using webShopping.Models;

namespace webShopping.Services
{
    public static class LocalizedContent
    {
        public static string ProductName(Product? p, IAppLocalizer loc) =>
            p == null ? "" : loc.IsArabic && !string.IsNullOrWhiteSpace(p.NameAr) ? p.NameAr : p.Name ?? "";

        public static string ProductDescription(Product? p, IAppLocalizer loc) =>
            p == null ? "" : loc.IsArabic && !string.IsNullOrWhiteSpace(p.DescriptionAr) ? p.DescriptionAr : p.Description ?? "";

        public static string CategoryName(Categoty? c, IAppLocalizer loc) =>
            c == null ? "" : loc.IsArabic && !string.IsNullOrWhiteSpace(c.NameAr) ? c.NameAr : c.Name;

        public static string BankTransferInfo(SiteSettings settings, IAppLocalizer loc) =>
            loc.IsArabic && !string.IsNullOrWhiteSpace(settings.BankTransferInfoAr)
                ? settings.BankTransferInfoAr
                : settings.BankTransferInfoEn;
    }
}
