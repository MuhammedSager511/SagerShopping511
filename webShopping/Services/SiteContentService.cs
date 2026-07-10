using System.Globalization;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Services
{
    public class SiteContentService : ISiteContentService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private SiteSettings _settings = new();
        private List<QuickLink> _quickLinks = new();
        private readonly object _lock = new();

        public SiteContentService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
            Refresh();
        }

        public SiteSettings Settings => _settings;
        public IReadOnlyList<QuickLink> QuickLinks => _quickLinks;

        public string T(string en, string ar) =>
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ar" ? ar : en;

        public void Refresh()
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            lock (_lock)
            {
                _settings = db.SiteSettings.OrderBy(s => s.Id).FirstOrDefault() ?? new SiteSettings();
                _quickLinks = db.QuickLinks
                    .Where(q => q.IsActive)
                    .OrderBy(q => q.SortOrder)
                    .ToList();
            }
        }
    }
}
