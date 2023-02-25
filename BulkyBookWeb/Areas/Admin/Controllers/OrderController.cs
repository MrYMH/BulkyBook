using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.TestHelpers;
using System.Security.Claims;
using RefundService = Stripe.RefundService;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    [Authorize(Roles = SD.Role_Admin)]
    public class OrderController : Controller
	{
        private readonly IUnitOfWork _uw;
        [BindProperty]
        public OrderVM OrVM { get; set; }

        public OrderController(IUnitOfWork uw)
        {
            _uw = uw;
        }

        public IActionResult Index()
		{
			return View();
		}

        public IActionResult Details(int orderId)
        {
            OrVM = new OrderVM()
            {
                OrderHeader = _uw.OrderHeader.GetFirstOrDefault
                    (u => u.Id == orderId , includeProps: "ApplicationUser"),
                OrderDetail = _uw.OrderDetail.GetAll
                    (u => u.OrderId == orderId, includeProps: "Product"),
            };
            return View(OrVM);
        }


        [HttpPost]
        [ActionName("Details")]
        [ValidateAntiForgeryToken]
        public IActionResult Pay_Now()
        {
            OrVM.OrderDetail = _uw.OrderDetail.GetAll
                (u => u.OrderId == OrVM.OrderHeader.Id, includeProps: "Product");
            OrVM.OrderHeader = _uw.OrderHeader.GetFirstOrDefault
                (u => u.Id == OrVM.OrderHeader.Id, includeProps: "ApplicationUser");


            //stripe settings
            var domain = "https://localhost:7004";
            var options = new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string>
                {
                  "card",
                },
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderid={OrVM.OrderHeader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrVM.OrderHeader.Id}",
            };


            foreach (var item in OrVM.OrderDetail)
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


            _uw.OrderHeader.UpdateStripePayment(OrVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _uw.save();

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _uw.OrderHeader.UpdateStatus(OrVM.OrderHeader.Id , SD.StatusInProcess);
            _uw.save();
            TempData["Success"] = "Order Status Updated Successfully.";
            return RedirectToAction("Details", "Order", new
                { orderId = OrVM.OrderHeader.Id });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _uw.OrderHeader.GetFirstOrDefault(u => u.Id == OrVM.OrderHeader.Id, includeProps: "ApplicationUser", tracked: false);
            //update some props
            orderHeader.TrackingNumber = OrVM.OrderHeader.TrackingNumber;
            orderHeader.Carrier = OrVM.OrderHeader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShippingDate = DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
            }
            //call update
            _uw.OrderHeader.Update(orderHeader);
            _uw.save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = OrVM.OrderHeader.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [ValidateAntiForgeryToken]
        public IActionResult CancelOrder()
        {
            var orderHeader = _uw.OrderHeader.GetFirstOrDefault(u => u.Id == OrVM.OrderHeader.Id, includeProps: "ApplicationUser", tracked: false);
            if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service = new RefundService();
                Refund r = service.Create(options);
                _uw.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
            }
            else
            {
                _uw.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);

            }
            _uw.save();
            TempData["Success"] = "Order Canceled Successfully.";
            return RedirectToAction("Details", "Order", new { orderId = OrVM.OrderHeader.Id });

        }




        




        //PaymentConfirmation?orderHeaderid
        public IActionResult PaymentConfirmation(int orderHeaderid)
        {
            OrderHeader or = _uw.OrderHeader.GetFirstOrDefault(u => u.Id == orderHeaderid);

            if (or.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                //create session
                var service = new SessionService();
                Session session = service.Get(or.SessionId);

                //check stripe status
                if (session.PaymentStatus.ToLower() == "paid")
                {
                    //_uw.OrderHeader.UpdateStripePayment(orderHeaderid, or.SessionId, session.PaymentIntentId);
                    _uw.OrderHeader.UpdateStatus(orderHeaderid, or.OrderStatus, SD.PaymentStatusApproved);
                    _uw.save();
                }
            }

            

            return View(orderHeaderid);
        }



        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateOrderDetail()
        {
            var orderheaderfromdb = _uw.OrderHeader.GetFirstOrDefault(u => u.Id == OrVM.OrderHeader.Id, includeProps: "ApplicationUser" , tracked:false );

            orderheaderfromdb.Name = OrVM.OrderHeader.Name;
            orderheaderfromdb.PhoneNumber = OrVM.OrderHeader.PhoneNumber;
            orderheaderfromdb.StreetAddress = OrVM.OrderHeader.StreetAddress;
            orderheaderfromdb.City = OrVM.OrderHeader.City;
            orderheaderfromdb.State = OrVM.OrderHeader.State;
            orderheaderfromdb.PostalCode = OrVM.OrderHeader.PostalCode;
            if(OrVM.OrderHeader.Carrier != null)
            {
                orderheaderfromdb.Carrier = OrVM.OrderHeader.Carrier;
            }
            if (OrVM.OrderHeader.TrackingNumber != null)
            {
                orderheaderfromdb.TrackingNumber = OrVM.OrderHeader.TrackingNumber;
            }
            _uw.OrderHeader.Update(orderheaderfromdb);
            _uw.save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction("Details" , "Order" , new 
                { orderId = orderheaderfromdb.Id});
        }







        //add endpoints here : 
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll(string status)
        {
            IEnumerable<OrderHeader> ors;

            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
            {
                ors = _uw.OrderHeader.GetAll(includeProps: "ApplicationUser");
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
                ors = _uw.OrderHeader.GetAll(u => u.ApplicationUserId == claim.Value, includeProps: "ApplicationUser");
            }

            switch (status)
            {
                case "pending":
                    ors = ors.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    ors = ors.Where(u => u.OrderStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    ors = ors.Where(u => u.OrderStatus == SD.StatusShipped);
                    break;
                case "approved":
                    ors = ors.Where(u => u.OrderStatus == SD.StatusApproved);
                    break;
                default:
                    break;
            }
            return Json(new
            {
                data = ors
            });
        }



        




        #endregion
    }
}
