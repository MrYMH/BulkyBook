using BulkyBook.DataAccess.Data;
using BulkyBook.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BulkyBook.DataAccess.Repository
{
    public class Repository<T> : IRepository<T> where T: class
    {
        private readonly ApplicationDbContext _db;
        private readonly DbSet<T> dbset; //do not sure
        public Repository(ApplicationDbContext db)
        {
             _db = db;
            //_db.Products.Include(u => u.Category).Include(u => u.TypeModel);
            this.dbset = _db.Set<T>();  //do not sure
        }
        public void Add(T entity)
        {
            dbset.Add(entity);
        }

        public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProps = null)
        {
            IQueryable<T> query = dbset.AsQueryable();
            if(filter != null)
            {
                query = query.Where(filter);
            }
            
            if (includeProps != null)
            {
                foreach (var includeProp in includeProps.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return query.ToList();
        }

        public T GetFirstOrDefault(Expression<Func<T, bool>> filter , string? includeProps = null , bool tracked = true)
        {
            IQueryable<T> query ;
            if (tracked)
            {
                query = dbset;
            }
            else
            {
                query = dbset.AsNoTracking();
            }
            query = query.Where(filter);
            if(includeProps != null)
            {
                foreach(var includeProp in includeProps.Split(new char[] {','} , StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProp);
                }
            }
            return query.FirstOrDefault();
        }

        public void Remove(T entity)
        {
            dbset.Remove(entity);
        }

        public void RemoveRange(IEnumerable<T> entity)
        {
            dbset.RemoveRange(entity);
        }
    }
}
