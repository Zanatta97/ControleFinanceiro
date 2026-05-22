using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ControleFinanceiroAPI.Interfaces
{
    public interface IRepository<T> where T : class
    {
        public Task<IEnumerable<T>> GetAllAsync();
        public Task<T?> GetByIdAsync(Expression<Func<T, bool>> predicate);
        public Task<T?> GetByIdReadOnlyAsync(Expression<Func<T, bool>> predicate);
        public void Add(T entity);
        public void Update(T entity);
        public void Delete(T entity);
    }
}
