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
    public class CoverTypeController : Controller

    {
        //add obj from dbcontext
        private readonly IUnitOfWork _uw;
        private readonly IToastNotification _tn;

        public CoverTypeController(IUnitOfWork uw , IToastNotification tn )
        {
            _uw = uw;
            _tn = tn;
        }


        public IActionResult Index()
        {
            //get all data from categories table
            IEnumerable<TypeModel> objCoverList = _uw.TypeModel.GetAll();
            return View(objCoverList);
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
        public IActionResult Create(TypeModel obj)
        {
            if (ModelState.IsValid)
            {
                _uw.TypeModel.Add(obj);
                _uw.save();
                _tn.AddSuccessToastMessage("cover type Created Successfully");
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
            var coverTypeFromDb = _uw.TypeModel.GetFirstOrDefault(c => c.Id == id);
            if (coverTypeFromDb == null)
            {
                return NotFound();
            }
            return View(coverTypeFromDb);
        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TypeModel obj)
        {
         

            if (ModelState.IsValid)
            {
                _uw.TypeModel.Update(obj);
                _uw.save();
                _tn.AddSuccessToastMessage("cover type Updated Successfully");
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
            var coverTypeFromDb = _uw.TypeModel.GetFirstOrDefault(c => c.Id == id);
            if (coverTypeFromDb == null)
            {
                return NotFound();
            }
            return View(coverTypeFromDb);
        }

        //post
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeletePost(int? id)
        {
            var obj = _uw.TypeModel.GetFirstOrDefault(c => c.Id == id);

            if (id == null || id == 0)
            {
                return NotFound();

            }
            _uw.TypeModel.Remove(obj);
            _uw.save();
            _tn.AddSuccessToastMessage("Type Model Deleted Successfully");
            return RedirectToAction("Index");
        }
    }
}
