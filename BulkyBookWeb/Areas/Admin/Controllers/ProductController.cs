using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Hosting;
using NToastNotify;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class ProductController : Controller

    {
        //add obj from dbcontext
        private readonly IUnitOfWork _uw;
        private readonly IToastNotification _tn;
        private readonly IWebHostEnvironment _he;

        public ProductController(IUnitOfWork uw , IWebHostEnvironment he , IToastNotification tn)
        {
            _uw = uw;
            _he = he;
            _tn = tn;
        }


        public IActionResult Index()
        {
            return View();
        }
        // [2] Upsert
        //get
        public IActionResult Upsert(int? id)
        {
            ProductVM Productvm = new()
            {
                Product = new(),
                CategoryList = _uw.Category.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
                CoverTypeList = _uw.TypeModel.GetAll().Select(i => new SelectListItem
                {
                    Text = i.Name,
                    Value = i.Id.ToString()
                }),
            };
            if (id == null || id == 0)
            {
                return View(Productvm);
            }
            else
            {
                Productvm.Product = _uw.Product.GetFirstOrDefault(u => u.Id == id);
                return View(Productvm);
            }
        }
        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(ProductVM obj , IFormFile? file)
        {
            // adjust photo saving and editing
            string wwwRootPath = _he.WebRootPath;
            if(file != null)
            {
                string fileName = Guid.NewGuid().ToString();
                var uploads = Path.Combine(wwwRootPath, @"Images\Products");
                var extension = Path.GetExtension(file.FileName);
                //adding something here while updating
                if(obj.Product.ImageUrl != null)
                {
                    var oldImgPath = Path.Combine(wwwRootPath, obj.Product.ImageUrl.TrimStart('\\'));
                    if (System.IO.File.Exists(oldImgPath))
                    {
                        System.IO.File.Delete(oldImgPath);
                    }
                }
                using(var fileStream = new FileStream(Path.Combine(uploads , fileName + extension), FileMode.Create))
                {
                    file.CopyTo(fileStream);
                }
                obj.Product.ImageUrl = @"\Images\Products\" + fileName + extension;
            }
            if (ModelState.IsValid)
            {
                if(obj.Product.Id == 0)
                {
                    _uw.Product.Add(obj.Product);
                    _uw.save();
                    _tn.AddSuccessToastMessage("Product Created Successfully");
                }
                else
                {
                    _uw.Product.Update(obj.Product);
                    _uw.save();
                    _tn.AddSuccessToastMessage("Product Updated Successfully");

                }
                return RedirectToAction("Index");
            }
            return View(obj);
        }
        //add endpoints here : 
        #region API CALLS

        [HttpGet]
        public IActionResult GetAll()
        {
            //include props are case sensetive so take care
            var ProductList = _uw.Product.GetAll(includeProps: "Category,TypeModel");
            return Json(new
            {
                data = ProductList
            });
        }
        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _uw.Product.GetFirstOrDefault(u => u.Id == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }
            var oldImagePath = Path.Combine(_he.WebRootPath, obj.ImageUrl.TrimStart('\\'));
            if (System.IO.File.Exists(oldImagePath))
            {
                System.IO.File.Delete(oldImagePath);
            }
            _uw.Product.Remove(obj);
            _uw.save();
            return Json(new { success = true, message = "Delete Successful" });

        }
        #endregion
    }
}
