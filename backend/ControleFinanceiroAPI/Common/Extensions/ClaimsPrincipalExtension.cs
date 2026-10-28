using System.Security.Claims;

namespace ControleFinanceiroAPI.Common.Extensions
{
    /// <summary>
    /// Leitura dos claims próprios do JWT da aplicação ("id" e "ambiente_id") nos controllers.
    /// </summary>
    public static class ClaimsPrincipalExtension
    {
        /// <summary>
        /// Ambiente ativo do usuário, lido do claim "ambiente_id" do token.
        /// </summary>
        /// <remarks>
        /// É o filtro de isolamento de toda consulta de Conta, Categoria, Transacao, Orcamento, SaldoMensal
        /// e Relatorio. O valor vem só do token: a participação no ambiente foi conferida quando o token foi
        /// emitido (SelecionarAmbienteAsync) e não é verificada de novo aqui.
        /// </remarks>
        /// <returns>Id do ambiente selecionado.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Token sem o claim (nenhum ambiente selecionado ainda); o ErrorHandlingMiddleware devolve 403.
        /// </exception>
        public static Guid GetAmbienteAtivo(this ClaimsPrincipal user)
        {
            var claim = user.FindFirstValue("ambiente_id");
            if (claim is null)
                throw new UnauthorizedAccessException("Nenhum ambiente selecionado.");
            return Guid.Parse(claim);
        }

        /// <summary>
        /// Id do usuário autenticado, lido do claim "id" gravado no token (Login, SelecionarAmbiente, LoginDemo).
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Token sem o claim "id"; o middleware devolve 403.</exception>
        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("id")
                ?? throw new UnauthorizedAccessException("Usuário não autenticado.");
        }
    }
}
