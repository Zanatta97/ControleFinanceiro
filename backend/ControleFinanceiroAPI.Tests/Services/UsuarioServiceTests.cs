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

        #region CreateRole Tests
        [Fact]
        public async Task CreateRole_RoleNova_CriaERetorna200()
        {
            // Arrange
            _roleManager.RoleExistsAsync("Gestor").Returns(false);
            _roleManager.CreateAsync(Arg.Any<IdentityRole>()).Returns(IdentityResult.Success);

            // Act
            var resposta = await _service.CreateRole("Gestor");

            // Assert
            Assert.True(resposta.Success);
            Assert.Equal(StatusCodes.Status200OK, resposta.StatusCode);
            await _roleManager.Received(1).CreateAsync(Arg.Is<IdentityRole>(r => r.Name == "Gestor"));
        }

        [Fact]
        public async Task CreateRole_RoleJaExistente_RetornaErro400()
        {
            // Arrange
            _roleManager.RoleExistsAsync("Admin").Returns(true);

            // Act
            var resposta = await _service.CreateRole("Admin");

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Role 'Admin' já existe.", resposta.ErrorMessage);
            await _roleManager.DidNotReceive().CreateAsync(Arg.Any<IdentityRole>());
        }

        [Fact]
        public async Task CreateRole_FalhaNoIdentity_RetornaErro500ComDescricoes()
        {
            // Arrange
            _roleManager.RoleExistsAsync("Gestor").Returns(false);
            _roleManager.CreateAsync(Arg.Any<IdentityRole>()).Returns(IdentityResult.Failed(
                new IdentityError { Description = "Nome inválido" },
                new IdentityError { Description = "Outro erro" }));

            // Act
            var resposta = await _service.CreateRole("Gestor");

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, resposta.StatusCode);
            Assert.Equal("Erro ao criar a role 'Gestor': Nome inválido, Outro erro", resposta.ErrorMessage);
        }
        #endregion

        #region AssignUserToRole Tests
        [Fact]
        public async Task AssignUserToRole_UsuarioExistente_AtribuiERetorna200()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.AddToRoleAsync(usuario, "Admin").Returns(IdentityResult.Success);

            // Act
            var resposta = await _service.AssignUserToRole(Email, "Admin");

            // Assert
            Assert.True(resposta.Success);
            Assert.Equal(StatusCodes.Status200OK, resposta.StatusCode);
            await _userManager.Received(1).AddToRoleAsync(usuario, "Admin");
        }

        [Fact]
        public async Task AssignUserToRole_UsuarioInexistente_RetornaErro400()
        {
            // Arrange
            _userManager.FindByEmailAsync("ninguem@teste.com").Returns((Usuario?)null);

            // Act
            var resposta = await _service.AssignUserToRole("ninguem@teste.com", "Admin");

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Usuário com email 'ninguem@teste.com' não encontrado.", resposta.ErrorMessage);
        }

        [Fact]
        public async Task AssignUserToRole_FalhaNoIdentity_RetornaErro500ComDescricoes()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.AddToRoleAsync(usuario, "Inexistente").Returns(IdentityResult.Failed(
                new IdentityError { Description = "Role não existe" }));

            // Act
            var resposta = await _service.AssignUserToRole(Email, "Inexistente");

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, resposta.StatusCode);
            Assert.Equal($"Erro ao atribuir a role 'Inexistente' ao usuário '{Email}': Role não existe", resposta.ErrorMessage);
        }
        #endregion

        #region Login Tests
        [Fact]
        public async Task Login_ComCredenciaisValidas_Retorna200ComTokens()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.CheckPasswordAsync(usuario, "Senha@123").Returns(true);

            // Act
            var resposta = await _service.Login(new UsuarioLoginRequestDTO { Email = Email, Senha = "Senha@123" });

            // Assert
            Assert.True(resposta.Success);
            Assert.Equal(StatusCodes.Status200OK, resposta.StatusCode);
            Assert.Equal(RefreshTokenNovo, LerPropriedade(resposta.Dados, "RefreshToken"));
            Assert.False(string.IsNullOrEmpty(LerPropriedade(resposta.Dados, "Token")));
        }

        [Fact]
        public async Task Login_ComAmbienteAtivoERoles_IncluiClaimsNoToken()
        {
            // Arrange — usuário que já tinha um ambiente selecionado volta direto para ele
            var ambienteAtivo = Guid.NewGuid();
            var usuario = CriarUsuario(null, default);
            usuario.AmbienteAtivoId = ambienteAtivo;
            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.CheckPasswordAsync(usuario, "Senha@123").Returns(true);
            _userManager.GetRolesAsync(usuario).Returns(new List<string> { "Admin", "User" });

            // Act
            await _service.Login(new UsuarioLoginRequestDTO { Email = Email, Senha = "Senha@123" });

            // Assert
            _tokenService.Received(1).GenerateAccessToken(Arg.Is<IEnumerable<Claim>>(claims =>
                claims.Any(c => c.Type == ClaimTypes.Name && c.Value == Email) &&
                claims.Any(c => c.Type == ClaimTypes.Email && c.Value == Email) &&
                claims.Any(c => c.Type == "id" && c.Value == "usuario-1") &&
                claims.Any(c => c.Type == "ambiente_id" && c.Value == ambienteAtivo.ToString()) &&
                claims.Count(c => c.Type == ClaimTypes.Role) == 2), _configuration);
        }

        [Fact]
        public async Task Login_SemAmbienteAtivo_NaoIncluiClaimDeAmbiente()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.CheckPasswordAsync(usuario, "Senha@123").Returns(true);

            // Act
            await _service.Login(new UsuarioLoginRequestDTO { Email = Email, Senha = "Senha@123" });

            // Assert
            _tokenService.Received(1).GenerateAccessToken(Arg.Is<IEnumerable<Claim>>(claims =>
                !claims.Any(c => c.Type == "ambiente_id")), _configuration);
        }

        [Fact]
        public async Task Login_ComUsuarioInexistente_RetornaErro401()
        {
            // Arrange
            _userManager.FindByEmailAsync(Email).Returns((Usuario?)null);

            // Act
            var resposta = await _service.Login(new UsuarioLoginRequestDTO { Email = Email, Senha = "Senha@123" });

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status401Unauthorized, resposta.StatusCode);
            Assert.Equal("Erro ao fazer login", resposta.ErrorMessage);
        }

        [Fact]
        public async Task Login_ComSenhaErrada_RetornaErro401SemGerarToken()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByEmailAsync(Email).Returns(usuario);
            _userManager.CheckPasswordAsync(usuario, "errada").Returns(false);

            // Act
            var resposta = await _service.Login(new UsuarioLoginRequestDTO { Email = Email, Senha = "errada" });

            // Assert
            Assert.Equal(StatusCodes.Status401Unauthorized, resposta.StatusCode);
            _tokenService.DidNotReceive().GenerateRefreshToken();
            await _userManager.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }

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

        [Fact]
        public async Task RefreshToken_ComDTONulo_RetornaErro400()
        {
            // Act
            var resposta = await _service.RefreshToken(null!);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Token inválido", resposta.ErrorMessage);
        }

        [Fact]
        public async Task RefreshToken_SemAccessToken_LancaArgumentNullException()
        {
            // Act / Assert — sobe para o middleware, que devolve 400
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.RefreshToken(new TokenDTO { AccessToken = null, RefreshToken = "x" }));
        }

        [Fact]
        public async Task RefreshToken_SemRefreshToken_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                _service.RefreshToken(new TokenDTO { AccessToken = "x", RefreshToken = null }));
        }

        [Fact]
        public async Task RefreshToken_ComPrincipalNulo_RetornaErro400()
        {
            // Arrange
            _tokenService.GetPrincipalFromExpiredToken("access-token-expirado", Arg.Any<IConfiguration>())
                .Returns((ClaimsPrincipal)null!);

            // Act
            var resposta = await _service.RefreshToken(CriarTokenDTO("refresh"));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Token de acesso inválido", resposta.ErrorMessage);
        }

        [Fact]
        public async Task RefreshToken_ComUsuarioInexistente_RetornaErro400()
        {
            // Arrange
            var principal = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "sumiu@teste.com") }, "Teste"));
            _tokenService.GetPrincipalFromExpiredToken("access-token-expirado", Arg.Any<IConfiguration>()).Returns(principal);
            _userManager.FindByEmailAsync("sumiu@teste.com").Returns((Usuario?)null);

            // Act
            var resposta = await _service.RefreshToken(CriarTokenDTO("refresh"));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Token de atualização inválido", resposta.ErrorMessage);
        }
        #endregion

        #region Register Tests
        private static UsuarioRegisterDTO CriarRegistro()
        {
            return new UsuarioRegisterDTO { Nome = "Fulano", Email = "novo@teste.com", Senha = "Senha@123", ConfirmacaoSenha = "Senha@123" };
        }

        [Fact]
        public async Task Register_ComEmailNovo_CriaUsuarioComRoleUser()
        {
            // Arrange
            _userManager.FindByEmailAsync("novo@teste.com").Returns((Usuario?)null);
            _userManager.CreateAsync(Arg.Any<Usuario>(), "Senha@123").Returns(IdentityResult.Success);
            _roleManager.RoleExistsAsync("User").Returns(true);

            // Act
            var resposta = await _service.Register(CriarRegistro());

            // Assert
            Assert.True(resposta.Success);
            Assert.Equal(StatusCodes.Status200OK, resposta.StatusCode);
            Assert.Equal("Usuário criado com sucesso", resposta.Dados);
            await _userManager.Received(1).CreateAsync(Arg.Is<Usuario>(u =>
                u.Email == "novo@teste.com" && u.UserName == "novo@teste.com" && u.Nome == "Fulano" &&
                !string.IsNullOrEmpty(u.SecurityStamp)), "Senha@123");
            await _userManager.Received(1).AddToRoleAsync(Arg.Any<Usuario>(), "User");
        }

        [Fact]
        public async Task Register_SemRoleUserCadastrada_CriaSemAtribuirRole()
        {
            // Arrange
            _userManager.FindByEmailAsync("novo@teste.com").Returns((Usuario?)null);
            _userManager.CreateAsync(Arg.Any<Usuario>(), "Senha@123").Returns(IdentityResult.Success);
            _roleManager.RoleExistsAsync("User").Returns(false);

            // Act
            var resposta = await _service.Register(CriarRegistro());

            // Assert
            Assert.True(resposta.Success);
            await _userManager.DidNotReceive().AddToRoleAsync(Arg.Any<Usuario>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Register_ComEmailJaCadastrado_RetornaErro400()
        {
            // Arrange
            _userManager.FindByEmailAsync("novo@teste.com").Returns(CriarUsuario(null, default));

            // Act
            var resposta = await _service.Register(CriarRegistro());

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Usuário de e-mail novo@teste.com já existe.", resposta.ErrorMessage);
            await _userManager.DidNotReceive().CreateAsync(Arg.Any<Usuario>(), Arg.Any<string>());
        }

        [Fact]
        public async Task Register_FalhaNoIdentity_RetornaErro500()
        {
            // Arrange
            _userManager.FindByEmailAsync("novo@teste.com").Returns((Usuario?)null);
            _userManager.CreateAsync(Arg.Any<Usuario>(), "Senha@123").Returns(IdentityResult.Failed(
                new IdentityError { Description = "Senha fraca" }));

            // Act
            var resposta = await _service.Register(CriarRegistro());

            // Assert
            Assert.False(resposta.Success);
            Assert.Equal(StatusCodes.Status500InternalServerError, resposta.StatusCode);
            Assert.StartsWith("Erro na criação do usuário:", resposta.ErrorMessage);
            await _userManager.DidNotReceive().AddToRoleAsync(Arg.Any<Usuario>(), Arg.Any<string>());
        }

        [Fact(Skip = "Bug: Register interpola result.Errors direto na mensagem e devolve o nome do tipo da coleção, não as descrições dos erros")]
        public async Task Register_FalhaNoIdentity_MensagemTrazDescricaoDosErros()
        {
            // Arrange
            _userManager.FindByEmailAsync("novo@teste.com").Returns((Usuario?)null);
            _userManager.CreateAsync(Arg.Any<Usuario>(), "Senha@123").Returns(IdentityResult.Failed(
                new IdentityError { Description = "Senha fraca" }));

            // Act
            var resposta = await _service.Register(CriarRegistro());

            // Assert — o cliente precisa saber por que o cadastro falhou
            Assert.Contains("Senha fraca", resposta.ErrorMessage);
        }
        #endregion

        #region Revoke Tests
        [Fact]
        public async Task Revoke_UsuarioExistente_LimpaRefreshTokenERetorna200()
        {
            // Arrange
            var usuario = CriarUsuario("refresh-token-salvo", DateTime.MaxValue);
            _userManager.FindByEmailAsync(Email).Returns(usuario);

            // Act
            var resposta = await _service.Revoke(Email);

            // Assert
            Assert.True(resposta.Success);
            Assert.Equal("Token revogado com sucesso", resposta.Dados);
            Assert.Null(usuario.RefreshToken);
            await _userManager.Received(1).UpdateAsync(usuario);
        }

        [Fact]
        public async Task Revoke_UsuarioInexistente_RetornaErro400()
        {
            // Arrange
            _userManager.FindByEmailAsync(Email).Returns((Usuario?)null);

            // Act
            var resposta = await _service.Revoke(Email);

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Usuário Inválido", resposta.ErrorMessage);
            await _userManager.DidNotReceive().UpdateAsync(Arg.Any<Usuario>());
        }
        #endregion

        #region AlterarSenha Tests
        private static AlterarSenhaDTO CriarAlteracaoSenha(string confirmacao = "Nova@123")
        {
            return new AlterarSenhaDTO { SenhaAtual = "Atual@123", NovaSenha = "Nova@123", ConfirmacaoNovaSenha = confirmacao };
        }

        [Fact]
        public async Task AlterarSenha_ComDadosValidos_TrocaSenhaERetorna200()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByIdAsync("usuario-1").Returns(usuario);
            _userManager.ChangePasswordAsync(usuario, "Atual@123", "Nova@123").Returns(IdentityResult.Success);

            // Act
            var resposta = await _service.AlterarSenha("usuario-1", CriarAlteracaoSenha());

            // Assert
            Assert.True(resposta.Success);
            Assert.Equal("Senha alterada com sucesso.", resposta.Dados);
            await _userManager.Received(1).ChangePasswordAsync(usuario, "Atual@123", "Nova@123");
        }

        [Fact]
        public async Task AlterarSenha_ComConfirmacaoDiferente_RetornaErro400SemBuscarUsuario()
        {
            // Act
            var resposta = await _service.AlterarSenha("usuario-1", CriarAlteracaoSenha(confirmacao: "Outra@123"));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("A nova senha e a confirmação não coincidem.", resposta.ErrorMessage);
            await _userManager.DidNotReceive().FindByIdAsync(Arg.Any<string>());
        }

        [Fact]
        public async Task AlterarSenha_ComUsuarioInexistente_RetornaErro404()
        {
            // Arrange
            _userManager.FindByIdAsync("usuario-1").Returns((Usuario?)null);

            // Act
            var resposta = await _service.AlterarSenha("usuario-1", CriarAlteracaoSenha());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, resposta.StatusCode);
            Assert.Equal("Usuário não encontrado.", resposta.ErrorMessage);
        }

        [Fact]
        public async Task AlterarSenha_ComSenhaAtualErrada_RetornaErro400ComPrimeiraDescricao()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByIdAsync("usuario-1").Returns(usuario);
            _userManager.ChangePasswordAsync(usuario, "Atual@123", "Nova@123").Returns(IdentityResult.Failed(
                new IdentityError { Description = "Senha incorreta." },
                new IdentityError { Description = "Outro erro." }));

            // Act
            var resposta = await _service.AlterarSenha("usuario-1", CriarAlteracaoSenha());

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Senha incorreta.", resposta.ErrorMessage);
        }

        [Fact]
        public async Task AlterarSenha_FalhaSemDescricao_RetornaMensagemPadrao()
        {
            // Arrange
            var usuario = CriarUsuario(null, default);
            _userManager.FindByIdAsync("usuario-1").Returns(usuario);
            _userManager.ChangePasswordAsync(usuario, "Atual@123", "Nova@123").Returns(IdentityResult.Failed());

            // Act
            var resposta = await _service.AlterarSenha("usuario-1", CriarAlteracaoSenha());

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, resposta.StatusCode);
            Assert.Equal("Erro ao alterar a senha.", resposta.ErrorMessage);
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
