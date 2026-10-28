using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Orcamento;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class OrcamentoControllerTests
    {
        private readonly IOrcamentoService _service = Substitute.For<IOrcamentoService>();
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly OrcamentoController _controller;

        public OrcamentoControllerTests()
        {
            _controller = new OrcamentoController(_service).ComUsuario(_ambienteId);
        }

        private static Orcamento CriarOrcamento(string nome = "Mercado")
        {
            return new Orcamento { Id = Guid.NewGuid(), Nome = nome, ValorLimite = 500, StatusOrcamento = StatusOrcamento.Ativo };
        }

        private static OrcamentoRequestDTO CriarRequest()
        {
            return new OrcamentoRequestDTO
            {
                Nome = "Lazer",
                Descricao = "Saídas",
                ValorLimite = 300,
                DataLimite = new DateTime(2026, 12, 31),
                StatusOrcamento = StatusOrcamento.Ativo,
                CategoriaId = Guid.NewGuid()
            };
        }

        #region GetAll Tests
        [Fact]
        public async Task GetAll_SemOrcamentos_Retorna404()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Orcamento>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<OrcamentoResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Nenhum orçamento encontrado.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetAll_ComOrcamentos_Retorna200()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Orcamento> { CriarOrcamento("A"), CriarOrcamento("B") });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<OrcamentoResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(["A", "B"], corpo.Dados!.Select(o => o.Nome));
        }
        #endregion

        #region Consultas do ambiente Tests
        [Fact]
        public async Task GetAllByAmbiente_UsaAmbienteDoTokenERetorna200()
        {
            // Arrange
            _service.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Orcamento> { CriarOrcamento() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<OrcamentoResponseDTO>>(await _controller.GetAllByAmbiente());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
        }

        [Fact]
        public async Task GetByStatus_RepassaStatusEAmbiente()
        {
            // Arrange
            _service.GetByStatusAsync(_ambienteId, StatusOrcamento.Inativo).Returns(new List<Orcamento> { CriarOrcamento() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<OrcamentoResponseDTO>>(await _controller.GetByStatus(StatusOrcamento.Inativo));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
            await _service.Received(1).GetByStatusAsync(_ambienteId, StatusOrcamento.Inativo);
        }

        [Fact]
        public async Task GetByStatus_SemAmbienteSelecionado_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new OrcamentoController(_service).ComUsuario(ambienteId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetByStatus(StatusOrcamento.Ativo));
        }
        #endregion

        #region GetById Tests
        [Fact]
        public async Task GetById_Existente_Retorna200()
        {
            // Arrange
            var orcamento = CriarOrcamento();
            _service.GetAsync(orcamento.Id, _ambienteId).Returns(orcamento);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<OrcamentoResponseDTO>(await _controller.GetById(orcamento.Id));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(orcamento.Id, corpo.Dados!.Id);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            // Arrange
            _service.GetAsync(Arg.Any<Guid>(), _ambienteId).Returns((Orcamento?)null);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<OrcamentoResponseDTO>(await _controller.GetById(Guid.NewGuid()));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Orçamento não encontrado.", corpo.ErrorMessage);
        }
        #endregion

        #region Create Tests
        [Fact]
        public async Task Create_MapeiaComAmbienteEUsuarioERetorna201()
        {
            // Arrange
            _service.AddAsync(Arg.Any<Orcamento>()).Returns(ci => ci.Arg<Orcamento>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<OrcamentoResponseDTO>(await _controller.Create(CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status201Created, status);
            Assert.Equal("Lazer", corpo.Dados!.Nome);
            await _service.Received(1).AddAsync(Arg.Is<Orcamento>(o =>
                o.AmbienteId == _ambienteId && o.UsuarioId == ControllerTestHelper.UsuarioId));
        }
        #endregion

        #region Update Tests
        [Fact]
        public async Task Update_RepassaIdEAmbienteERetorna200()
        {
            // Arrange
            var id = Guid.NewGuid();
            _service.UpdateAsync(id, Arg.Any<Orcamento>(), _ambienteId).Returns(CriarOrcamento("Atualizado"));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<OrcamentoResponseDTO>(await _controller.Update(id, CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Atualizado", corpo.Dados!.Nome);
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
            Assert.Equal("Orçamento excluído com sucesso.", corpo.StatusMessage);
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
            Assert.Equal("Orçamento não encontrado.", corpo.ErrorMessage);
        }
        #endregion
    }
}
