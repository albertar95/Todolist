using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Web;
using Todolist.Models;
using Todolist.Services.Contracts;

namespace Todolist.Services
{
    public class DbRepository : IDbRepository
    {
        private readonly ToDoListDbEntities _context;
        public DbRepository(ToDoListDbEntities context)
        {
            _context = context;
        }

        public bool Add<T>(T entity) where T : class
        {
            try
            {
            _context.Entry(entity).State = System.Data.EntityState.Added;
            if (_context.SaveChanges() >= 1)
                return true;
            else
                return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool Update<T>(T entity) where T : class
        {
            try
            {
                _context.Entry(entity).State = System.Data.EntityState.Modified;
                if (_context.SaveChanges() >= 1)
                    return true;
                else
                    return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
        public bool Delete<T>(T entity) where T : class
        {
            try
            {
                _context.Entry(entity).State = System.Data.EntityState.Deleted;
                if (_context.SaveChanges() >= 1)
                    return true;
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public T Get<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return _context.Set<T>().FirstOrDefault(predicate);
        }
        public List<T> GetList<T>(int pageSize = 1000) where T : class
        {
            return _context.Set<T>().Take(pageSize).ToList();
        }
        public List<T> GetList<T>(Expression<Func<T, bool>> predicate, int pageSize = 1000) where T : class
        {
            return _context.Set<T>().Where(predicate).Take(pageSize).ToList();
        }
        public T GetMax<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return _context.Set<T>().OrderByDescending(predicate).FirstOrDefault();
        }

        public T GetMax<T,TKEY>(Expression<Func<T, TKEY>> predicate, Expression<Func<T, bool>> condition) where T : class
        {
            return _context.Set<T>().Where(condition).OrderByDescending(predicate).FirstOrDefault();
        }
    }
}