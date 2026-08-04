using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using webShopping.Services;

namespace webShopping.Controllers
{
    public class CultureController : Controller
    {
        private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase)
        {
            "en", "ar", "tr"
        };

        private readonly ICurrencyService _currencyService;

        public CultureController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        [HttpGet]
        public IActionResult SetLanguage(string culture, string? returnUrl)
        {
            culture = Normalize(culture);
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName,
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddYears(1),
                    IsEssential = true,
                    HttpOnly = false,
                    Secure = Request.IsHttps,
                    SameSite = SameSiteMode.Lax
                });

            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SetCurrency(string currency, string? returnUrl)
        {
            if (!string.IsNullOrEmpty(currency))
                _currencyService.SetCurrency(currency);

            return LocalRedirect(SafeReturnUrl(returnUrl));
        }

        private static string Normalize(string? culture)
        {
            var two = (culture ?? "en").Trim().ToLowerInvariant();
            if (two.Length > 2)
                two = two.Split('-', '_')[0];
            return Supported.Contains(two) ? two : "en";
        }

        private string SafeReturnUrl(string? returnUrl)
        {
            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return returnUrl;
            return "/";
        }
    }
}
