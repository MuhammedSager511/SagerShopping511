using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using webShopping.Services;

namespace webShopping.Controllers
{
    public class CultureController : Controller
    {
        private readonly ICurrencyService _currencyService;

        public CultureController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            culture = culture == "ar" ? "ar" : "en";
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1), IsEssential = true });

            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetCurrency(string currency, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(currency))
                _currencyService.SetCurrency(currency);

            return LocalRedirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
        }
    }
}
