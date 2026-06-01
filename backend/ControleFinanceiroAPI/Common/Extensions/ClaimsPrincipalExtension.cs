using System.Security.Claims;

namespace ControleFinanceiroAPI.Common.Extensions
{
    public static class ClaimsPrincipalExtension
    {
        public static Guid GetAmbienteAtivo(this ClaimsPrincipal user)
        {
            var claim = user.FindFirstValue("ambiente_id");
            if (claim is null)
                throw new UnauthorizedAccessException("Nenhum ambiente selecionado.");
            return Guid.Parse(claim);
        }

        public static string GetUserId(this ClaimsPrincipal user)
        {
            return user.FindFirstValue("id")
                ?? throw new UnauthorizedAccessException("Usuário não autenticado.");
        }
    }
}
