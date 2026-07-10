using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webShopping.Data;

namespace webShopping.Controllers
{
    public class SitemapController : Controller
    {
        private readonly ApplicationDbContext _db;

        public SitemapController(ApplicationDbContext db) => _db = db;

        [Route("sitemap.xml")]
        [ResponseCache(Duration = 3600)]
        public async Task<IActionResult> Index()
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var urls = new List<(string loc, string? changefreq, double? priority)>
            {
                ($"{baseUrl}/", "daily", 1.0),
                ($"{baseUrl}/Home/Shop", "daily", 0.9),
                ($"{baseUrl}/Home/About", "monthly", 0.5),
                ($"{baseUrl}/Home/Terms", "monthly", 0.4),
                ($"{baseUrl}/Home/Refund", "monthly", 0.4),
            };

            var products = await _db.Products.Select(p => p.Id).ToListAsync();
            foreach (var id in products)
                urls.Add(($"{baseUrl}/Home/Details/{id}", "weekly", 0.8));

            var ns = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
            var doc = new XDocument(
                new XDeclaration("1.0", "utf-8", null),
                new XElement(ns + "urlset",
                    urls.Select(u => new XElement(ns + "url",
                        new XElement(ns + "loc", u.loc),
                        new XElement(ns + "changefreq", u.changefreq),
                        new XElement(ns + "priority", u.priority?.ToString("F1", System.Globalization.CultureInfo.InvariantCulture))
                    ))
                )
            );

            return Content(doc.ToString(), "application/xml", Encoding.UTF8);
        }
    }
}
