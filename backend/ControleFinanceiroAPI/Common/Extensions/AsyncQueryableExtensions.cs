using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq.Expressions;

namespace ControleFinanceiroAPI.Common.Extensions
{
    /// <summary>
    /// Converte coleções ou expressões em fontes assíncronas do Entity Framework 6 (System.Data.Entity),
    /// via <see cref="DbAsyncEnumerable{T}"/>. Não há chamada a estas extensões no código atual.
    /// </summary>
    public static class AsyncQueryableExtensions
    {
        public static IQueryable<TElement> AsAsyncQueryable<TElement>(this IEnumerable<TElement> source)
        {
            return new DbAsyncEnumerable<TElement>(source);
        }

        public static IDbAsyncEnumerable<TElement> AsDbAsyncEnumerable<TElement>(this IEnumerable<TElement> source)
        {
            return new DbAsyncEnumerable<TElement>(source);
        }

        public static EnumerableQuery<TElement> AsAsyncEnumerableQuery<TElement>(this IEnumerable<TElement> source)
        {
            return new DbAsyncEnumerable<TElement>(source);
        }

        public static IQueryable<TElement> AsAsyncQueryable<TElement>(this Expression expression)
        {
            return new DbAsyncEnumerable<TElement>(expression);
        }

        public static IDbAsyncEnumerable<TElement> AsDbAsyncEnumerable<TElement>(this Expression expression)
        {
            return new DbAsyncEnumerable<TElement>(expression);
        }

        public static EnumerableQuery<TElement> AsAsyncEnumerableQuery<TElement>(this Expression expression)
        {
            return new DbAsyncEnumerable<TElement>(expression);
        }
    }
}
