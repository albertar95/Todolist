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
        public T Get<T>(Expression<Func<T, bool>> predicate, string Include = "") where T : class
        {
            if (!string.IsNullOrEmpty(Include))
                return _context.Set<T>().Include(Include).FirstOrDefault(predicate);
            else
                return _context.Set<T>().FirstOrDefault(predicate);
        }
        public List<T> GetList<T>(int pageSize = 1000, string Include = "") where T : class
        {
            if (!string.IsNullOrEmpty(Include))
                return _context.Set<T>().Include(Include).Take(pageSize).ToList();
            else
                return _context.Set<T>().Take(pageSize).ToList();
        }
        public List<T> GetList<T>(Expression<Func<T, bool>> predicate, int pageSize = 1000,string Include = "") where T : class
        {
            if(!string.IsNullOrEmpty(Include))
                return _context.Set<T>().Include(Include).Where(predicate).Take(pageSize).ToList();
            else
                return _context.Set<T>().Where(predicate).Take(pageSize).ToList();
        }
        public T GetMax<T>(Expression<Func<T, bool>> predicate, string Include = "") where T : class
        {
            if (!string.IsNullOrEmpty(Include))
                return _context.Set<T>().Include(Include).OrderByDescending(predicate).FirstOrDefault();
            else
                return _context.Set<T>().OrderByDescending(predicate).FirstOrDefault();
        }

        public T GetMax<T,TKEY>(Expression<Func<T, TKEY>> predicate, Expression<Func<T, bool>> condition, string Include = "") where T : class
        {
            if (!string.IsNullOrEmpty(Include))
            {
                if (condition != null)
                    return _context.Set<T>().Include(Include).Where(condition).OrderByDescending(predicate).FirstOrDefault();
                else
                    return _context.Set<T>().Include(Include).OrderByDescending(predicate).FirstOrDefault();
            }
            else
            {
                if (condition != null)
                    return _context.Set<T>().Where(condition).OrderByDescending(predicate).FirstOrDefault();
                else
                    return _context.Set<T>().OrderByDescending(predicate).FirstOrDefault();
            }
        }
        public bool AddBatch<T>(List<T> entities,int batchSize = 1000) where T : class
        {
            try
            {
                bool result = true;
                int to = 0;
                if(entities.Count >= batchSize)
                {
                    for (int i = 0; i <= entities.Count / 1000; i++)
                    {
                        if ((i + 1) * 1000 > entities.Count)
                            to = entities.Count;
                        else
                            to = (i + 1) * 1000;
                        for (int j = i * 1000; j < to; j++)
                        {
                            _context.Entry(entities[j]).State = System.Data.EntityState.Added;
                        }
                        if (!(_context.SaveChanges() >= to))
                            result = false;
                    }
                }
                else
                {
                    for (int j = 0; j < entities.Count; j++)
                    {
                        _context.Entry(entities[j]).State = System.Data.EntityState.Added;
                    }
                    if (!(_context.SaveChanges() >= entities.Count))
                        result = false;
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool DeleteBatch<T>(List<T> entities, int batchSize = 1000) where T : class
        {
            try
            {
                bool result = true;
                int to = 0;
                if (entities.Count >= batchSize)
                {
                    for (int i = 0; i <= entities.Count / 1000; i++)
                    {
                        if ((i + 1) * 1000 > entities.Count)
                            to = entities.Count;
                        else
                            to = (i + 1) * 1000;
                        for (int j = i * 1000; j < to; j++)
                        {
                            _context.Entry(entities[j]).State = System.Data.EntityState.Deleted;
                        }
                        if (!(_context.SaveChanges() >= to))
                            result = false;
                    }
                }
                else
                {
                    for (int j = 0; j < entities.Count; j++)
                    {
                        _context.Entry(entities[j]).State = System.Data.EntityState.Deleted;
                    }
                    if (!(_context.SaveChanges() >= entities.Count))
                        result = false;
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool UpdateBatch<T>(List<T> entities, int batchSize = 1000) where T : class
        {
            try
            {
                bool result = true;
                int to = 0;
                if (entities.Count >= batchSize)
                {
                    for (int i = 0; i <= entities.Count / 1000; i++)
                    {
                        if ((i + 1) * 1000 > entities.Count)
                            to = entities.Count;
                        else
                            to = (i + 1) * 1000;
                        for (int j = i * 1000; j < to; j++)
                        {
                            _context.Entry(entities[j]).State = System.Data.EntityState.Modified;
                        }
                        if (!(_context.SaveChanges() >= to))
                            result = false;
                    }
                }
                else
                {
                    for (int j = 0; j < entities.Count; j++)
                    {
                        _context.Entry(entities[j]).State = System.Data.EntityState.Modified;
                    }
                    if (!(_context.SaveChanges() >= entities.Count))
                        result = false;
                }
                return result;
            }
            catch (Exception)
            {
                return false;
            }
        }
        public bool Any<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            return _context.Set<T>().Any(predicate);
        }
    }
}