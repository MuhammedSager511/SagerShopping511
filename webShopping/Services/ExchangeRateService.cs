using System.Text.Json;

namespace webShopping.Services
{
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ExchangeRateService> _logger;
        private readonly object _lock = new();

        private static readonly string[] SupportedCodes = { "USD", "EUR", "GBP", "TRY", "SAR", "AED", "SYP" };
        private static readonly string[] FrankfurterCodes = { "EUR", "GBP", "TRY", "SAR", "AED" };

        private static readonly Dictionary<string, double> FallbackRates = new()
        {
            ["USD"] = 1.0,
            ["EUR"] = 0.92,
            ["GBP"] = 0.79,
            ["TRY"] = 34.0,
            ["SAR"] = 3.75,
            ["AED"] = 3.67,
            ["SYP"] = 13000.0
        };

        private Dictionary<string, double> _rates = new(FallbackRates);
        private DateTime? _lastUpdated;
        private string? _source;

        public ExchangeRateService(IHttpClientFactory httpClientFactory, ILogger<ExchangeRateService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public DateTime? LastUpdated
        {
            get { lock (_lock) return _lastUpdated; }
        }

        public string? Source
        {
            get { lock (_lock) return _source; }
        }

        public double GetRate(string currencyCode)
        {
            var code = NormalizeCode(currencyCode);
            lock (_lock)
            {
                return _rates.TryGetValue(code, out var rate) ? rate : 1.0;
            }
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            var rates = new Dictionary<string, double>(FallbackRates);
            string? source = null;
            DateTime? updated = null;

            try
            {
                var frankfurterRates = await FetchFrankfurterAsync(cancellationToken);
                if (frankfurterRates.Count > 0)
                {
                    foreach (var pair in frankfurterRates)
                        rates[pair.Key] = pair.Value;
                    source = "Frankfurter (ECB)";
                    updated = DateTime.UtcNow;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Frankfurter API failed, trying fallback provider.");
            }

            if (source == null)
            {
                try
                {
                    var openRates = await FetchOpenErApiAsync(cancellationToken);
                    if (openRates.Count > 0)
                    {
                        foreach (var code in SupportedCodes)
                        {
                            if (openRates.TryGetValue(code, out var rate))
                                rates[code] = rate;
                        }
                        source = "open.er-api.com";
                        updated = DateTime.UtcNow;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "open.er-api.com failed, using static fallback rates.");
                }
            }
            else if (!rates.ContainsKey("SYP") || rates["SYP"] == FallbackRates["SYP"])
            {
                try
                {
                    var openRates = await FetchOpenErApiAsync(cancellationToken);
                    if (openRates.TryGetValue("SYP", out var sypRate))
                        rates["SYP"] = sypRate;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not fetch SYP rate.");
                }
            }

            rates["USD"] = 1.0;
            lock (_lock)
            {
                _rates = rates;
                _lastUpdated = updated ?? _lastUpdated;
                _source = source ?? _source ?? "Fallback";
            }

            _logger.LogInformation("Exchange rates updated from {Source}. USD→TRY={TryRate}", source ?? "Fallback", rates["TRY"]);
        }

        private async Task<Dictionary<string, double>> FetchFrankfurterAsync(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("ExchangeRates");
            var to = string.Join(",", FrankfurterCodes);
            var url = $"https://api.frankfurter.app/latest?from=USD&to={to}";
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var result = new Dictionary<string, double> { ["USD"] = 1.0 };

            if (doc.RootElement.TryGetProperty("rates", out var ratesElement))
            {
                foreach (var property in ratesElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Number)
                        result[property.Name] = property.Value.GetDouble();
                }
            }

            return result;
        }

        private async Task<Dictionary<string, double>> FetchOpenErApiAsync(CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient("ExchangeRates");
            var response = await client.GetAsync("https://open.er-api.com/v6/latest/USD", cancellationToken);
            response.EnsureSuccessStatusCode();

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            var result = new Dictionary<string, double> { ["USD"] = 1.0 };

            if (doc.RootElement.TryGetProperty("rates", out var ratesElement))
            {
                foreach (var property in ratesElement.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Number)
                        result[property.Name] = property.Value.GetDouble();
                }
            }

            return result;
        }

        private static string NormalizeCode(string code) =>
            string.IsNullOrWhiteSpace(code) ? "USD" : code.ToUpperInvariant();
    }
}
