using BulkyBook.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface ITypeModelRepository :IRepository<TypeModel>
    {
        void Update(TypeModel obj);
        TypeModel GetById(int? id);
    }
}
