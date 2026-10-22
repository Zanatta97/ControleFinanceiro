using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class UsuarioServiceTests
    {
        private const int ValidadeRefreshTokenEmMinutos = 60;
        private const string Email = "usuario@teste.com";
        private const string RefreshTokenNovo = "refresh-token-novo";

        private readonly UserManager<Usuario> _userManager = Substitute.For<UserManager<Usuario>>(
            Substitute.For<IUserStore<Usuario>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        private readonly RoleManager<IdentityRole> _roleManager = Substitute.For<RoleManager<IdentityRole>>(
            Substitute.For<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);
        private readonly ITokenService _tokenService = Substitute.For<ITokenService>();
        private readonly IConfiguration _configuration = Substitute.For<IConfiguration>();
        private readonly UsuarioService _service;

        public UsuarioServiceTests()
        {
            _configuration["JWT:RefreshTokenValidityInMinutes"].Returns(ValidadeRefreshTokenEmMinutos.ToString());

            _tokenService.GenerateAccessToken(Arg.Any<IEnumerable<Claim>>(), Arg.Any<IConfiguration>())
                .Returns(new JwtSecurityToken());
            _tokenService.GenerateRefreshToken().Returns(RefreshTokenNovo);

            _userManager.GetRolesAsync(Arg.Any<Usuario>()).Returns(new List<string>());
            _userManager.UpdateAsync(Arg.Any<Usuario>()).Returns(IdentityResult.Success);

            _service = new UsuarioService(
                Substitute.For<ILogger<UsuarioService>>(),
                _tokenService,
                _userManager,
                _roleManager,
                _configuration);
        }

        #region Login Tests
        [Fact]
        public async Task Login_ComCredenciaisValidas_PersisteRefreshTokenEValidade()
        {
            // Arrange — usuário que nunca teve refresh token salvo (validade no valor padrão 0001-01-01)
            var usuario = CriarUsuario(refreshToken: null, validadeRefreshToken: default);
            var login = new UsuarioLoginRequestDTO { Email = Email, Senha = "Senha@123" };

            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.CheckPasswordAsync(usuario, login.Senha).Returns(true);

            var antes = DateTime.UtcNow;

            // Act
            await _service.Login(login);

            var depois = DateTime.UtcNow;

            // Assert — o token e a validade chegam ao banco pelo UpdateAsync
            await _userManager.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u == usuario &&
                u.RefreshToken == RefreshTokenNovo &&
                u.RefreshTokenExpiryTime >= antes.AddMinutes(ValidadeRefreshTokenEmMinutos) &&
                u.RefreshTokenExpiryTime <= depois.AddMinutes(ValidadeRefreshTokenEmMinutos)));
        }
        #endregion

        #region RefreshToken Tests
        [Fact]
        public async Task RefreshToken_ComTokenValido_RetornaNovosTokens()
        {
            // Arrange
            var usuario = CriarUsuario("refresh-token-salvo", DateTime.MaxValue);
            ConfigurarTokenExpiradoDoUsuario(usuario);

            // Act
            var resposta = await _service.RefreshToken(CriarTokenDTO("refresh-token-salvo"));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, resposta.StatusCode);
            Assert.Equal(RefreshTokenNovo, LerPropriedade(resposta.Dados, "RefreshToken"));
            Assert.False(string.IsNullOrEmpty(LerPropriedade(resposta.Dados, "AccessToken")));
        }

        [Fact]
        public async Task RefreshToken_ComTokenValido_PersisteRefreshTokenRotacionado()
        {
            // Arrange
            var usuario = CriarUsuario("refresh-token-salvo", DateTime.MaxValue);
            ConfigurarTokenExpiradoDoUsuario(usuario);

            // Act
            await _service.RefreshToken(CriarTokenDTO("refresh-token-salvo"));

            // Assert — o token antigo é substituído e salvo
            await _userManager.Received(1).UpdateAsync(Arg.Is<Usuario>(u =>
                u == usuario && u.RefreshToken == RefreshTokenNovo));
        }

        [Fact]
        public async Task RefreshToken_ComTokenValido_EstendeValidadeDoRefreshToken()
        {
            // Arrange — validade atual termina em 5 minutos
            var antes = DateTime.UtcNow;
            var validadeAtual = antes.AddMinutes(5);
            var usuario = CriarUsuario("refresh-token-salvo", validadeAtual);
            ConfigurarTokenExpiradoDoUsuario(usuario);

            // Act
            await _service.RefreshToken(CriarTokenDTO("refresh-token-salvo"));

            var depois = DateTime.UtcNow;

            // Assert — nova validade é "agora + validade configurada", e não a antiga
            Assert.InRange(usuario.RefreshTokenExpiryTime,
                antes.AddMinutes(ValidadeRefreshTokenEmMinutos),
                depois.AddMinutes(ValidadeRefreshTokenEmMinutos));
            Assert.True(usuario.RefreshTokenExpiryTime > validadeAtual);
        }

        [Fact]
        public async Task RefreshToken_ComValidadeExpirada_RetornaErro400()
        {
            // Arrange
            var validadeExpirada = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var usuario = CriarUsuario("refresh-token-salvo", validadeExpirada);
            ConfigurarTokenExpiradoDoUsuario(usuario);

            // Act
            var resposta = await _service.RefreshToken(CriarTokenDTO("refresh-token-salvo"));

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
        }

        [Fact]
        public async Task RefreshToken_ComTokenDivergenteDoSalvo_RetornaErro400()
        {
            // Arrange
            var usuario = CriarUsuario("refresh-token-salvo", DateTime.MaxValue);
            ConfigurarTokenExpiradoDoUsuario(usuario);

            // Act
            var resposta = await _service.RefreshToken(CriarTokenDTO("refresh-token-de-outra-sessao"));

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
        }
        #endregion

        private static Usuario CriarUsuario(string? refreshToken, DateTime validadeRefreshToken)
        {
            return new Usuario
            {
                Id = "usuario-1",
                UserName = Email,
                Email = Email,
                RefreshToken = refreshToken,
                RefreshTokenExpiryTime = validadeRefreshToken
            };
        }

        // Access token expirado é aceito pelo TokenService e aponta para o usuário informado
        private void ConfigurarTokenExpiradoDoUsuario(Usuario usuario)
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, usuario.Email!) }, "Teste"));

            _tokenService.GetPrincipalFromExpiredToken("access-token-expirado", Arg.Any<IConfiguration>())
                .Returns(principal);
            _userManager.FindByEmailAsync(usuario.Email!).Returns(usuario);
        }

        private static TokenDTO CriarTokenDTO(string refreshToken)
        {
            return new TokenDTO { AccessToken = "access-token-expirado", RefreshToken = refreshToken };
        }

        // A resposta do serviço é um tipo anônimo; lê a propriedade por reflexão
        private static string? LerPropriedade(object? dados, string nome)
        {
            return dados?.GetType().GetProperty(nome)?.GetValue(dados) as string;
        }
    }
}
