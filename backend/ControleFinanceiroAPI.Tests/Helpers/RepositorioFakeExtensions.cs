using ControleFinanceiroAPI.Interfaces.Repositories;
using NSubstitute;
using System.Linq.Expressions;

namespace ControleFinanceiroAPI.Tests.Helpers
{
    /// <summary>
    /// Faz o repositório substituto responder GetByIdAsync/GetByIdReadOnlyAsync avaliando
    /// o predicado de verdade sobre uma lista em memória. Assim o teste confere o filtro
    /// que o serviço montou (id + ambiente), e não só que o método foi chamado.
    /// </summary>
    internal static class RepositorioFakeExtensions
    {
        public static void ComDados<T>(this IRepository<T> repositorio, params T[] itens) where T : class
        {
            repositorio.GetByIdAsync(Arg.Any<Expression<Func<T, bool>>>())
                .Returns(ci => Task.FromResult<T?>(itens.AsQueryable().FirstOrDefault(ci.Arg<Expression<Func<T, bool>>>())));

            repositorio.GetByIdReadOnlyAsync(Arg.Any<Expression<Func<T, bool>>>())
                .Returns(ci => Task.FromResult<T?>(itens.AsQueryable().FirstOrDefault(ci.Arg<Expression<Func<T, bool>>>())));
        }
    }
}
