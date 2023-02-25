using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    [Authorize]
    public class CartController : Controller
    {
        private readonly IUnitOfWork _uw;
        private readonly IEmailSender _em;
        [BindProperty]
        public ShoppingCartVM ShoppingCartVM { get; set; }
        public CartController(IUnitOfWork uw , IEmailSender em)
        {
            _uw = uw;
            _em = em;
        }
        public IActionResult Index()
        {
            //get id of user who logged in:
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _uw.ShoppingCart.GetAll
                (u=>u.ApplicationUserId==claim.Value,includeProps:"Product"),
                OrderHeader = new()

            };
            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                    cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Count * cart.Price);
            }

            return View(shoppingCartVM);
        }


        public IActionResult Summary()
        {
            //get id of user who logged in:
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            ShoppingCartVM shoppingCartVM = new ShoppingCartVM()
            {
                ListCart = _uw.ShoppingCart.GetAll
                (u => u.ApplicationUserId == claim.Value, includeProps: "Product"),
                OrderHeader = new()
            };

            shoppingCartVM.OrderHeader.ApplicationUser = _uw.ApplicationUser.GetFirstOrDefault(
                u => u.Id == claim.Value);

            shoppingCartVM.OrderHeader.Name = shoppingCartVM.OrderHeader.ApplicationUser.Name;
            shoppingCartVM.OrderHeader.PhoneNumber = shoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber;
            shoppingCartVM.OrderHeader.StreetAddress = shoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
            shoppingCartVM.OrderHeader.City = shoppingCartVM.OrderHeader.ApplicationUser.City;
            shoppingCartVM.OrderHeader.State = shoppingCartVM.OrderHeader.ApplicationUser.State;
            shoppingCartVM.OrderHeader.PostalCode = shoppingCartVM.OrderHeader.ApplicationUser.PostalCode;


            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                    cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Count * cart.Price);
            }

            return View(shoppingCartVM);
        }


        //https://localhost:7004/

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public IActionResult SummaryPost(ShoppingCartVM shoppingCartVM) 
        {
            //get id of user who logged in:
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            shoppingCartVM.ListCart = _uw.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value, includeProps: "Product");

            shoppingCartVM.OrderHeader.ApplicationUser = _uw.ApplicationUser.GetFirstOrDefault(
                u => u.Id == claim.Value);

           


            
            shoppingCartVM.OrderHeader.OrderDate = System.DateTime.Now;
            shoppingCartVM.OrderHeader.ApplicationUserId = claim.Value;

            

            foreach (var cart in shoppingCartVM.ListCart)
            {
                cart.Price = GetPriceBasedOnQuantity(cart.Count, cart.Product.Price,
                    cart.Product.Price50, cart.Product.Price100);
                shoppingCartVM.OrderHeader.OrderTotal += (cart.Count * cart.Price);
            }
            
            //set company payment
            ApplicationUser appuser = _uw.ApplicationUser.GetFirstOrDefault(u => u.Id == claim.Value);

            if(appuser.CompanyId.GetValueOrDefault() == 0)
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
            }
            else
            {
                shoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
                shoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            }


            _uw.OrderHeader.Add(shoppingCartVM.OrderHeader);
            _uw.save();

            foreach (var cart in shoppingCartVM.ListCart)
            {
                OrderDetail orderDetail = new()
                {
                    ProductId = cart.ProductId,
                    OrderId = shoppingCartVM.OrderHeader.Id,
                    Price=  cart.Price,
                    Count = cart.Count,
                };
                _uw.OrderDetail.Add(orderDetail);
                _uw.save();
            }


            
            if (appuser.CompanyId.GetValueOrDefault() == 0)
            {
                //stripe settings
                var domain = "https://localhost:7004/";
                var options = new SessionCreateOptions
                {
                    PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                    LineItems = new List<SessionLineItemOptions>(),
                    Mode = "payment",
                    SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={shoppingCartVM.OrderHeader.Id}",
                    CancelUrl = domain + $"customer/cart/index",
                };


                foreach (var item in shoppingCartVM.ListCart)
                {

                    var sessionLineItem = new SessionLineItemOptions
                    {
                        PriceData = new SessionLineItemPriceDataOptions
                        {
                            UnitAmount = (long)(item.Price * 100),//20.00 -> 2000
                            Currency = "usd",
                            ProductData = new SessionLineItemPriceDataProductDataOptions
                            {
                                Name = item.Product.Title
                            },

                        },
                        Quantity = item.Count,
                    };
                    options.LineItems.Add(sessionLineItem);

                }

                var service = new SessionService();
                Session session = service.Create(options); //error here: ==
                                                           //Stripe.StripeException: 'Not a valid URL'


                _uw.OrderHeader.UpdateStripePayment(shoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
                _uw.save();

                Response.Headers.Add("Location", session.Url);
                return new StatusCodeResult(303);
            }
            else
            {
                return RedirectToAction("OrderConfirmation", "Cart",
                    new { id = shoppingCartVM.OrderHeader.Id });
            }

            
        }





        public IActionResult OrderConfirmation(int id)
        {
            OrderHeader or = _uw.OrderHeader.GetFirstOrDefault(u => u.Id == id);

            if(or.PaymentStatus != SD.PaymentStatusDelayedPayment)
            {
                //create session
                var service = new SessionService();
                Session session = service.Get(or.SessionId);

                //check stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _uw.OrderHeader.UpdateStripePayment(id, or.SessionId, session.PaymentIntentId);
                    _uw.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                    _uw.save();
                }
            }

            _em.SendEmailAsync(or.ApplicationUser.Email,
                "New Order - Bulky Book", "<p>New Order Created</p>");


            //delete shoppingCartVM
            List<ShoppingCart> shoppingCartVM = _uw.ShoppingCart.GetAll(
                u => u.ApplicationUserId == or.ApplicationUserId).ToList();
            _uw.ShoppingCart.RemoveRange(shoppingCartVM);
            _uw.save();

            return View(id);
        }


        public IActionResult Plus(int cartId)
        {
            var cart = _uw.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            _uw.ShoppingCart.IncrementCount(cart, 1);
            _uw.save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Minus(int cartId)
        {
            var cart = _uw.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            if (cart.Count <= 1)
            {
                _uw.ShoppingCart.Remove(cart);
                var count = _uw.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count - 1;
                HttpContext.Session.SetInt32(SD.SessionCart, count);
            }
            else
            {
                _uw.ShoppingCart.DecrementCount(cart, 1);
            }
            _uw.save();
            return RedirectToAction(nameof(Index));
        }

        public IActionResult Remove(int cartId)
        {
            var cart = _uw.ShoppingCart.GetFirstOrDefault(u => u.Id == cartId);
            _uw.ShoppingCart.Remove(cart);
            _uw.save();
            var count = _uw.ShoppingCart.GetAll(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;
            HttpContext.Session.SetInt32(SD.SessionCart, count);
            return RedirectToAction(nameof(Index));
        }



        private double GetPriceBasedOnQuantity(double quantity, double price, double price50, double price100)
        {
            if (quantity <= 50)
            {
                return price;
            }
            else
            {
                if (quantity <= 100)
                {
                    return price50;
                }
                return price100;
            }
        }
    }
}
