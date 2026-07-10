using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Services
{
    public interface IShippingService
    {
        decimal GetShippingCostUsd(string country, string? city, decimal subtotalUsd = 0);
        decimal GetVatRate(string country, string? city = null);
        int GetDeliveryDaysMin(string country, string? city = null);
        int GetDeliveryDaysMax(string country, string? city = null);
        void InvalidateCache();
    }

    internal record ShippingConfig(ShippingSettings Settings, List<ShippingZone> Zones);

    public class ShippingService : IShippingService
    {
        private const string CacheKey = "shipping:config";
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        private readonly ApplicationDbContext _db;
        private readonly IMemoryCache _cache;

        public ShippingService(ApplicationDbContext db, IMemoryCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public void InvalidateCache() => _cache.Remove(CacheKey);

        public decimal GetShippingCostUsd(string country, string? city, decimal subtotalUsd = 0)
        {
            var config = GetConfig();
            if (IsFreeShipping(config.Settings, country, city, subtotalUsd))
                return 0m;

            var zone = ResolveZone(config, country, city);
            return zone?.IsFreeShipping == true ? 0m : zone?.ShippingCostUsd ?? config.Settings.DefaultCostUsd;
        }

        public decimal GetVatRate(string country, string? city = null)
        {
            var config = GetConfig();
            var zone = ResolveZone(config, country, city);
            return zone?.VatRate ?? config.Settings.DefaultVatRate;
        }

        public int GetDeliveryDaysMin(string country, string? city = null)
        {
            var config = GetConfig();
            var zone = ResolveZone(config, country, city);
            return zone?.DeliveryMinDays ?? config.Settings.DefaultMinDays;
        }

        public int GetDeliveryDaysMax(string country, string? city = null)
        {
            var config = GetConfig();
            var zone = ResolveZone(config, country, city);
            return zone?.DeliveryMaxDays ?? config.Settings.DefaultMaxDays;
        }

        private ShippingConfig GetConfig() =>
            _cache.GetOrCreate(CacheKey, entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = CacheDuration;
                var settings = _db.ShippingSettings.OrderBy(s => s.Id).FirstOrDefault() ?? new ShippingSettings();
                var zones = _db.ShippingZones
                    .Where(z => z.IsActive)
                    .OrderBy(z => z.SortOrder)
                    .ThenBy(z => z.Country)
                    .ToList();
                return new ShippingConfig(settings, zones);
            })!;

        private static bool IsFreeShipping(ShippingSettings settings, string country, string? city, decimal subtotalUsd)
        {
            if (settings.FreeShippingMinOrderUsd > 0 && subtotalUsd >= settings.FreeShippingMinOrderUsd)
                return true;

            if (!settings.FreeShippingSameCity)
                return false;

            return CitiesMatch(settings.StoreCity, city) && CountriesMatch(settings.StoreCountry, country);
        }

        private static ShippingZone? ResolveZone(ShippingConfig config, string country, string? city)
        {
            var active = config.Zones;
            var cityZone = active.FirstOrDefault(z =>
                CountriesMatch(z.Country, country) &&
                !string.IsNullOrWhiteSpace(z.City) &&
                CitiesMatch(z.City, city));

            if (cityZone != null)
                return cityZone;

            return active.FirstOrDefault(z =>
                CountriesMatch(z.Country, country) && string.IsNullOrWhiteSpace(z.City));
        }

        private static bool CountriesMatch(string a, string b) =>
            string.Equals(NormalizeCountry(a), NormalizeCountry(b), StringComparison.OrdinalIgnoreCase);

        private static string NormalizeCountry(string country)
        {
            var c = country.Trim();
            return c switch
            {
                "TR" => "Turkey",
                "SY" => "Syria",
                "US" => "United States",
                "GB" => "United Kingdom",
                "DE" => "Germany",
                _ => c
            };
        }

        private static bool CitiesMatch(string? a, string? b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            var x = a.Trim();
            var y = b.Trim();
            return x.Equals(y, StringComparison.OrdinalIgnoreCase) ||
                   y.Contains(x, StringComparison.OrdinalIgnoreCase) ||
                   x.Contains(y, StringComparison.OrdinalIgnoreCase);
        }
    }
}
