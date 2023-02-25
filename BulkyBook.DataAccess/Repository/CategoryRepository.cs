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
    public class CategoryRepository : Repository<Category> , ICategoryRepository //not sure here
    {
        private readonly ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public Category GetById(int? id) //my try
        {
           var obj =  _db.Categories.Find(id);
            return obj;
        }

        public void Update(Category obj)
        {
            _db.Categories.Update(obj);
        }
    }
}
