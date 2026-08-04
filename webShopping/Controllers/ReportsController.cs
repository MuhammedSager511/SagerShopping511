using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webShopping.Data;
using webShopping.Models;
using webShopping.Services;

namespace webShopping.Controllers
{
    [Authorize(Roles = Diger.Role_Admin)]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _db;

        public ReportsController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var completed = new[] { Diger.status_confirmed, Diger.status_cargo, Diger.status_preparing };
            var since14 = DateTime.UtcNow.Date.AddDays(-13);

            var daily = await _db.OrderHeaders.AsNoTracking()
                .Where(o => completed.Contains(o.orderStatus) && o.orderDate >= since14)
                .GroupBy(o => o.orderDate.Date)
                .Select(g => new { Day = g.Key, Total = g.Sum(x => x.orderTotal), Count = g.Count() })
                .OrderBy(x => x.Day)
                .ToListAsync();

            var since6m = DateTime.UtcNow.Date.AddMonths(-5);
            var monthly = await _db.OrderHeaders.AsNoTracking()
                .Where(o => completed.Contains(o.orderStatus) && o.orderDate >= since6m)
                .GroupBy(o => new { o.orderDate.Year, o.orderDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Total = g.Sum(x => x.orderTotal), Count = g.Count() })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync();

            ViewBag.DailySales = daily;
            ViewBag.MonthlySales = monthly;
            ViewBag.CompletedCount = await _db.OrderHeaders.CountAsync(o => completed.Contains(o.orderStatus));
            ViewBag.CancelledCount = await _db.OrderHeaders.CountAsync(o => o.orderStatus == Diger.status_cancelled);
            ViewBag.Revenue = await _db.OrderHeaders
                .Where(o => completed.Contains(o.orderStatus))
                .SumAsync(o => (double?)o.orderTotal) ?? 0;

            ViewBag.BestSellers = await _db.orderDetailses.AsNoTracking()
                .GroupBy(d => d.productId)
                .Select(g => new { ProductId = g.Key, Sold = g.Sum(x => x.count), Revenue = g.Sum(x => x.count * x.Price) })
                .OrderByDescending(x => x.Sold)
                .Take(10)
                .ToListAsync();

            ViewBag.BestSellerProducts = await _db.Products.AsNoTracking()
                .Where(p => _db.orderDetailses.Any(d => d.productId == p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            ViewBag.LowStock = await _db.Products.AsNoTracking()
                .Where(p => p.StockQuantity <= StockHelper.LimitedThreshold)
                .OrderBy(p => p.StockQuantity)
                .Take(20)
                .ToListAsync();

            ViewBag.TopCustomers = await _db.OrderHeaders.AsNoTracking()
                .Where(o => completed.Contains(o.orderStatus))
                .GroupBy(o => o.ApplicationUserId)
                .Select(g => new
                {
                    UserId = g.Key,
                    Orders = g.Count(),
                    Total = g.Sum(x => x.orderTotal),
                    Name = g.Max(x => x.Name + " " + x.LastName)
                })
                .OrderByDescending(x => x.Total)
                .Take(10)
                .ToListAsync();

            ViewBag.Coupons = await _db.Coupons.AsNoTracking()
                .OrderByDescending(c => c.UsedCount)
                .ThenBy(c => c.Code)
                .Take(20)
                .ToListAsync();

            ViewBag.CouponOrderCount = await _db.OrderHeaders.AsNoTracking()
                .CountAsync(o => o.CouponCode != null && o.CouponCode != "");
            ViewBag.CouponDiscountTotal = await _db.OrderHeaders.AsNoTracking()
                .Where(o => o.DiscountUsd > 0)
                .SumAsync(o => (double?)o.DiscountUsd) ?? 0;
            ViewBag.SaleProducts = await _db.Products.AsNoTracking()
                .Where(p => p.SalePrice != null && p.SalePrice > 0 && p.SalePrice < p.Price)
                .OrderByDescending(p => p.Price - p.SalePrice!.Value)
                .Take(15)
                .ToListAsync();

            return View();
        }
    }
}
