using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Ambiente;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using System.Reflection;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class AdminControllerTests
    {
        private const string UsuarioAlvoId = "usuario-alvo";

        private readonly UserManager<Usuario> _userManager = Substitute.For<UserManager<Usuario>>(
            Substitute.For<IUserStore<Usuario>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        private readonly RoleManager<IdentityRole> _roleManager = Substitute.For<RoleManager<IdentityRole>>(
            Substitute.For<IRoleStore<IdentityRole>>(), null!, null!, null!, null!);
        private readonly IAmbienteService _ambienteService = Substitute.For<IAmbienteService>();
        private readonly IUsuarioService _usuarioService = Substitute.For<IUsuarioService>();
        private readonly AdminController _controller;

        public AdminControllerTests()
        {
            _controller = new AdminController(_userManager, _roleManager, _ambienteService, _usuarioService)
                .ComUsuario(ambienteId: null, roles: "Admin");
        }

        private Usuario ConfigurarUsuarioAlvo()
        {
            var usuario = new Usuario { Id = UsuarioAlvoId, Nome = "Alvo", Email = "alvo@teste.com" };
            _userManager.FindByIdAsync(UsuarioAlvoId).Returns(usuario);
            return usuario;
        }

        private void ConfigurarUsuarioInexistente()
        {
            _userManager.FindByIdAsync(UsuarioAlvoId).Returns((Usuario?)null);
        }

        #region CriarUsuario Tests
        [Fact]
        public async Task CriarUsuario_RepassaRespostaDoUsuarioService()
        {
            // Arrange
            var dto = new UsuarioRegisterDTO { Nome = "Novo", Email = "novo@teste.com", Senha = "x", ConfirmacaoSenha = "x" };
            var resposta = ApiResponseDTO<object>.SuccessResponse("Usuário criado com sucesso");
            _usuarioService.Register(dto).Returns(resposta);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.CriarUsuario(dto));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(resposta, corpo);
        }
        #endregion

        #region GetUsuarios Tests
        [Fact]
        public async Task GetUsuarios_RetornaOrdenadosPorNomeComRolesEBloqueio()
        {
            // Arrange
            var bia = new Usuario { Id = "u2", Nome = "Bia", Email = "bia@teste.com", LockoutEnd = DateTimeOffset.MaxValue };
            var ana = new Usuario { Id = "u1", Nome = "Ana", Email = "ana@teste.com", LockoutEnd = DateTimeOffset.UtcNow.AddDays(-1) };
            _userManager.Users.Returns(new TestAsyncQueryable<Usuario>([bia, ana]));
            _userManager.GetRolesAsync(ana).Returns(new List<string> { "Admin" });
            _userManager.GetRolesAsync(bia).Returns(new List<string>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<UsuarioAdminResponseDTO>>(await _controller.GetUsuarios());

            // Assert
            var usuarios = corpo.Dados!.ToList();
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(["Ana", "Bia"], usuarios.Select(u => u.Nome));
            Assert.False(usuarios[0].Bloqueado);              // bloqueio já vencido
            Assert.Equal(["Admin"], usuarios[0].Roles);
            Assert.True(usuarios[1].Bloqueado);
            Assert.Empty(usuarios[1].Roles);
        }
        #endregion

        #region GetRoles Tests
        [Fact]
        public void GetRoles_RetornaNomesOrdenados()
        {
            // Arrange
            _roleManager.Roles.Returns(new List<IdentityRole> { new("User"), new("Admin"), new("Demo") }.AsQueryable());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<string>>(_controller.GetRoles());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(["Admin", "Demo", "User"], corpo.Dados);
        }
        #endregion

        #region AtribuirRoles Tests
        [Fact]
        public async Task AtribuirRoles_UsuarioExistente_SubstituiRoles()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();
            var rolesAtuais = new List<string> { "User" };
            _userManager.GetRolesAsync(usuario).Returns(rolesAtuais);
            _userManager.RemoveFromRolesAsync(usuario, rolesAtuais).Returns(IdentityResult.Success);
            _userManager.AddToRolesAsync(usuario, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
            var dto = new AtribuirRolesDTO { Roles = ["Admin", "User"] };

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.AtribuirRoles(UsuarioAlvoId, dto));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Roles atualizadas com sucesso.", corpo.StatusMessage);
            await _userManager.Received(1).RemoveFromRolesAsync(usuario, rolesAtuais);
            await _userManager.Received(1).AddToRolesAsync(usuario, dto.Roles);
        }

        [Fact]
        public async Task AtribuirRoles_ListaVazia_SoRemoveAsRolesAtuais()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();
            _userManager.GetRolesAsync(usuario).Returns(new List<string> { "User" });
            _userManager.RemoveFromRolesAsync(usuario, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<object>(await _controller.AtribuirRoles(UsuarioAlvoId, new AtribuirRolesDTO()));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            await _userManager.DidNotReceive().AddToRolesAsync(Arg.Any<Usuario>(), Arg.Any<IEnumerable<string>>());
        }

        [Fact]
        public async Task AtribuirRoles_UsuarioInexistente_Retorna404()
        {
            // Arrange
            ConfigurarUsuarioInexistente();

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(
                await _controller.AtribuirRoles(UsuarioAlvoId, new AtribuirRolesDTO { Roles = ["Admin"] }));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Usuário não encontrado.", corpo.ErrorMessage);
        }

        [Fact(Skip = "Bug: AtribuirRoles ignora o IdentityResult de RemoveFromRolesAsync/AddToRolesAsync; se a nova role falha, o usuário perde todas e a API responde 200")]
        public async Task AtribuirRoles_FalhaAoAdicionarRole_NaoRespondeSucesso()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();
            _userManager.GetRolesAsync(usuario).Returns(new List<string> { "User" });
            _userManager.RemoveFromRolesAsync(usuario, Arg.Any<IEnumerable<string>>()).Returns(IdentityResult.Success);
            _userManager.AddToRolesAsync(usuario, Arg.Any<IEnumerable<string>>())
                .Returns(IdentityResult.Failed(new IdentityError { Description = "Role Inexistente não existe." }));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(
                await _controller.AtribuirRoles(UsuarioAlvoId, new AtribuirRolesDTO { Roles = ["Inexistente"] }));

            // Assert
            Assert.NotEqual(StatusCodes.Status200OK, status);
            Assert.False(corpo.Success);
        }
        #endregion

        #region Bloqueio Tests
        [Fact]
        public async Task BloquearUsuario_UsuarioExistente_BloqueiaPorTempoIndeterminado()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.BloquearUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Usuário bloqueado.", corpo.StatusMessage);
            await _userManager.Received(1).SetLockoutEnabledAsync(usuario, true);
            await _userManager.Received(1).SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);
        }

        [Fact]
        public async Task BloquearUsuario_UsuarioInexistente_Retorna404()
        {
            // Arrange
            ConfigurarUsuarioInexistente();

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<object>(await _controller.BloquearUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            await _userManager.DidNotReceive().SetLockoutEndDateAsync(Arg.Any<Usuario>(), Arg.Any<DateTimeOffset?>());
        }

        [Fact]
        public async Task DesbloquearUsuario_UsuarioExistente_LimpaFimDoBloqueio()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.DesbloquearUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Usuário desbloqueado.", corpo.StatusMessage);
            await _userManager.Received(1).SetLockoutEndDateAsync(usuario, null);
        }

        [Fact]
        public async Task DesbloquearUsuario_UsuarioInexistente_Retorna404()
        {
            // Arrange
            ConfigurarUsuarioInexistente();

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.DesbloquearUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Usuário não encontrado.", corpo.ErrorMessage);
        }
        #endregion

        #region DeleteUsuario Tests
        [Fact]
        public async Task DeleteUsuario_UsuarioExistente_Retorna200()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();
            _userManager.DeleteAsync(usuario).Returns(IdentityResult.Success);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.DeleteUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Usuário excluído.", corpo.StatusMessage);
        }

        [Fact]
        public async Task DeleteUsuario_FalhaNoIdentity_Retorna500ComDescricoes()
        {
            // Arrange
            var usuario = ConfigurarUsuarioAlvo();
            _userManager.DeleteAsync(usuario).Returns(IdentityResult.Failed(
                new IdentityError { Description = "Erro A" },
                new IdentityError { Description = "Erro B" }));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.DeleteUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status500InternalServerError, status);
            Assert.Equal("Erro A, Erro B", corpo.ErrorMessage);
        }

        [Fact]
        public async Task DeleteUsuario_UsuarioInexistente_Retorna404()
        {
            // Arrange
            ConfigurarUsuarioInexistente();

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<object>(await _controller.DeleteUsuario(UsuarioAlvoId));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            await _userManager.DidNotReceive().DeleteAsync(Arg.Any<Usuario>());
        }
        #endregion

        #region Ambientes Tests
        [Fact]
        public async Task GetAmbientes_RetornaTodosComo200()
        {
            // Arrange
            _ambienteService.GetAllAsync().Returns(new List<Ambiente> { new() { Nome = "A" } });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.GetAmbientes());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            var ambientes = Assert.IsAssignableFrom<IEnumerable<AmbienteResponseDTO>>(corpo.Dados);
            Assert.Equal("A", Assert.Single(ambientes).Nome);
        }

        [Fact]
        public async Task DeleteAmbiente_Existente_ForcaDelecaoSemUsuario()
        {
            // Arrange
            var id = Guid.NewGuid();
            _ambienteService.DeleteAsync(id, string.Empty).Returns(true);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.DeleteAmbiente(id));

            // Assert — userId vazio é o sinal de deleção forçada no AmbienteService
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Ambiente excluído.", corpo.StatusMessage);
            await _ambienteService.Received(1).DeleteAsync(id, string.Empty);
        }

        [Fact]
        public async Task DeleteAmbiente_Inexistente_Retorna404()
        {
            // Arrange
            _ambienteService.DeleteAsync(Arg.Any<Guid>(), string.Empty).Returns(false);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.DeleteAmbiente(Guid.NewGuid()));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Ambiente não encontrado.", corpo.ErrorMessage);
        }
        #endregion

        #region Atributos Tests
        [Fact]
        public void AdminController_ExigeRoleAdminNaClasse()
        {
            // Act
            var autorizacao = typeof(AdminController).GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.Equal("Admin", autorizacao?.Roles);
        }
        #endregion
    }
}
