using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>
    /// Operações genéricas sobre uma entidade. Add, Update e Delete só marcam a alteração no DbContext;
    /// nada vai ao banco até IUnityOfWork.SaveChangesAsync.
    /// </summary>
    /// <typeparam name="T">Entidade mapeada no AppDbContext.</typeparam>
    public interface IRepository<T> where T : class
    {
        /// <summary>Todos os registros da entidade, sem filtro de ambiente e sem rastreamento.</summary>
        public Task<IEnumerable<T>> GetAllAsync();

        /// <summary>
        /// Primeiro registro que atende ao predicado, rastreado pelo EF (use para alterar ou excluir).
        /// O nome diz "ById", mas qualquer critério serve — os services passam id e ambiente juntos.
        /// </summary>
        /// <returns>O registro, ou null se nenhum atender.</returns>
        public Task<T?> GetByIdAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Igual a <see cref="GetByIdAsync"/>, sem rastreamento (só leitura).</summary>
        public Task<T?> GetByIdReadOnlyAsync(Expression<Func<T, bool>> predicate);

        /// <summary>Marca a entidade para inclusão.</summary>
        public void Add(T entity);

        /// <summary>Marca a entidade inteira como alterada.</summary>
        public void Update(T entity);

        /// <summary>Marca a entidade para exclusão.</summary>
        public void Delete(T entity);
    }
}
