using Moq;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Tests;

public class LocalizedContentTests
{
    private static IAppLocalizer Loc(bool arabic)
    {
        var mock = new Mock<IAppLocalizer>();
        mock.SetupGet(x => x.IsArabic).Returns(arabic);
        mock.SetupGet(x => x.CurrentLanguage).Returns(arabic ? "ar" : "en");
        mock.Setup(x => x[It.IsAny<string>()]).Returns((string key) => key);
        return mock.Object;
    }

    [Fact]
    public void CategoryName_UsesArabic_WhenAvailable()
    {
        var cat = new Categoty { Name = "Electronics", NameAr = "إلكترونيات" };
        Assert.Equal("إلكترونيات", LocalizedContent.CategoryName(cat, Loc(true)));
    }

    [Fact]
    public void CategoryName_FallsBackToEnglish_WhenArabicEmpty()
    {
        var cat = new Categoty { Name = "Electronics", NameAr = "  " };
        Assert.Equal("Electronics", LocalizedContent.CategoryName(cat, Loc(true)));
    }

    [Fact]
    public void CategoryName_UsesEnglish_WhenNotArabic()
    {
        var cat = new Categoty { Name = "Electronics", NameAr = "إلكترونيات" };
        Assert.Equal("Electronics", LocalizedContent.CategoryName(cat, Loc(false)));
    }

    [Fact]
    public void ProductName_UsesArabic_WhenAvailable()
    {
        var product = new Product { Name = "Phone", NameAr = "هاتف" };
        Assert.Equal("هاتف", LocalizedContent.ProductName(product, Loc(true)));
    }

    [Fact]
    public void FormatBankAccounts_OrdersFields_InArabic()
    {
        var text = LocalizedContent.FormatBankAccounts(
        [
            new BankAccount
            {
                BankNameEn = "Sham Bank",
                BankNameAr = "بنك الشام",
                AccountNumber = "123",
                BeneficiaryEn = "Sager",
                BeneficiaryAr = "ساجر",
                IsActive = true
            }
        ], arabic: true);

        Assert.Contains("بنك الشام", text);
        Assert.Contains("رقم الحساب: 123", text);
        Assert.Contains("المستفيد: ساجر", text);
    }
}
