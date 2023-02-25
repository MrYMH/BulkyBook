using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NToastNotify;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CategoryController : Controller

    {
        //add obj from dbcontext
        private readonly IUnitOfWork _uw;
        private readonly IToastNotification _tn;


        public CategoryController(IUnitOfWork uw , IToastNotification tn)
        {
            _uw = uw;
            _tn = tn;
        }


        public IActionResult Index()
        {
            //get all data from categories table
            IEnumerable<Category> objCategoryList = _uw.Category.GetAll();
            return View(objCategoryList);
        }

        //[1] create
        //get
        public IActionResult Create()
        {

            return View();
        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Category obj)
        {
            //custom my errors
            //[1]
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Category.Name", "The display order cannot match name");
            }


            if (ModelState.IsValid)
            {
                _uw.Category.Add(obj);
                _uw.save();
                //TempData["Success"] = "Category Created Successfully";
                _tn.AddSuccessToastMessage("Category Created Successfully");
                return RedirectToAction("Index");
            }
            return View(obj);
        }



        // [2] Edit
        //get
        public IActionResult Edit(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();

            }
            var categoryFromDb = _uw.Category.GetFirstOrDefault(c => c.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Category obj)
        {
            if (obj.Name == obj.DisplayOrder.ToString())
            {
                ModelState.AddModelError("Category.Name", "The display order cannot match name");
            }
            if (ModelState.IsValid)
            {
                _uw.Category.Update(obj);
                _uw.save();
                //TempData["Success"] = "Category Updated Successfully";
                _tn.AddSuccessToastMessage("Category Updated Successfully");
                return RedirectToAction("Index");
            }
            return View(obj);
        }


        // [2] Delete
        //get
        public IActionResult Delete(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();

            }
            var categoryFromDb = _uw.Category.GetFirstOrDefault(c => c.Id == id);
            if (categoryFromDb == null)
            {
                return NotFound();
            }
            return View(categoryFromDb);
        }

        //post
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            if (id == null || id == 0)
            {
                return NotFound();

            }
            var obj = _uw.Category.GetFirstOrDefault(c => c.Id == id);
            if (obj == null)
            {
                return NotFound();
            }
            _uw.Category.Remove(obj);
            _uw.save();
            //TempData["Success"] = "Category Deleted Successfully";
            _tn.AddSuccessToastMessage("Category Deleted Successfully");

            return RedirectToAction("Index");
        }
    }
}
