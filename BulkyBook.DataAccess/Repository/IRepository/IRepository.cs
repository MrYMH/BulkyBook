using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProps = null);
        void Add(T entity);
        //T GetById(int id);
        T GetFirstOrDefault(Expression<Func<T, bool>> filter , string? includeProps = null , bool tracked = true);
        void Remove (T entity);
        void RemoveRange(IEnumerable<T> entity);
    }
}
