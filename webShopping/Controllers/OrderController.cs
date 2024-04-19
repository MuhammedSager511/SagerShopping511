using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext db;
        [BindProperty]
        public OrderDetailsVM orderMv {  get; set; }    
        public OrderController(ApplicationDbContext db)
        {
            this.db = db;
        }
        [HttpPost]
        [Authorize(Roles =Diger.Role_Admin)]    
        public IActionResult Approved()
        { 
            OrderHeader orderHeader=db.OrderHeaders.FirstOrDefault(i=>i.Id==orderMv.OrderHeader.Id);
            orderHeader.orderStatus=Diger.status_confirmed;
            db.SaveChanges();
            return RedirectToAction("Index");
        
        
        }

        [HttpPost]
        [Authorize(Roles = Diger.Role_Admin)]
        public IActionResult ShipIt()
        {
            OrderHeader orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == orderMv.OrderHeader.Id);
            orderHeader.orderStatus = Diger.status_cargo;
            db.SaveChanges();
            return RedirectToAction("Index");


        }



        public IActionResult Details(int id)
        {
            orderMv = new OrderDetailsVM
            {
                OrderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id),
                orderDetails=db.orderDetailses.Where(x=>x.OrederId == id).Include(x=>x.product)

            };
            return View(orderMv);
        }
        public IActionResult Index()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(Diger.Role_Admin))
            {
                    orderHeadersList=db.OrderHeaders.ToList();
            }
            else
            {
                orderHeadersList = db.OrderHeaders.Where(i=>i.ApplicationUserId==claim.Value)
                                    .Include(i=>i.ApplicationUser);
            }

            return View(orderHeadersList);
        }
        public IActionResult pending()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(Diger.Role_Admin))
            {
                orderHeadersList = db.OrderHeaders.Where(i => i.orderStatus == Diger.status_pending);
            }
            else
            {
                orderHeadersList = db.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value
                                                      && i.orderStatus == Diger.status_pending)
                                    .Include(i => i.ApplicationUser);
            }

            return View(orderHeadersList);
        }
        public IActionResult confirmed()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(Diger.Role_Admin))
            {
                orderHeadersList = db.OrderHeaders.Where(i => i.orderStatus == Diger.status_confirmed);
            }
            else
            {
                orderHeadersList = db.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value
                                                      && i.orderStatus == Diger.status_confirmed)
                                    .Include(i => i.ApplicationUser);
            }

            return View(orderHeadersList);
        }

        public IActionResult cargo()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(Diger.Role_Admin))
            {
                orderHeadersList = db.OrderHeaders.Where(i => i.orderStatus == Diger.status_cargo);
            }
            else
            {
                orderHeadersList = db.OrderHeaders.Where(i => i.ApplicationUserId == claim.Value
                                                      && i.orderStatus == Diger.status_cargo)
                                    .Include(i => i.ApplicationUser);
            }

            return View(orderHeadersList);
        }
    }
}
