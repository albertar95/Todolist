using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Todolist.Services.Contracts
{
    public interface IDbRepository
    {
        bool Add<T>(T entity) where T : class;
        bool Update<T>(T entity) where T : class;
        bool Delete<T>(T entity) where T : class;
        List<T> GetList<T>(int pageSize = 1000, string Include = "") where T : class;
        List<T> GetList<T>(Expression<Func<T, bool>> predicate, int pageSize = 1000, string Include = "") where T : class;
        T Get<T>(Expression<Func<T, bool>> predicate, string Include = "") where T : class;
        T GetMax<T>(Expression<Func<T, bool>> predicate, string Include = "") where T : class;
        T GetMax<T,TKEY>(Expression<Func<T, TKEY>> predicate, Expression<Func<T, bool>> condition, string Include = "") where T : class;
        bool AddBatch<T>(List<T> entities, int batchSize = 1000) where T : class;
        bool DeleteBatch<T>(List<T> entities, int batchSize = 1000) where T : class;
        bool UpdateBatch<T>(List<T> entities, int batchSize = 1000) where T : class;
        bool Any<T>(Expression<Func<T, bool>> predicate) where T : class;
    }
}
