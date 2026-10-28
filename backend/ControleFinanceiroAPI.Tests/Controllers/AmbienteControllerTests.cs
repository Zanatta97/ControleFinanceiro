using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Ambiente;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Filters;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using System.Reflection;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class AmbienteControllerTests
    {
        private readonly IAmbienteService _service = Substitute.For<IAmbienteService>();
        private readonly AmbienteController _controller;
        private readonly Guid _ambienteId = Guid.NewGuid();
        private const string UsuarioId = ControllerTestHelper.UsuarioId;

        public AmbienteControllerTests()
        {
            // Rotas de ambiente não dependem de ambiente selecionado, só do usuário
            _controller = new AmbienteController(_service).ComUsuario(ambienteId: null);
        }

        private Ambiente CriarAmbiente(string nome = "Família")
        {
            return new Ambiente { Id = _ambienteId, Nome = nome };
        }

        #region GetAll Tests
        [Fact]
        public async Task GetAll_SemAmbientes_Retorna404()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Ambiente>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<AmbienteResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Nenhum ambiente encontrado.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetAll_ComAmbientes_Retorna200()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Ambiente> { CriarAmbiente("A"), CriarAmbiente("B") });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<AmbienteResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(["A", "B"], corpo.Dados!.Select(a => a.Nome));
        }
        #endregion

        #region GetAllByUsuario Tests
        [Fact]
        public async Task GetAllByUsuario_UsaUsuarioDoToken()
        {
            // Arrange
            _service.GetAllByUsuarioAsync(UsuarioId).Returns(new List<Ambiente> { CriarAmbiente() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<AmbienteResponseDTO>>(await _controller.GetAllByUsuario());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
        }

        [Fact]
        public async Task GetAllByUsuario_SemUsuarioNoToken_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new AmbienteController(_service).ComUsuario(ambienteId: null, usuarioId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetAllByUsuario());
        }
        #endregion

        #region GetById Tests
        [Fact]
        public async Task GetById_UsuarioMembro_Retorna200()
        {
            // Arrange
            _service.GetAsync(_ambienteId, UsuarioId).Returns(CriarAmbiente());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<AmbienteResponseDTO>(await _controller.GetById(_ambienteId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(_ambienteId, corpo.Dados!.Id);
        }

        [Fact]
        public async Task GetById_NaoMembroOuInexistente_Retorna404()
        {
            // Arrange
            _service.GetAsync(_ambienteId, UsuarioId).Returns((Ambiente?)null);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<AmbienteResponseDTO>(await _controller.GetById(_ambienteId));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Ambiente não encontrado.", corpo.ErrorMessage);
        }
        #endregion

        #region GetMembros Tests
        [Fact]
        public async Task GetMembros_RetornaResumoDosUsuarios()
        {
            // Arrange
            _service.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Usuario>
            {
                new() { Id = "u1", Nome = "Ana", Email = "ana@teste.com" },
                new() { Id = "u2", Nome = "Bia", Email = "bia@teste.com" }
            });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<UsuarioResumoDTO>>(await _controller.GetMembros(_ambienteId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(["Ana", "Bia"], corpo.Dados!.Select(u => u.Nome));
        }
        #endregion

        #region Create Tests
        [Fact]
        public async Task Create_RepassaUsuarioERetorna201()
        {
            // Arrange
            _service.AddAsync(Arg.Any<Ambiente>(), UsuarioId).Returns(ci => ci.Arg<Ambiente>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<AmbienteResponseDTO>(
                await _controller.Create(new AmbienteRequestDTO { Nome = "Casa" }));

            // Assert
            Assert.Equal(StatusCodes.Status201Created, status);
            Assert.Equal("Casa", corpo.Dados!.Nome);
            await _service.Received(1).AddAsync(Arg.Is<Ambiente>(a => a.Nome == "Casa"), UsuarioId);
        }
        #endregion

        #region Update Tests
        [Fact]
        public async Task Update_RepassaIdEUsuarioERetorna200()
        {
            // Arrange
            _service.UpdateAsync(_ambienteId, Arg.Any<Ambiente>(), UsuarioId).Returns(CriarAmbiente("Novo"));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<AmbienteResponseDTO>(
                await _controller.Update(_ambienteId, new AmbienteRequestDTO { Nome = "Novo" }));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Novo", corpo.Dados!.Nome);
        }

        [Fact]
        public async Task Update_SemPermissao_PropagaUnauthorizedAccessException()
        {
            // Arrange
            _service.UpdateAsync(_ambienteId, Arg.Any<Ambiente>(), UsuarioId)
                .ThrowsAsync(new UnauthorizedAccessException("Apenas o Dono ou um Admin do sistema pode editar o ambiente."));

            // Act / Assert — o middleware devolve 403
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
                _controller.Update(_ambienteId, new AmbienteRequestDTO { Nome = "Novo" }));
        }
        #endregion

        #region Delete Tests
        [Fact]
        public async Task Delete_Existente_Retorna200()
        {
            // Arrange
            _service.DeleteAsync(_ambienteId, UsuarioId).Returns(true);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.Delete(_ambienteId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Ambiente excluído com sucesso.", corpo.StatusMessage);
        }

        [Fact]
        public async Task Delete_Inexistente_Retorna404()
        {
            // Arrange
            _service.DeleteAsync(_ambienteId, UsuarioId).Returns(false);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.Delete(_ambienteId));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Ambiente não encontrado.", corpo.ErrorMessage);
        }
        #endregion

        #region Membros Tests
        [Fact]
        public async Task AdicionarMembro_RepassaEmailERetorna200()
        {
            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.AdicionarMembro(_ambienteId, "novo@teste.com"));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Membro adicionado com sucesso.", corpo.StatusMessage);
            await _service.Received(1).AdicionarMembroAsync(_ambienteId, UsuarioId, "novo@teste.com");
        }

        [Fact]
        public async Task AdicionarMembro_UsuarioJaMembro_PropagaInvalidOperationException()
        {
            // Arrange
            _service.AdicionarMembroAsync(_ambienteId, UsuarioId, "novo@teste.com")
                .ThrowsAsync(new InvalidOperationException("Este usuário já é membro do ambiente."));

            // Act / Assert — o middleware devolve 409
            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.AdicionarMembro(_ambienteId, "novo@teste.com"));
        }

        [Fact]
        public async Task RemoverMembro_RepassaMembroERetorna200()
        {
            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.RemoverMembro(_ambienteId, "usuario-2"));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Membro removido com sucesso.", corpo.StatusMessage);
            await _service.Received(1).RemoverMembroAsync(_ambienteId, UsuarioId, "usuario-2");
        }

        [Fact]
        public async Task AtribuirDono_RepassaMembroERetorna200()
        {
            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.AtribuirDono(_ambienteId, "usuario-2"));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Dono atribuído com sucesso.", corpo.StatusMessage);
            await _service.Received(1).AtribuirDonoAsync(_ambienteId, UsuarioId, "usuario-2");
        }
        #endregion

        #region SelecionarAmbiente Tests
        [Fact]
        public async Task SelecionarAmbiente_RetornaNovoTokenCom200()
        {
            // Arrange
            var token = new TokenDTO { AccessToken = "access", RefreshToken = "refresh" };
            _service.SelecionarAmbienteAsync(_ambienteId, UsuarioId).Returns(token);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<TokenDTO>(await _controller.SelecionarAmbiente(_ambienteId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(token, corpo.Dados);
        }

        [Fact]
        public async Task SelecionarAmbiente_NaoMembro_PropagaUnauthorizedAccessException()
        {
            // Arrange
            _service.SelecionarAmbienteAsync(_ambienteId, UsuarioId)
                .ThrowsAsync(new UnauthorizedAccessException("Você não é membro deste ambiente."));

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _controller.SelecionarAmbiente(_ambienteId));
        }
        #endregion

        #region Atributos Tests
        [Theory]
        [InlineData(nameof(AmbienteController.Create))]
        [InlineData(nameof(AmbienteController.Update))]
        [InlineData(nameof(AmbienteController.Delete))]
        [InlineData(nameof(AmbienteController.AdicionarMembro))]
        [InlineData(nameof(AmbienteController.RemoverMembro))]
        [InlineData(nameof(AmbienteController.AtribuirDono))]
        public void AcoesDeGestao_TemBloquearDemo(string acao)
        {
            // Act
            var metodo = typeof(AmbienteController).GetMethod(acao)!;

            // Assert — usuário demo não pode alterar ambientes
            Assert.NotNull(metodo.GetCustomAttribute<BloquearDemoAttribute>());
        }

        [Fact]
        public void GetAll_ExigeRoleAdmin()
        {
            // Act
            var autorizacao = typeof(AmbienteController).GetMethod(nameof(AmbienteController.GetAll))!
                .GetCustomAttribute<AuthorizeAttribute>();

            // Assert
            Assert.Equal("Admin", autorizacao?.Roles);
        }
        #endregion
    }
}
