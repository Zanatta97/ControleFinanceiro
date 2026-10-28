using ControleFinanceiroAPI.Common.Extensions;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Tests.Common
{
    public class ClaimsPrincipalExtensionTests
    {
        private static ClaimsPrincipal CriarUsuario(params Claim[] claims)
        {
            return new ClaimsPrincipal(new ClaimsIdentity(claims, "Teste"));
        }

        #region GetAmbienteAtivo Tests
        [Fact]
        public void GetAmbienteAtivo_ComClaimDeAmbiente_RetornaGuid()
        {
            // Arrange
            var ambienteId = Guid.NewGuid();
            var usuario = CriarUsuario(new Claim("ambiente_id", ambienteId.ToString()));

            // Act
            var resultado = usuario.GetAmbienteAtivo();

            // Assert
            Assert.Equal(ambienteId, resultado);
        }

        [Fact]
        public void GetAmbienteAtivo_SemClaimDeAmbiente_LancaUnauthorizedAccessException()
        {
            // Arrange — usuário logado que ainda não selecionou ambiente
            var usuario = CriarUsuario(new Claim("id", "usuario-1"));

            // Act
            var excecao = Assert.Throws<UnauthorizedAccessException>(() => usuario.GetAmbienteAtivo());

            // Assert
            Assert.Equal("Nenhum ambiente selecionado.", excecao.Message);
        }

        [Fact]
        public void GetAmbienteAtivo_ComClaimQueNaoEGuid_LancaFormatException()
        {
            // Arrange
            var usuario = CriarUsuario(new Claim("ambiente_id", "nao-e-guid"));

            // Act / Assert
            Assert.Throws<FormatException>(() => usuario.GetAmbienteAtivo());
        }
        #endregion

        #region GetUserId Tests
        [Fact]
        public void GetUserId_ComClaimId_RetornaId()
        {
            // Arrange
            var usuario = CriarUsuario(new Claim("id", "usuario-1"));

            // Act
            var resultado = usuario.GetUserId();

            // Assert
            Assert.Equal("usuario-1", resultado);
        }

        [Fact]
        public void GetUserId_SemClaimId_LancaUnauthorizedAccessException()
        {
            // Arrange
            var usuario = CriarUsuario();

            // Act
            var excecao = Assert.Throws<UnauthorizedAccessException>(() => usuario.GetUserId());

            // Assert
            Assert.Equal("Usuário não autenticado.", excecao.Message);
        }
        #endregion
    }
}
