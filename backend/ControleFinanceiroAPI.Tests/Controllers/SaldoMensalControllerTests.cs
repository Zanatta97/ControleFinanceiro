using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.SaldoMensal;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class SaldoMensalControllerTests
    {
        private readonly ISaldoMensalService _service = Substitute.For<ISaldoMensalService>();
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly SaldoMensalController _controller;

        public SaldoMensalControllerTests()
        {
            _controller = new SaldoMensalController(_service).ComUsuario(_ambienteId);
        }

        #region GetByMes Tests
        [Fact]
        public async Task GetByMes_RepassaAmbienteMesEAnoERetorna200()
        {
            // Arrange
            var saldos = new List<SaldoMensalContaResponseDTO> { new() { NomeConta = "Corrente", SaldoFinal = 10 } };
            _service.GetByMesAsync(_ambienteId, 9, 2026).Returns(saldos);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<SaldoMensalContaResponseDTO>>(await _controller.GetByMes(9, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(saldos, corpo.Dados);
        }

        [Fact]
        public async Task GetByMes_SemAmbienteSelecionado_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new SaldoMensalController(_service).ComUsuario(ambienteId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetByMes(9, 2026));
        }
        #endregion

        #region SalvarSaldoInicial Tests
        [Fact]
        public async Task SalvarSaldoInicial_RepassaValoresERetorna200()
        {
            // Arrange
            var contaId = Guid.NewGuid();

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(
                await _controller.SalvarSaldoInicial(contaId, 9, 2026, new SaldoMensalContaRequestDTO { SaldoInicial = 1500 }));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Saldo inicial salvo com sucesso.", corpo.StatusMessage);
            await _service.Received(1).SalvarSaldoInicialAsync(_ambienteId, contaId, 9, 2026, 1500);
        }
        #endregion

        #region Calcular Tests
        [Fact]
        public async Task Calcular_CalculaEDevolveSaldosAtualizados()
        {
            // Arrange
            var saldos = new List<SaldoMensalContaResponseDTO> { new() { NomeConta = "Corrente" } };
            _service.GetByMesAsync(_ambienteId, 10, 2026).Returns(saldos);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<SaldoMensalContaResponseDTO>>(await _controller.Calcular(10, 2026));

            // Assert — calcula primeiro e só depois consulta
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Saldo inicial calculado com sucesso.", corpo.StatusMessage);
            Assert.Same(saldos, corpo.Dados);
            Received.InOrder(() =>
            {
                _service.CalcularSaldoInicialAsync(_ambienteId, 10, 2026);
                _service.GetByMesAsync(_ambienteId, 10, 2026);
            });
        }
        #endregion
    }
}
