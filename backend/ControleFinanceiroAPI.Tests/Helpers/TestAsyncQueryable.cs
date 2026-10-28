using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;

namespace ControleFinanceiroAPI.Tests.Helpers
{
    /// <summary>
    /// IQueryable em memória que aceita os métodos assíncronos do EF Core (ToListAsync etc.).
    /// Necessário para substituir UserManager.Users sem subir banco.
    /// </summary>
    internal class TestAsyncQueryable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncQueryable(IEnumerable<T> itens) : base(itens) { }

        public TestAsyncQueryable(Expression expressao) : base(expressao) { }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());
    }

    internal class TestAsyncQueryProvider<TEntidade> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _interno;

        public TestAsyncQueryProvider(IQueryProvider interno)
        {
            _interno = interno;
        }

        public IQueryable CreateQuery(Expression expression) => new TestAsyncQueryable<TEntidade>(expression);

        public IQueryable<TElemento> CreateQuery<TElemento>(Expression expression) => new TestAsyncQueryable<TElemento>(expression);

        public object? Execute(Expression expression) => _interno.Execute(expression);

        public TResultado Execute<TResultado>(Expression expression) => _interno.Execute<TResultado>(expression);

        public TResultado ExecuteAsync<TResultado>(Expression expression, CancellationToken cancellationToken = default)
        {
            // TResultado é Task<X>: executa de forma síncrona e embrulha o resultado em Task.FromResult
            var tipoResultado = typeof(TResultado).GetGenericArguments()[0];
            var resultado = typeof(IQueryProvider)
                .GetMethods()
                .Single(m => m.Name == nameof(IQueryProvider.Execute) && m.IsGenericMethod)
                .MakeGenericMethod(tipoResultado)
                .Invoke(this, [expression]);

            return (TResultado)typeof(Task)
                .GetMethod(nameof(Task.FromResult))!
                .MakeGenericMethod(tipoResultado)
                .Invoke(null, [resultado])!;
        }
    }

    internal class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _interno;

        public TestAsyncEnumerator(IEnumerator<T> interno)
        {
            _interno = interno;
        }

        public T Current => _interno.Current;

        public ValueTask<bool> MoveNextAsync() => new(_interno.MoveNext());

        public ValueTask DisposeAsync()
        {
            _interno.Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
