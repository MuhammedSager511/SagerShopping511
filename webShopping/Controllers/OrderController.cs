using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.Security.Claims;
using System.Text;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IToastNotification toast;

        [BindProperty]
        public OrderDetailsVM orderMv {  get; set; }    
        public OrderController(ApplicationDbContext db, IToastNotification toast)
        {
            this.db = db;
            this.toast = toast;
        }


        public IActionResult Index()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            IEnumerable<OrderHeader> orderHeadersList;
            if (User.IsInRole(Diger.Role_Admin))
            {
                orderHeadersList = db.OrderHeaders
                                     .Include(o => o.orderDetails)
                                     .ThenInclude(d => d.product)
                                     .ToList();
            }
            else
            {
                orderHeadersList = db.OrderHeaders
                                     .Where(i => i.ApplicationUserId == claim.Value)
                                     .Include(i => i.ApplicationUser)
                                     .Include(o => o.orderDetails)
                                     .ThenInclude(d => d.product)
                                     .ToList();
            }

            return View(orderHeadersList);
        }
        /// <summary>
        /// تم موافقة على الطلب من قبل الادمن
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = Diger.Role_Admin)]
        public IActionResult Approved()
        {
            OrderHeader orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == orderMv.OrderHeader.Id);
            orderHeader.orderStatus = Diger.status_confirmed;
            db.SaveChanges();
            return RedirectToAction("Index");


        }
        /// <summary>
        /// تم شحن المنتج من قبل الادمن
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Authorize(Roles = Diger.Role_Admin)]
        public IActionResult ShipIt()
        {
            OrderHeader orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == orderMv.OrderHeader.Id);
            orderHeader.orderStatus = Diger.status_cargo;
            db.SaveChanges();
            return RedirectToAction("Index");


        }




        /// <summary>
        /// حيث ان منتج معلق او على قيد الانتظار 
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// تم تم التأكيد على طلبك للمنتج
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// المنتجات نقل للتسليم للعملاء
        /// </summary>
        /// <returns></returns>
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
        /// <summary>
        /// حذف منتج
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IActionResult Delete(int id)
        {
            var orderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id);
            if (orderHeader == null)
            {
                return NotFound();
            }


         
            db.OrderHeaders.Remove(orderHeader);

           
            db.SaveChanges();
            toast.AddSuccessToastMessage("The active request has been deleted....");
            return RedirectToAction("Index");
        }
        public IActionResult Details(int id)
        {
            orderMv = new OrderDetailsVM
            {
                OrderHeader = db.OrderHeaders.FirstOrDefault(i => i.Id == id),
                orderDetails = db.orderDetailses.Where(x => x.OrederId == id).Include(x => x.product)

            };
            return View(orderMv);
        }
   
       
    }
}
