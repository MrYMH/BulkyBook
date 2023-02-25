using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using BulkyBook.Models.ViewModels;
using BulkyBook.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using NToastNotify;

namespace BulkyBookWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.Role_Admin)]
    public class CompanyController : Controller

    {
        //add obj from dbcontext
        private readonly IUnitOfWork _uw;
        private readonly IToastNotification _tn;

        public CompanyController(IUnitOfWork uw , IWebHostEnvironment he , IToastNotification tn)
        {
            _uw = uw;
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
            Company company = new();

            if (id == null || id == 0)
            {
                return View(company);

            }
            else
            {
                company = _uw.Company.GetFirstOrDefault(u => u.CompanyId == id);
                return View(company);
            }


        }

        //post
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Company obj )
        {
            if (ModelState.IsValid)
            {
                if(obj.CompanyId == 0)
                {
                    _uw.Company.Add(obj);
                    _uw.save();
                    _tn.AddSuccessToastMessage("Company Created Successfully");

                }
                else
                {
                    _uw.Company.Update(obj);
                    _uw.save();
                    //TempData["Success"] = "Company Updated Successfully";
                    _tn.AddSuccessToastMessage("Company Updated Successfully");

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
            var company = _uw.Company.GetAll();
            return Json(new
            {
                data = company
            });
        }



        [HttpDelete]
        public IActionResult Delete(int? id)
        {
            var obj = _uw.Company.GetFirstOrDefault(u => u.CompanyId == id);
            if (obj == null)
            {
                return Json(new { success = false, message = "Error while deleting" });
            }

            _uw.Company.Remove(obj);
            _uw.save();
            return Json(new { success = true, message = "Delete Successful" });

        }




        #endregion
    }
}
