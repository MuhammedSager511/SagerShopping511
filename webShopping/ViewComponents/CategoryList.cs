using Microsoft.AspNetCore.Mvc;
using webShopping.Data;

namespace webShopping.ViewComponents
{
    public class CategoryList:ViewComponent
    {
        private readonly ApplicationDbContext db;

        public CategoryList(ApplicationDbContext db)
        {
            this.db = db;
        }
        public IViewComponentResult Invoke()
        {
            var caregory=db.Categoties.ToList();
            return View(caregory);
        }
    }
}
