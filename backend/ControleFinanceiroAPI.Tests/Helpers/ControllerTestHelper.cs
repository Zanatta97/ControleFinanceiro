using ControleFinanceiroAPI.DTO.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Tests.Helpers
{
    /// <summary>
    /// Monta o usuário autenticado do controller (claims "id", "ambiente_id" e roles)
    /// e lê o ApiResponseDTO devolvido pela action.
    /// </summary>
    internal static class ControllerTestHelper
    {
        public const string UsuarioId = "usuario-1";

        public static T ComUsuario<T>(this T controller, Guid? ambienteId, string? usuarioId = UsuarioId,
                                      params string[] roles) where T : ControllerBase
        {
            var claims = new List<Claim>();

            if (usuarioId is not null)
                claims.Add(new Claim("id", usuarioId));

            if (ambienteId.HasValue)
                claims.Add(new Claim("ambiente_id", ambienteId.Value.ToString()));

            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"))
                }
            };

            return controller;
        }

        // Status HTTP da action e o corpo tipado; falha o teste se o corpo não for ApiResponseDTO<TDados>
        public static (int StatusCode, ApiResponseDTO<TDados> Corpo) LerResposta<TDados>(IActionResult resultado)
        {
            var objectResult = Assert.IsAssignableFrom<ObjectResult>(resultado);
            var corpo = Assert.IsType<ApiResponseDTO<TDados>>(objectResult.Value);

            return (objectResult.StatusCode ?? StatusCodes.Status200OK, corpo);
        }
    }
}
