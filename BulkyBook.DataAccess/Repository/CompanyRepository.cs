using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class CompanyRepository : Repository<Company> , ICompanyRepository //not sure here
    {
        private readonly ApplicationDbContext _db;
        public CompanyRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public Company GetById(int? id) //my try
        {
            var obj = _db.Companies.Find(id);
            return obj;
        }

        public void Update(Company obj)
        {
            var objFromDb = _db.Companies.FirstOrDefault(u => u.CompanyId == obj.CompanyId);
            if(objFromDb != null)
            {
                objFromDb.Name = obj.Name;
                objFromDb.StreetAddress = obj.StreetAddress;
                objFromDb.City = obj.City;
                objFromDb.State = obj.State;
                objFromDb.PostalCode = obj.PostalCode;
                objFromDb.PhoneNumber = obj.PhoneNumber;
                
                
            }
        }
    }
}
