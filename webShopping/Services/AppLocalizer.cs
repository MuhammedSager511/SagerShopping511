using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace webShopping.Services
{
    public class AppLocalizer : IAppLocalizer
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache = new();
        private static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase) { "en", "ar", "tr" };
        private readonly IWebHostEnvironment _env;

        public AppLocalizer(IWebHostEnvironment env)
        {
            _env = env;
        }

        public bool IsArabic => CurrentLanguage == "ar";
        public bool IsRtl => IsArabic;

        public string CurrentLanguage
        {
            get
            {
                var two = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
                return Supported.Contains(two) ? two : "en";
            }
        }

        public string this[string key] => Get(key);

        public string Get(string key) => Get(key, CurrentLanguage);

        public string Get(string key, string culture)
        {
            var lang = Normalize(culture);
            var dict = Cache.GetOrAdd(lang, LoadLanguage);
            if (dict.TryGetValue(key, out var value))
                return value;

            if (lang != "en")
            {
                var en = Cache.GetOrAdd("en", LoadLanguage);
                if (en.TryGetValue(key, out var fallback))
                    return fallback;
            }

            return key;
        }

        private static string Normalize(string? culture)
        {
            var two = (culture ?? "en").Trim().ToLowerInvariant();
            if (two.Length > 2)
                two = two.Split('-', '_')[0];
            return Supported.Contains(two) ? two : "en";
        }

        private Dictionary<string, string> LoadLanguage(string culture)
        {
            var path = Path.Combine(_env.ContentRootPath, "Resources", "lang", $"{culture}.json");
            if (!File.Exists(path))
                path = Path.Combine(_env.ContentRootPath, "Resources", "lang", "en.json");

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                   ?? new Dictionary<string, string>();
        }
    }
}
