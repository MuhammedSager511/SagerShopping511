using System.Text;
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

        public static string BankTransferInfo(ISiteContentService site, IAppLocalizer loc)
        {
            var accounts = site.BankAccounts;
            if (accounts.Count > 0)
                return FormatBankAccounts(accounts, loc.IsArabic);

            var settings = site.Settings;
            return loc.IsArabic && !string.IsNullOrWhiteSpace(settings.BankTransferInfoAr)
                ? settings.BankTransferInfoAr
                : settings.BankTransferInfoEn;
        }

        public static string FormatBankAccounts(IEnumerable<BankAccount> accounts, bool arabic)
        {
            var sb = new StringBuilder();
            foreach (var a in accounts)
            {
                if (sb.Length > 0) sb.AppendLine().AppendLine();
                sb.AppendLine(arabic ? a.BankNameAr : a.BankNameEn);
                sb.Append(arabic ? "رقم الحساب: " : "Account: ").AppendLine(a.AccountNumber);
                if (!string.IsNullOrWhiteSpace(a.Iban))
                    sb.Append("IBAN: ").AppendLine(a.Iban);
                var beneficiary = arabic ? a.BeneficiaryAr : a.BeneficiaryEn;
                if (!string.IsNullOrWhiteSpace(beneficiary))
                    sb.Append(arabic ? "المستفيد: " : "Beneficiary: ").AppendLine(beneficiary);
                var notes = arabic ? a.NotesAr : a.NotesEn;
                if (!string.IsNullOrWhiteSpace(notes))
                    sb.AppendLine(notes);
            }
            return sb.ToString().Trim();
        }
    }
}
