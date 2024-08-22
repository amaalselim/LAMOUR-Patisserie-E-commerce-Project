using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace MyShop.Entities.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        // _context.Categories.Include("").Tolist();
        // _context.Categories.where(x=>x.id==id).Tolist();
        IEnumerable<T> GetAll(Expression<Func<T,bool>>? perdicate=null,string? IncludeWord=null);

        // _context.Categories.Include("").SingleOrDefault();
        // _context.Categories.where(x=>x.id==id).SingleOrDefault();

        T GetFirstOrDefault(Expression<Func<T, bool>>? perdicate=null, string? IncludeWord=null);

        //_context.Categories.Add(category);
        void Add(T entity);

        //_context.Categories.Remove(category);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entities);
    }
}
