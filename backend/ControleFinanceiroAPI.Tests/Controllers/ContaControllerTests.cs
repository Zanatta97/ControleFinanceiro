using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Conta;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class ContaControllerTests
    {
        private readonly IContaService _service = Substitute.For<IContaService>();
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly ContaController _controller;

        public ContaControllerTests()
        {
            _controller = new ContaController(_service).ComUsuario(_ambienteId);
        }

        private static Conta CriarConta(string nome = "Corrente")
        {
            return new Conta { Id = Guid.NewGuid(), Nome = nome, TipoConta = TipoConta.Corrente, Saldo = 10 };
        }

        private static ContaRequestDTO CriarRequest()
        {
            return new ContaRequestDTO { Nome = "Nubank", TipoConta = TipoConta.CartaoCredito, Saldo = 0 };
        }

        #region GetAll Tests
        [Fact]
        public async Task GetAll_SemContas_Retorna404()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Conta>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<ContaResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Nenhuma conta encontrada.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetAll_ComContas_Retorna200()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Conta> { CriarConta("A"), CriarConta("B") });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<ContaResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(["A", "B"], corpo.Dados!.Select(c => c.Nome));
        }
        #endregion

        #region GetAllByAmbiente Tests
        [Fact]
        public async Task GetAllByAmbiente_UsaAmbienteDoTokenERetorna200()
        {
            // Arrange
            _service.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Conta> { CriarConta() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<ContaResponseDTO>>(await _controller.GetAllByAmbiente());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
        }

        [Fact]
        public async Task GetAllByAmbiente_SemAmbienteSelecionado_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new ContaController(_service).ComUsuario(ambienteId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetAllByAmbiente());
        }
        #endregion

        #region GetById Tests
        [Fact]
        public async Task GetById_Existente_Retorna200()
        {
            // Arrange
            var conta = CriarConta();
            _service.GetAsync(conta.Id, _ambienteId).Returns(conta);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ContaResponseDTO>(await _controller.GetById(conta.Id));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(conta.Id, corpo.Dados!.Id);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            // Arrange
            _service.GetAsync(Arg.Any<Guid>(), _ambienteId).Returns((Conta?)null);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ContaResponseDTO>(await _controller.GetById(Guid.NewGuid()));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Conta não encontrada.", corpo.ErrorMessage);
        }
        #endregion

        #region Create Tests
        [Fact]
        public async Task Create_MapeiaComAmbienteEUsuarioERetorna201()
        {
            // Arrange
            _service.AddAsync(Arg.Any<Conta>()).Returns(ci => ci.Arg<Conta>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ContaResponseDTO>(await _controller.Create(CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status201Created, status);
            Assert.Equal("Nubank", corpo.Dados!.Nome);
            await _service.Received(1).AddAsync(Arg.Is<Conta>(c =>
                c.AmbienteId == _ambienteId && c.UsuarioId == ControllerTestHelper.UsuarioId));
        }

        [Fact]
        public async Task Create_SemUsuarioNoToken_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new ContaController(_service).ComUsuario(_ambienteId, usuarioId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.Create(CriarRequest()));
        }
        #endregion

        #region Update Tests
        [Fact]
        public async Task Update_RepassaIdEAmbienteERetorna200()
        {
            // Arrange
            var id = Guid.NewGuid();
            _service.UpdateAsync(id, Arg.Any<Conta>(), _ambienteId).Returns(CriarConta("Atualizada"));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ContaResponseDTO>(await _controller.Update(id, CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Atualizada", corpo.Dados!.Nome);
        }
        #endregion

        #region Delete Tests
        [Fact]
        public async Task Delete_Existente_Retorna200()
        {
            // Arrange
            var id = Guid.NewGuid();
            _service.DeleteAsync(id, _ambienteId).Returns(true);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.Delete(id));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Conta excluída com sucesso.", corpo.StatusMessage);
        }

        [Fact]
        public async Task Delete_Inexistente_Retorna404()
        {
            // Arrange
            _service.DeleteAsync(Arg.Any<Guid>(), _ambienteId).Returns(false);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.Delete(Guid.NewGuid()));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Conta não encontrada.", corpo.ErrorMessage);
        }
        #endregion
    }
}
