using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyBookWeb.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IUnitOfWork _uw;

        public HomeController(ILogger<HomeController> logger , IUnitOfWork uw)
        {
            _logger = logger;
            _uw = uw;
        }
        public IActionResult Index()
        {
            IEnumerable<Product> productList = _uw.Product.GetAll(includeProps: "Category,TypeModel");
            return View(productList);
        }
        public IActionResult Details(int productId)
        {
            ShoppingCart carobj = new()
            {
                Count = 1,
                ProductId = productId,
                Product = _uw.Product.GetFirstOrDefault(u => u.Id == productId, includeProps: "Category,TypeModel")
            };
            return View(carobj);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Details(ShoppingCart shoppingCart)
        {
            //shoppingCart.ProductId = 0;
            //get id of user who logged in:
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            shoppingCart.ApplicationUserId = claim.Value;
            //make sure not add alredy exist cart
            ShoppingCart cartfromdb = _uw.ShoppingCart.GetFirstOrDefault
                (u => u.ApplicationUserId == claim.Value && u.ProductId == shoppingCart.ProductId);

            if(cartfromdb == null)
            {
                _uw.ShoppingCart.Add(shoppingCart);

                _uw.save();
                HttpContext.Session.SetInt32(SD.SessionCart,
               _uw.ShoppingCart.GetAll(u => u.ApplicationUserId == claim.Value).ToList().Count);
            }
            else
            {
                _uw.ShoppingCart.IncrementCount(cartfromdb, shoppingCart.Count);
                _uw.save();
            }
            return RedirectToAction(nameof(Index));
        }
        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}