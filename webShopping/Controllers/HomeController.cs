using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Diagnostics;
using System.Security.Claims;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext db;
        private readonly IToastNotification toast;

        public HomeController(ILogger<HomeController> logger,ApplicationDbContext context, IToastNotification toast)
        {
            _logger = logger;
            this.db = context;
            this.toast = toast;
        }


        public IActionResult Search(string q)
        {
            if (!String.IsNullOrEmpty(q))
            {
                //||i.Description.Contains(q)
                var sea =db.Products.Where(i=>i.Name.Contains(q));
                return View(sea);
            }
            return View();
        }

        public IActionResult CategoryDetails(int id)
        {
            var product=db.Products.Where(i=>i.CategoryId==id).ToList();
            ViewBag.KategoryId = id;
            return View(product);
        }



        //public IActionResult Index()

        //{

        //    var product=db.Products.Where(i => i.IsHome).ToList();
        //    var claimIdentity = (ClaimsIdentity)User.Identity;
        //    var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
        //    if (claim!=null)
        //    {
        //        var count=db.ShoppingCarts.Where(i=>i.ApplicationUserId==claim.Value).Count();
        //        HttpContext.Session.SetInt32(Diger.ssShoppingCart, count);
        //    }

        //    return View(product);
        //}
        public IActionResult Index(int page = 1, int pageSize = 7)
        {
            
            var totalProducts = db.Products.Where(i => i.IsHome).Count();
            var totalPages = (int)Math.Ceiling((double)totalProducts / pageSize);

            // ÊÍãíá ÇáÚäÇÕÑ ááÕÝÍÉ ÇáÍÇáíÉ
            var products = db.Products
                                    .Where(i => i.IsHome)
                                    .OrderBy(p => p.Id)
                                    .Skip((page - 1) * pageSize)
                                    .Take(pageSize)
                                    .ToList();

            // äÞá ÇáãÚáæãÇÊ ááÚÑÖ
            ViewData["TotalPages"] = totalPages;
            ViewData["CurrentPage"] = page;
            return View(products);
        }

        public IActionResult Privacy()
        {
            return View();
        }


        public IActionResult Details(int Id)
        {
            var prodcut = db.Products.FirstOrDefault(i => i.Id == Id);
            ShoppingCart cart = new ShoppingCart()
            {
                Product = prodcut,
                ProductId = prodcut.Id

            };
           
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart Scart)
        {
            Scart.Id = 0;
            if (!ModelState.IsValid)
            {
                var claimIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
                Scart.ApplicationUserId = claim.Value;
                ShoppingCart cart = db.ShoppingCarts.FirstOrDefault(
                    u => u.ApplicationUserId == Scart.ApplicationUserId && u.ProductId == Scart.ProductId);
                if (cart == null)
                {
                    db.ShoppingCarts.Add(Scart);
                }
                else
                {
                    cart.Count += Scart.Count;
                }
                db.SaveChanges();
                toast.AddSuccessToastMessage("The product has been added to the cart successfully....");
                var count=db.ShoppingCarts.Where(i=>i.ApplicationUserId==Scart.ApplicationUserId).ToList().Count();
                HttpContext.Session.SetInt32(Diger.ssShoppingCart,count);
                return RedirectToAction("Index");
            }
            else
            {
                var prodcut = db.Products.FirstOrDefault(i => i.Id == Scart.Id);
                ShoppingCart cart = new ShoppingCart()
                {
                    Product = prodcut,
                    ProductId = prodcut.Id

                };
            }

            return View(Scart);
        }


        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
