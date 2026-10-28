using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Relatorio;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class RelatorioControllerTests
    {
        private readonly IRelatorioService _service = Substitute.For<IRelatorioService>();
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly RelatorioController _controller;

        public RelatorioControllerTests()
        {
            _controller = new RelatorioController(_service).ComUsuario(_ambienteId);
        }

        #region GetResumo Tests
        [Fact]
        public async Task GetResumo_RepassaAmbienteERetorna200()
        {
            // Arrange
            var resumo = new ResumoFinanceiroResponseDTO { Mes = 9, Ano = 2026, Saldo = 100 };
            _service.GetResumoAsync(_ambienteId, 9, 2026).Returns(resumo);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ResumoFinanceiroResponseDTO>(await _controller.GetResumo(9, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(resumo, corpo.Dados);
        }

        [Fact]
        public async Task GetResumo_SemAmbienteSelecionado_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new RelatorioController(_service).ComUsuario(ambienteId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetResumo(9, 2026));
        }
        #endregion

        #region GetGastoPorCategoria Tests
        [Fact]
        public async Task GetGastoPorCategoria_ComDados_Retorna200()
        {
            // Arrange
            var gastos = new List<GastoPorCategoriaResponseDTO> { new() { NomeCategoria = "Mercado" } };
            _service.GetGastoPorCategoriaAsync(_ambienteId, 9, 2026, default, default).Returns(gastos);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<GastoPorCategoriaResponseDTO>>(
                await _controller.GetGastoPorCategoria(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(gastos, corpo.Dados);
        }

        [Fact]
        public async Task GetGastoPorCategoria_ComPeriodo_RepassaDatas()
        {
            // Arrange
            var inicio = new DateTime(2026, 9, 1);
            var fim = new DateTime(2026, 9, 30);
            _service.GetGastoPorCategoriaAsync(_ambienteId, 0, 0, inicio, fim)
                .Returns(new List<GastoPorCategoriaResponseDTO> { new() });

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<GastoPorCategoriaResponseDTO>>(
                await _controller.GetGastoPorCategoria(0, 0, inicio, fim));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            await _service.Received(1).GetGastoPorCategoriaAsync(_ambienteId, 0, 0, inicio, fim);
        }

        [Fact]
        public async Task GetGastoPorCategoria_SemDados_Retorna404()
        {
            // Arrange
            _service.GetGastoPorCategoriaAsync(_ambienteId, 9, 2026, default, default)
                .Returns(new List<GastoPorCategoriaResponseDTO>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<GastoPorCategoriaResponseDTO>>(
                await _controller.GetGastoPorCategoria(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Nenhuma despesa encontrada no período.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetGastoPorCategoria_FiltroInvalido_Retorna400ComMensagem()
        {
            // Arrange
            _service.GetGastoPorCategoriaAsync(_ambienteId, 0, 0, default, default)
                .ThrowsAsync(new ArgumentException("Informe mês/ano ou período de datas."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<GastoPorCategoriaResponseDTO>>(
                await _controller.GetGastoPorCategoria(0, 0, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Informe mês/ano ou período de datas.", corpo.ErrorMessage);
        }
        #endregion

        #region GetGastosPorConta Tests
        [Fact]
        public async Task GetGastosPorConta_ComDados_Retorna200()
        {
            // Arrange
            var gastos = new List<GastosPorContaResponseDTO> { new() { NomeConta = "Cartão" } };
            _service.GetGastosPorContaAsync(_ambienteId, 9, 2026, default, default).Returns(gastos);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<GastosPorContaResponseDTO>>(
                await _controller.GetGastosPorConta(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(gastos, corpo.Dados);
        }

        [Fact]
        public async Task GetGastosPorConta_SemDados_Retorna404()
        {
            // Arrange
            _service.GetGastosPorContaAsync(_ambienteId, 9, 2026, default, default).Returns(new List<GastosPorContaResponseDTO>());

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<GastosPorContaResponseDTO>>(
                await _controller.GetGastosPorConta(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
        }

        [Fact]
        public async Task GetGastosPorConta_FiltroInvalido_Retorna400()
        {
            // Arrange
            _service.GetGastosPorContaAsync(_ambienteId, Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .ThrowsAsync(new ArgumentException("Informe apenas mês/ano ou período de datas, não ambos."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<GastosPorContaResponseDTO>>(
                await _controller.GetGastosPorConta(9, 2026, DateTime.Today, DateTime.Today));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Informe apenas mês/ano ou período de datas, não ambos.", corpo.ErrorMessage);
        }
        #endregion

        #region GetComparativoCategoria Tests
        [Fact]
        public async Task GetComparativoCategoria_ComDados_Retorna200()
        {
            // Arrange
            var comparativo = new List<ComparativoCategoriaResponseDTO> { new() { NomeCategoria = "Mercado" } };
            _service.GetComparativoCategoriaAsync(_ambienteId, 9, 2026).Returns(comparativo);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<ComparativoCategoriaResponseDTO>>(
                await _controller.GetComparativoCategoria(9, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(comparativo, corpo.Dados);
        }

        [Fact]
        public async Task GetComparativoCategoria_SemDados_Retorna404()
        {
            // Arrange
            _service.GetComparativoCategoriaAsync(_ambienteId, 9, 2026).Returns(new List<ComparativoCategoriaResponseDTO>());

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<ComparativoCategoriaResponseDTO>>(
                await _controller.GetComparativoCategoria(9, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
        }

        [Fact]
        public async Task GetComparativoCategoria_MesInvalido_Retorna400()
        {
            // Arrange
            _service.GetComparativoCategoriaAsync(_ambienteId, 13, 2026)
                .ThrowsAsync(new ArgumentException("Informe um mês entre 1 e 12."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<ComparativoCategoriaResponseDTO>>(
                await _controller.GetComparativoCategoria(13, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Informe um mês entre 1 e 12.", corpo.ErrorMessage);
        }
        #endregion

        #region GetMaioresDespesas Tests
        [Fact]
        public async Task GetMaioresDespesas_RepassaQuantidadeERetorna200()
        {
            // Arrange
            var despesas = new List<MaiorDespesaResponseDTO> { new() { Valor = 100 } };
            _service.GetMaioresDespesasAsync(_ambienteId, 5, 9, 2026, default, default).Returns(despesas);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<MaiorDespesaResponseDTO>>(
                await _controller.GetMaioresDespesas(9, 2026, null, null, 5));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(despesas, corpo.Dados);
        }

        [Fact]
        public async Task GetMaioresDespesas_SemDados_Retorna404()
        {
            // Arrange
            _service.GetMaioresDespesasAsync(_ambienteId, 10, 9, 2026, default, default).Returns(new List<MaiorDespesaResponseDTO>());

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<MaiorDespesaResponseDTO>>(
                await _controller.GetMaioresDespesas(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
        }

        [Fact]
        public async Task GetMaioresDespesas_QuantidadeInvalida_Retorna400()
        {
            // Arrange
            _service.GetMaioresDespesasAsync(_ambienteId, 0, 9, 2026, default, default)
                .ThrowsAsync(new ArgumentException("A quantidade deve estar entre 1 e 100."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<MaiorDespesaResponseDTO>>(
                await _controller.GetMaioresDespesas(9, 2026, null, null, 0));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("A quantidade deve estar entre 1 e 100.", corpo.ErrorMessage);
        }
        #endregion

        #region GetMatrizCategoriaMes Tests
        [Fact]
        public async Task GetMatrizCategoriaMes_Retorna200()
        {
            // Arrange
            var matriz = new MatrizCategoriaMesResponseDTO { Ano = 2026 };
            _service.GetMatrizCategoriaMesAsync(_ambienteId, 2026).Returns(matriz);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<MatrizCategoriaMesResponseDTO>(await _controller.GetMatrizCategoriaMes(2026));

            // Assert — matriz vazia continua 200
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(matriz, corpo.Dados);
        }

        [Fact]
        public async Task GetMatrizCategoriaMes_AnoInvalido_Retorna400()
        {
            // Arrange
            _service.GetMatrizCategoriaMesAsync(_ambienteId, 1800).ThrowsAsync(new ArgumentException("Informe um ano entre 1900 e 2100."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<MatrizCategoriaMesResponseDTO>(await _controller.GetMatrizCategoriaMes(1800));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Informe um ano entre 1900 e 2100.", corpo.ErrorMessage);
        }
        #endregion

        #region GetFrequenciaTicketMedio Tests
        [Fact]
        public async Task GetFrequenciaTicketMedio_ComDados_Retorna200()
        {
            // Arrange
            var frequencia = new List<FrequenciaCategoriaResponseDTO> { new() { Quantidade = 3 } };
            _service.GetFrequenciaTicketMedioAsync(_ambienteId, 9, 2026, default, default).Returns(frequencia);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<FrequenciaCategoriaResponseDTO>>(
                await _controller.GetFrequenciaTicketMedio(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(frequencia, corpo.Dados);
        }

        [Fact]
        public async Task GetFrequenciaTicketMedio_SemDados_Retorna404()
        {
            // Arrange
            _service.GetFrequenciaTicketMedioAsync(_ambienteId, 9, 2026, default, default).Returns(new List<FrequenciaCategoriaResponseDTO>());

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<FrequenciaCategoriaResponseDTO>>(
                await _controller.GetFrequenciaTicketMedio(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
        }

        [Fact]
        public async Task GetFrequenciaTicketMedio_FiltroInvalido_Retorna400()
        {
            // Arrange
            _service.GetFrequenciaTicketMedioAsync(_ambienteId, 0, 0, default, default)
                .ThrowsAsync(new ArgumentException("Informe mês/ano ou período de datas."));

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<FrequenciaCategoriaResponseDTO>>(
                await _controller.GetFrequenciaTicketMedio(0, 0, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
        }
        #endregion

        #region GetRitmoMes Tests
        [Fact]
        public async Task GetRitmoMes_UsaDataAtualERetorna200()
        {
            // Arrange
            var ritmo = new RitmoMesResponseDTO { GastoAteHoje = 100 };
            _service.GetRitmoMesAsync(_ambienteId, null).Returns(ritmo);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<RitmoMesResponseDTO>(await _controller.GetRitmoMes());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(ritmo, corpo.Dados);
        }
        #endregion

        #region GetFaturaCompetencia Tests
        [Fact]
        public async Task GetFaturaCompetencia_Cartao_Retorna200()
        {
            // Arrange
            var contaId = Guid.NewGuid();
            var fatura = new FaturaCompetenciaResponseDTO { ContaId = contaId, SaldoEmAberto = 200 };
            _service.GetFaturaCompetenciaAsync(_ambienteId, contaId, 9, 2026).Returns(fatura);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<FaturaCompetenciaResponseDTO>(
                await _controller.GetFaturaCompetencia(contaId, 9, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(fatura, corpo.Dados);
        }

        [Fact]
        public async Task GetFaturaCompetencia_ContaQueNaoECartao_Retorna400()
        {
            // Arrange
            var contaId = Guid.NewGuid();
            _service.GetFaturaCompetenciaAsync(_ambienteId, contaId, 9, 2026)
                .ThrowsAsync(new ArgumentException("A conta informada não é um cartão de crédito."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<FaturaCompetenciaResponseDTO>(
                await _controller.GetFaturaCompetencia(contaId, 9, 2026));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("A conta informada não é um cartão de crédito.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetFaturaCompetencia_ContaForaDoAmbiente_PropagaKeyNotFoundException()
        {
            // Arrange
            var contaId = Guid.NewGuid();
            _service.GetFaturaCompetenciaAsync(_ambienteId, contaId, 9, 2026)
                .ThrowsAsync(new KeyNotFoundException("Conta não encontrada."));

            // Act / Assert — o ErrorHandlingMiddleware devolve 404
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.GetFaturaCompetencia(contaId, 9, 2026));
        }
        #endregion

        #region GetProjecaoParcelas Tests
        [Fact]
        public async Task GetProjecaoParcelas_RepassaParametrosERetorna200()
        {
            // Arrange
            var projecao = new ProjecaoParcelasResponseDTO { QuantidadeMeses = 6 };
            _service.GetProjecaoParcelasAsync(_ambienteId, 9, 2026, 6).Returns(projecao);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ProjecaoParcelasResponseDTO>(
                await _controller.GetProjecaoParcelas(9, 2026, 6));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(projecao, corpo.Dados);
        }

        [Fact]
        public async Task GetProjecaoParcelas_ParametrosInvalidos_Retorna400()
        {
            // Arrange
            _service.GetProjecaoParcelasAsync(_ambienteId, 9, null, 12)
                .ThrowsAsync(new ArgumentException("Informe mês e ano juntos, ou nenhum dos dois."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ProjecaoParcelasResponseDTO>(
                await _controller.GetProjecaoParcelas(9, null));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Informe mês e ano juntos, ou nenhum dos dois.", corpo.ErrorMessage);
        }
        #endregion

        #region GetProjecaoProximoMes Tests
        [Fact]
        public async Task GetProjecaoProximoMes_RepassaCategoriasERetorna200()
        {
            // Arrange
            var fixas = new List<Guid> { Guid.NewGuid() };
            var recebimento = new List<Guid> { Guid.NewGuid() };
            var projecao = new ProjecaoProximoMesResponseDTO { Mes = 10, Ano = 2026 };
            _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026, fixas, recebimento).Returns(projecao);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ProjecaoProximoMesResponseDTO>(
                await _controller.GetProjecaoProximoMes(10, 2026, fixas, recebimento));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(projecao, corpo.Dados);
        }

        [Fact]
        public async Task GetProjecaoProximoMes_CategoriaInvalida_Retorna400()
        {
            // Arrange
            _service.GetProjecaoProximoMesAsync(_ambienteId, 10, 2026, Arg.Any<IEnumerable<Guid>?>(), Arg.Any<IEnumerable<Guid>?>())
                .ThrowsAsync(new ArgumentException("Uma mesma categoria não pode estar entre as fixas e as de recebimento."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ProjecaoProximoMesResponseDTO>(
                await _controller.GetProjecaoProximoMes(10, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Uma mesma categoria não pode estar entre as fixas e as de recebimento.", corpo.ErrorMessage);
        }
        #endregion

        #region GetFixosRecebimentos Tests
        [Fact]
        public async Task GetFixosRecebimentos_RepassaCategoriasERetorna200()
        {
            // Arrange
            var fixas = new List<Guid> { Guid.NewGuid() };
            var recebimento = new List<Guid> { Guid.NewGuid() };
            var relatorio = new FixosRecebimentosResponseDTO { Mes = 9, Ano = 2026 };
            _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, fixas, recebimento).Returns(relatorio);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<FixosRecebimentosResponseDTO>(
                await _controller.GetFixosRecebimentos(9, 2026, fixas, recebimento));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(relatorio, corpo.Dados);
        }

        [Fact]
        public async Task GetFixosRecebimentos_SemCategorias_Retorna400()
        {
            // Arrange
            _service.GetFixosRecebimentosAsync(_ambienteId, 9, 2026, null, null)
                .ThrowsAsync(new ArgumentException("Informe ao menos uma categoria de gasto fixo."));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<FixosRecebimentosResponseDTO>(
                await _controller.GetFixosRecebimentos(9, 2026, null, null));

            // Assert
            Assert.Equal(StatusCodes.Status400BadRequest, status);
            Assert.Equal("Informe ao menos uma categoria de gasto fixo.", corpo.ErrorMessage);
        }
        #endregion

        #region GetEvolucaoMensal Tests
        [Fact]
        public async Task GetEvolucaoMensal_Retorna200()
        {
            // Arrange
            var evolucao = new EvolucaoMensalResponseDTO { Ano = 2026 };
            _service.GetEvolucaoMensalAsync(_ambienteId, 2026).Returns(evolucao);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<EvolucaoMensalResponseDTO>(await _controller.GetEvolucaoMensal(2026));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(evolucao, corpo.Dados);
        }
        #endregion

        #region GetOrcamentosStatus Tests
        [Fact]
        public async Task GetOrcamentosStatus_ComOrcamentos_Retorna200()
        {
            // Arrange
            var orcamentos = new List<OrcamentoStatusResponseDTO> { new() { NomeOrcamento = "Mercado" } };
            _service.GetOrcamentosStatusAsync(_ambienteId).Returns(orcamentos);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<OrcamentoStatusResponseDTO>>(await _controller.GetOrcamentosStatus());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(orcamentos, corpo.Dados);
        }

        [Fact]
        public async Task GetOrcamentosStatus_SemOrcamentos_Retorna404()
        {
            // Arrange
            _service.GetOrcamentosStatusAsync(_ambienteId).Returns(new List<OrcamentoStatusResponseDTO>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<OrcamentoStatusResponseDTO>>(await _controller.GetOrcamentosStatus());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Nenhum orçamento encontrado.", corpo.ErrorMessage);
        }
        #endregion

        #region GetExtratoConta Tests
        [Fact]
        public async Task GetExtratoConta_RepassaContaEPeriodoERetorna200()
        {
            // Arrange
            var contaId = Guid.NewGuid();
            var inicio = new DateTime(2026, 9, 1);
            var fim = new DateTime(2026, 9, 30);
            var extrato = new ExtratoContaResponseDTO { ContaId = contaId };
            _service.GetExtratoContaAsync(_ambienteId, contaId, inicio, fim).Returns(extrato);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<ExtratoContaResponseDTO>(
                await _controller.GetExtratoConta(contaId, inicio, fim));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Same(extrato, corpo.Dados);
        }

        [Fact]
        public async Task GetExtratoConta_ContaForaDoAmbiente_PropagaKeyNotFoundException()
        {
            // Arrange
            _service.GetExtratoContaAsync(_ambienteId, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
                .ThrowsAsync(new KeyNotFoundException("Conta não encontrada."));

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _controller.GetExtratoConta(Guid.NewGuid(), DateTime.Today, DateTime.Today));
        }
        #endregion
    }
}
