using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webShopping.Data;

namespace webShopping.ViewComponents
{
    public class CategoryList : ViewComponent
    {
        private readonly ApplicationDbContext _db;

        public CategoryList(ApplicationDbContext db) => _db = db;

        public IViewComponentResult Invoke()
        {
            var categories = _db.Categoties
                .AsNoTracking()
                .OrderBy(c => c.SortOrder)
                .ThenBy(c => c.Name)
                .ToList();
            return View(categories);
        }
    }
}
