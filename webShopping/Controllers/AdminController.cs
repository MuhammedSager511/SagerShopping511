using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _db;

        public AdminController(ApplicationDbContext db)
        {
            _db = db;
        }

        public IActionResult Index()
        {
            ViewBag.TotalProducts = _db.Products.Count();
            ViewBag.TotalCategories = _db.Categoties.Count();
            ViewBag.TotalUsers = _db.Users.Count();
            ViewBag.TotalOrders = _db.OrderHeaders.Count();
            ViewBag.PendingOrders = _db.OrderHeaders.Count(o =>
                o.orderStatus == Diger.status_pending ||
                o.orderStatus == Diger.status_awaiting_payment ||
                o.orderStatus == Diger.status_payment_review);
            ViewBag.ConfirmedOrders = _db.OrderHeaders.Count(o => o.orderStatus == Diger.status_confirmed);
            ViewBag.CargoOrders = _db.OrderHeaders.Count(o => o.orderStatus == Diger.status_cargo);
            ViewBag.LowStock = _db.Products.Count(p => !p.IsStock);
            ViewBag.Revenue = _db.OrderHeaders
                .Where(o => o.orderStatus == Diger.status_confirmed || o.orderStatus == Diger.status_cargo)
                .Sum(o => (double?)o.orderTotal) ?? 0;

            ViewBag.RecentOrders = _db.OrderHeaders
                .Include(o => o.ApplicationUser)
                .OrderByDescending(o => o.orderDate)
                .Take(5)
                .ToList();

            ViewBag.TopProducts = _db.orderDetailses
                .GroupBy(d => d.productId)
                .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.count) })
                .OrderByDescending(x => x.Sold)
                .Take(5)
                .ToList()
                .Select(x => new
                {
                    Product = _db.Products.Find(x.ProductId),
                    x.Sold
                })
                .Where(x => x.Product != null)
                .ToList();

            return View();
        }
    }
}
