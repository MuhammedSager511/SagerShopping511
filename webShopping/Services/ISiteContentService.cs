using webShopping.Models;

namespace webShopping.Services
{
    public interface ISiteContentService
    {
        SiteSettings Settings { get; }
        IReadOnlyList<QuickLink> QuickLinks { get; }
        string T(string en, string ar);
        void Refresh();
    }
}
