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
    public class TypeModelRepository :Repository<TypeModel> , ITypeModelRepository //not sure here
    {
        private readonly ApplicationDbContext _db;
        public TypeModelRepository (ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public TypeModel GetById(int? id) //my try
        {
            var obj = _db.TypeModels.Find(id);
            return obj;
        }

        public void Update(TypeModel obj)
        {
            _db.TypeModels.Update(obj);
        }
    }
}
