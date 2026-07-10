using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Json;

namespace webShopping.Services
{
    public class AppLocalizer : IAppLocalizer
    {
        private static readonly ConcurrentDictionary<string, Dictionary<string, string>> Cache = new();
        private readonly IWebHostEnvironment _env;

        public AppLocalizer(IWebHostEnvironment env)
        {
            _env = env;
        }

        public bool IsArabic => CurrentLanguage == "ar";
        public string CurrentLanguage => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? "ar" : "en";

        public string this[string key] => Get(key);

        public string Get(string key) => Get(key, CurrentLanguage);

        public string Get(string key, string culture)
        {
            var lang = culture == "ar" ? "ar" : "en";
            var dict = Cache.GetOrAdd(lang, LoadLanguage);
            return dict.TryGetValue(key, out var value) ? value : key;
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
