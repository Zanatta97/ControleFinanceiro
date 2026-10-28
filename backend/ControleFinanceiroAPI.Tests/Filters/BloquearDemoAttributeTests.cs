using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.Filters;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Tests.Filters
{
    public class BloquearDemoAttributeTests
    {
        private static ActionExecutingContext CriarContexto(params string[] roles)
        {
            var claims = roles.Select(r => new Claim(ClaimTypes.Role, r));
            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(claims, roles.Length > 0 ? "Teste" : null))
            };

            return new ActionExecutingContext(
                new ActionContext(httpContext, new RouteData(), new ActionDescriptor()),
                new List<IFilterMetadata>(),
                new Dictionary<string, object?>(),
                controller: new object());
        }

        #region OnActionExecuting Tests
        [Fact]
        public void OnActionExecuting_ComUsuarioDemo_Bloqueia403ComApiResponse()
        {
            // Arrange
            var contexto = CriarContexto("Demo");

            // Act
            new BloquearDemoAttribute().OnActionExecuting(contexto);

            // Assert
            var resultado = Assert.IsType<ObjectResult>(contexto.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, resultado.StatusCode);

            var corpo = Assert.IsType<ApiResponseDTO<object>>(resultado.Value);
            Assert.False(corpo.Success);
            Assert.Equal(StatusCodes.Status403Forbidden, corpo.StatusCode);
            Assert.Equal("Ação indisponível no modo demonstração.", corpo.ErrorMessage);
        }

        [Theory]
        [InlineData("User")]
        [InlineData("Admin")]
        public void OnActionExecuting_ComUsuarioNaoDemo_DeixaSeguir(string role)
        {
            // Arrange
            var contexto = CriarContexto(role);

            // Act
            new BloquearDemoAttribute().OnActionExecuting(contexto);

            // Assert
            Assert.Null(contexto.Result);
        }

        [Fact]
        public void OnActionExecuting_SemUsuarioAutenticado_DeixaSeguir()
        {
            // Arrange
            var contexto = CriarContexto();

            // Act
            new BloquearDemoAttribute().OnActionExecuting(contexto);

            // Assert
            Assert.Null(contexto.Result);
        }

        [Fact]
        public void OnActionExecuting_ComDemoEOutraRole_Bloqueia()
        {
            // Arrange
            var contexto = CriarContexto("User", "Demo");

            // Act
            new BloquearDemoAttribute().OnActionExecuting(contexto);

            // Assert
            var resultado = Assert.IsType<ObjectResult>(contexto.Result);
            Assert.Equal(StatusCodes.Status403Forbidden, resultado.StatusCode);
        }
        #endregion
    }
}
