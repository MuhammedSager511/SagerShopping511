using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using NToastNotify;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using webShopping.Data;
using webShopping.Models;

namespace webShopping.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext db;
        private readonly IEmailSender emailSender;
        private readonly IToastNotification toast;
        private readonly UserManager<IdentityUser> userManager;

        [BindProperty]
        public  ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(ApplicationDbContext db,
                             IEmailSender emailSender,
                             UserManager<IdentityUser> userManager,
                             IToastNotification toast)
        {
            this.db = db;
            this.emailSender = emailSender;
            this.toast = toast;
            this.userManager = userManager;
        }
        public IActionResult Index()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader=new Models.OrderHeader(),
                ListCart=db.ShoppingCarts.Where(i=>i.ApplicationUserId==claim.Value).Include(i=>i.Product)
            };
            ShoppingCartVM.OrderHeader.orderTotal = 0;
            ShoppingCartVM.OrderHeader.ApplicationUser = db.ApplicationUsers.FirstOrDefault(i => i.Id == claim.Value);

            foreach (var item in ShoppingCartVM.ListCart)
            {
                ShoppingCartVM.OrderHeader.orderTotal += (item.Count * item.Product.Price);
            }
           
            return View(ShoppingCartVM);
        }

        public IActionResult Add(int cartId)
        {
            var cart= db.ShoppingCarts.FirstOrDefault(i=>i.Id == cartId);
            cart.Count += 1;
            db.SaveChanges();
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Decrease(int cartId)
        {
            var cart = db.ShoppingCarts.FirstOrDefault(i => i.Id == cartId);
            if (cart.Count==1)
            {
                var count = db.ShoppingCarts.Where(i => i.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
                db.ShoppingCarts.Remove(cart);
                db.SaveChanges();
                HttpContext.Session.SetInt32(Diger.ssShoppingCart,count-1);
            }else
            {
                cart.Count -= 1;
                db.SaveChanges();
            }
          
            return RedirectToAction(nameof(Index));
        }


        public IActionResult Remove(int cartId)
        {
            var cart = db.ShoppingCarts.FirstOrDefault(i => i.Id == cartId);
    
            
                var count = db.ShoppingCarts.Where(i => i.ApplicationUserId == cart.ApplicationUserId).ToList().Count();
                db.ShoppingCarts.Remove(cart);
                db.SaveChanges();
              HttpContext.Session.SetInt32(Diger.ssShoppingCart, count - 1);
            
          

            return RedirectToAction(nameof(Index));
        }

        public IActionResult Summary()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCartVM = new ShoppingCartVM()
            {
                OrderHeader = new Models.OrderHeader(),
                ListCart = db.ShoppingCarts.Where(i => i.ApplicationUserId == claim.Value).Include(i => i.Product)
            };
            foreach (var item in ShoppingCartVM.ListCart) { 
                item.Price=item.Product.Price;
                ShoppingCartVM.OrderHeader.orderTotal += (item.Count * item.Product.Price);
            }
            return View(ShoppingCartVM);
        }
    

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Summary(ShoppingCartVM model)
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            ShoppingCartVM.ListCart=db.ShoppingCarts.Where(i=>i.ApplicationUserId==claim.Value).Include(i=>i.Product);
            ShoppingCartVM.OrderHeader.orderStatus=Diger.status_pending;
            ShoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;
            ShoppingCartVM.OrderHeader.orderDate=DateTime.Now;
            db.OrderHeaders.Add(ShoppingCartVM.OrderHeader);
            db.SaveChanges();
            foreach (var item in ShoppingCartVM.ListCart)
            {
                item.Price = item.Product.Price;
               orderDetails orderDetails=new orderDetails() 
               { 
                   productId = item.ProductId,
                   OrederId=ShoppingCartVM.OrderHeader.Id,
                   Price=item.Price,
                   count=item.Count,
               };
                ShoppingCartVM.OrderHeader.orderTotal += (item.Count * item.Product.Price);
                model.OrderHeader.orderTotal += (item.Count * item.Product.Price);
                db.orderDetailses.Add(orderDetails);
              


            }
            var payment=PaymentProcess(model);

            db.ShoppingCarts.RemoveRange(ShoppingCartVM.ListCart);
            db.SaveChanges();
            toast.AddSuccessToastMessage("The order has been confirmed successfully....");
            HttpContext.Session.SetInt32(Diger.ssShoppingCart,0);


            return RedirectToAction(nameof(SiparisTamam));
        }

        private Payment PaymentProcess(ShoppingCartVM model)
        {
            Options options = new Options();
            options.ApiKey = "sandbox-AKt0qx1u2wdXZXKrVHyqkgnNqMP011CT";
            options.SecretKey = "sandbox-J9u0UBlT1nn1KT4btgpRqzS4ErlNXAe9";
            options.BaseUrl = "https://sandbox-api.iyzipay.com";

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = new Random().Next(1111,9999).ToString();
            request.Price = model.OrderHeader.orderTotal.ToString();
            request.PaidPrice = model.OrderHeader.orderTotal.ToString();
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = "B67832";
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            //PaymentCard paymentCard = new PaymentCard();
            //paymentCard.CardHolderName = "John Doe";
            //paymentCard.CardNumber = "5528790000000008";
            //paymentCard.ExpireMonth = "12";
            //paymentCard.ExpireYear = "2030";
            //paymentCard.Cvc = "123";
            //paymentCard.RegisterCard = 0;
            //request.PaymentCard = paymentCard;



            PaymentCard paymentCard = new PaymentCard();
            paymentCard.CardHolderName = model.OrderHeader.CartName;
            paymentCard.CardNumber = model.OrderHeader.CartNumber;
            paymentCard.ExpireMonth = model.OrderHeader.ExpirationMonth;
            paymentCard.ExpireYear = model.OrderHeader.ExpiratioYear;
            paymentCard.Cvc = model.OrderHeader.CVC;
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;


            Buyer buyer = new Buyer();
            buyer.Id = model.OrderHeader.Id.ToString();
            buyer.Name = model.OrderHeader.Name;
            buyer.Surname = model.OrderHeader.LastName;
            buyer.GsmNumber = model.OrderHeader.PhoneNumber;
            buyer.Email = "email@email.com";
            buyer.IdentityNumber = "74300864791";
            buyer.LastLoginDate = "2015-10-05 12:43:35";
            buyer.RegistrationDate = "2013-04-21 15:12:09";
            buyer.RegistrationAddress = model.OrderHeader.Addres;
            buyer.Ip = "85.34.78.112";
            buyer.City = model.OrderHeader.sehir;
            buyer.Country = "Turkey";
            buyer.ZipCode = model.OrderHeader.PostKodu;
            request.Buyer = buyer;

            Address shippingAddress = new Address();
            shippingAddress.ContactName = "Jane Doe";
            shippingAddress.City = "Istanbul";
            shippingAddress.Country = "Turkey";
            shippingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            shippingAddress.ZipCode = "34742";
            request.ShippingAddress = shippingAddress;

            Address billingAddress = new Address();
            billingAddress.ContactName = "Jane Doe";
            billingAddress.City = "Istanbul";
            billingAddress.Country = "Turkey";
            billingAddress.Description = "Nidakule Göztepe, Merdivenköy Mah. Bora Sok. No:1";
            billingAddress.ZipCode = "34742";
            request.BillingAddress = billingAddress;

            List<BasketItem> basketItems = new List<BasketItem>();

            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);

            foreach (var item in db.ShoppingCarts.Where(i=>i.ApplicationUserId==claim.Value)
                                                 .Include(i=>i.Product))
            {
                basketItems.Add(new BasketItem()
                {
                    Id=item.Id.ToString(),
                    Name=item.Product.Name,
                    Category1=item.Product.CategoryId.ToString(),
                    ItemType=BasketItemType.PHYSICAL.ToString(),
                    Price=(item.Price *item.Count).ToString()
                });
            }

            request.BasketItems = basketItems;


           return Payment.Create(request, options);
        }

        public IActionResult SiparisTamam( )
        {
            return View();
        }




            [HttpPost]
        [ActionName("Index")]
        public async Task<IActionResult> IndexPOST()
        {
            var claimIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var user=db.ApplicationUsers.FirstOrDefault(i => i.Id == claim.Value);
            if (user==null)
            {
                ModelState.AddModelError(string.Empty, "verification email is empty...");
            }



            var userId = await userManager.GetUserIdAsync(user);



            var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Page(
                "/Account/ConfirmEmail",
                pageHandler: null,
                values: new { area = "Identity", userId = userId, code = code},
                protocol: Request.Scheme);

            await emailSender.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl)}'>clicking here</a>.");

            ModelState.AddModelError(string.Empty, "send email verification code...");

            return RedirectToAction("Success");
        }
        public IActionResult Success()
        {
            return View();
        }



    }
}
