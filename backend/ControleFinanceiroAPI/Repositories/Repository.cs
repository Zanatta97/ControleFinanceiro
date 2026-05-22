using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ControleFinanceiroAPI.Repositories
{
    public class Repository<T> : IRepository<T> where T : class  
    {
        protected readonly AppDbContext _context;

        public Repository(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retorna todos os registros da entidade T do banco de dados de forma assíncrona.
        /// Sem Tracking para melhorar o desempenho em consultas somente leitura.
        /// </summary>
        /// <returns>IEnumerable<typeparamref name="T"/></returns>
        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _context.Set<T>().AsNoTracking().ToListAsync();
        }

        /// <summary>
        /// Retorna um registro da entidade T do banco de dados que corresponde ao predicado fornecido de forma assíncrona.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual async Task<T?> GetByIdAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().FirstOrDefaultAsync(predicate);
        }

        /// <summary>
        /// Retorna um registro da entidade T do banco de dados que corresponde ao predicado fornecido de forma assíncrona, sem Tracking.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public virtual async Task<T?> GetByIdReadOnlyAsync(Expression<Func<T, bool>> predicate)
        {
            return await _context.Set<T>().AsNoTracking().FirstOrDefaultAsync(predicate);
        }

        public virtual void Add(T entity)
        {
            _context.Set<T>().Add(entity);
        }

        public virtual void Update(T entity)
        {
            _context.Set<T>().Update(entity);
        }

        public virtual void Delete(T entity)
        {
            _context.Set<T>().Remove(entity);
        }
    }
}
