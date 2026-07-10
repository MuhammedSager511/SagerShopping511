using System.Globalization;

namespace webShopping.Services
{
    public class CurrencyService : ICurrencyService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IExchangeRateService _exchangeRates;

        private static readonly Dictionary<string, (string Symbol, string Name)> CurrencyMeta = new()
        {
            ["USD"] = ("$", "US Dollar"),
            ["EUR"] = ("€", "Euro"),
            ["GBP"] = ("£", "British Pound"),
            ["TRY"] = ("₺", "Turkish Lira"),
            ["SAR"] = ("﷼", "Saudi Riyal"),
            ["AED"] = ("د.إ", "UAE Dirham"),
            ["SYP"] = ("£S", "Syrian Pound")
        };

        public CurrencyService(IHttpContextAccessor httpContextAccessor, IExchangeRateService exchangeRates)
        {
            _httpContextAccessor = httpContextAccessor;
            _exchangeRates = exchangeRates;
        }

        public string CurrentCurrency
        {
            get
            {
                var code = _httpContextAccessor.HttpContext?.Session.GetString("Currency");
                return !string.IsNullOrEmpty(code) && CurrencyMeta.ContainsKey(code) ? code : "USD";
            }
        }

        public string CurrentSymbol => CurrencyMeta[CurrentCurrency].Symbol;

        public DateTime? RatesLastUpdated => _exchangeRates.LastUpdated;

        public string? RatesSource => _exchangeRates.Source;

        public double GetRate(string currencyCode) => _exchangeRates.GetRate(currencyCode);

        public double ConvertFromUsd(double amountUsd, string? currencyCode = null)
        {
            var code = currencyCode ?? CurrentCurrency;
            return amountUsd * GetRate(code);
        }

        public string Format(double amountUsd) => FormatInCurrency(amountUsd, CurrentCurrency);

        public string FormatInCurrency(double amountUsd, string currencyCode)
        {
            if (!CurrencyMeta.TryGetValue(currencyCode, out var meta))
                return amountUsd.ToString("N2", CultureInfo.CurrentUICulture);

            var converted = ConvertFromUsd(amountUsd, currencyCode);
            var formatted = converted.ToString("N2", CultureInfo.CurrentUICulture);

            if (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ||
                currencyCode is "AED" or "SYP")
                return $"{formatted} {meta.Symbol}";

            return $"{meta.Symbol}{formatted}";
        }

        public void SetCurrency(string code)
        {
            if (CurrencyMeta.ContainsKey(code))
                _httpContextAccessor.HttpContext?.Session.SetString("Currency", code);
        }

        public IReadOnlyList<(string Code, string Symbol, string Name)> GetSupportedCurrencies() =>
            CurrencyMeta.Select(c => (c.Key, c.Value.Symbol, c.Value.Name)).ToList();
    }
}
