using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using NSubstitute;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class SaldoMensalServiceTests
    {
        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly SaldoMensalService _service;
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly Conta _corrente;
        private readonly Conta _cartao;

        public SaldoMensalServiceTests()
        {
            _corrente = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", TipoConta = TipoConta.Corrente, AmbienteId = _ambienteId };
            _cartao = new Conta { Id = Guid.NewGuid(), Nome = "Cartão", TipoConta = TipoConta.CartaoCredito, AmbienteId = _ambienteId };

            _repository.ContaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Conta> { _corrente, _cartao });

            _service = new SaldoMensalService(_repository);
        }

        private void ConfigurarTransacoes(Conta conta, int mes, int ano, params Transacao[] transacoes)
        {
            _repository.TransacaoRepository
                .GetByMesCompetenciaEContaAsync(_ambienteId, conta.Id, mes, ano)
                .Returns(transacoes.ToList());
        }

        #region GetByMesAsync Tests
        [Fact]
        public async Task GetByMesAsync_ComSaldoSalvoEMovimento_CalculaSaldoFinalPorConta()
        {
            // Arrange — corrente tem saldo inicial salvo; cartão não (vale 0)
            _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(_ambienteId, new DateOnly(2026, 9, 1))
                .Returns(new List<SaldoMensalConta> { new() { ContaId = _corrente.Id, SaldoInicial = 1000 } });

            var pagamentoFatura = new Transacao
            {
                TipoTransacao = TipoTransacao.Transferencia, Valor = 300, ContaId = _corrente.Id, ContaDestinoId = _cartao.Id
            };

            ConfigurarTransacoes(_corrente, 9, 2026,
                new Transacao { TipoTransacao = TipoTransacao.Receita, Valor = 5000, ContaId = _corrente.Id },
                new Transacao { TipoTransacao = TipoTransacao.Despesa, Valor = 200, ContaId = _corrente.Id },
                pagamentoFatura);
            ConfigurarTransacoes(_cartao, 9, 2026,
                new Transacao { TipoTransacao = TipoTransacao.Despesa, Valor = 450, ContaId = _cartao.Id },
                pagamentoFatura);

            // Act
            var resultado = (await _service.GetByMesAsync(_ambienteId, 9, 2026)).ToList();

            // Assert
            Assert.Equal(2, resultado.Count);

            var corrente = Assert.Single(resultado, r => r.ContaId == _corrente.Id);
            Assert.Equal("Corrente", corrente.NomeConta);
            Assert.Equal(TipoConta.Corrente, corrente.TipoConta);
            Assert.Equal(1000, corrente.SaldoInicial);
            Assert.Equal(5000, corrente.TotalEntradas);
            Assert.Equal(500, corrente.TotalSaidas);      // despesa + transferência saindo
            Assert.Equal(5500, corrente.SaldoFinal);
            Assert.Equal(4500, corrente.Diferenca);

            var cartao = Assert.Single(resultado, r => r.ContaId == _cartao.Id);
            Assert.Equal(0, cartao.SaldoInicial);
            Assert.Equal(300, cartao.TotalEntradas);       // pagamento entra no cartão
            Assert.Equal(450, cartao.TotalSaidas);
            Assert.Equal(-150, cartao.SaldoFinal);
            Assert.Equal(-150, cartao.Diferenca);
        }

        [Fact]
        public async Task GetByMesAsync_SemContas_RetornaVazio()
        {
            // Arrange
            _repository.ContaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Conta>());
            _repository.SaldoMensalRepository.GetAllByAmbienteAsync(_ambienteId, Arg.Any<DateOnly>())
                .Returns(new List<SaldoMensalConta>());

            // Act
            var resultado = await _service.GetByMesAsync(_ambienteId, 9, 2026);

            // Assert
            Assert.Empty(resultado);
        }

        [Fact]
        public async Task GetByMesAsync_SemMovimento_SaldoFinalIgualAoInicial()
        {
            // Arrange
            _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(_ambienteId, new DateOnly(2026, 9, 1))
                .Returns(new List<SaldoMensalConta> { new() { ContaId = _corrente.Id, SaldoInicial = 250 } });
            ConfigurarTransacoes(_corrente, 9, 2026);
            ConfigurarTransacoes(_cartao, 9, 2026);

            // Act
            var corrente = (await _service.GetByMesAsync(_ambienteId, 9, 2026)).Single(r => r.ContaId == _corrente.Id);

            // Assert
            Assert.Equal(250, corrente.SaldoFinal);
            Assert.Equal(0, corrente.Diferenca);
        }

        [Fact]
        public async Task GetByMesAsync_ComMesInvalido_LancaArgumentOutOfRangeException()
        {
            // Act / Assert — sem validação própria, a data inválida estoura no DateOnly
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetByMesAsync(_ambienteId, 13, 2026));
        }
        #endregion

        #region SalvarSaldoInicialAsync Tests
        [Fact]
        public async Task SalvarSaldoInicialAsync_GravaSaldoNoDia1DoMesESalva()
        {
            // Act
            await _service.SalvarSaldoInicialAsync(_ambienteId, _corrente.Id, 9, 2026, 1234.56m);

            // Assert
            _repository.SaldoMensalRepository.Received(1).AddOrUpdate(Arg.Is<SaldoMensalConta>(s =>
                s.ContaId == _corrente.Id &&
                s.AmbienteId == _ambienteId &&
                s.Mes == new DateOnly(2026, 9, 1) &&
                s.SaldoInicial == 1234.56m));
            await _repository.Received(1).SaveChangesAsync();
        }
        #endregion

        #region CalcularSaldoInicialAsync Tests
        [Fact]
        public async Task CalcularSaldoInicialAsync_MesComum_UsaSaldoFinalDoMesAnterior()
        {
            // Arrange — saldo inicial de 10/2026 = saldo final de 09/2026
            _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(_ambienteId, new DateOnly(2026, 9, 1))
                .Returns(new List<SaldoMensalConta> { new() { ContaId = _corrente.Id, SaldoInicial = 1000 } });

            ConfigurarTransacoes(_corrente, 9, 2026,
                new Transacao { TipoTransacao = TipoTransacao.Receita, Valor = 500, ContaId = _corrente.Id },
                new Transacao { TipoTransacao = TipoTransacao.Despesa, Valor = 200, ContaId = _corrente.Id });
            ConfigurarTransacoes(_cartao, 9, 2026,
                new Transacao { TipoTransacao = TipoTransacao.Despesa, Valor = 80, ContaId = _cartao.Id });

            // Act
            await _service.CalcularSaldoInicialAsync(_ambienteId, 10, 2026);

            // Assert
            _repository.SaldoMensalRepository.Received(1).AddOrUpdate(Arg.Is<SaldoMensalConta>(s =>
                s.ContaId == _corrente.Id && s.AmbienteId == _ambienteId &&
                s.Mes == new DateOnly(2026, 10, 1) && s.SaldoInicial == 1300));
            _repository.SaldoMensalRepository.Received(1).AddOrUpdate(Arg.Is<SaldoMensalConta>(s =>
                s.ContaId == _cartao.Id && s.Mes == new DateOnly(2026, 10, 1) && s.SaldoInicial == -80));
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task CalcularSaldoInicialAsync_EmJaneiro_UsaDezembroDoAnoAnterior()
        {
            // Arrange
            _repository.SaldoMensalRepository
                .GetAllByAmbienteAsync(_ambienteId, new DateOnly(2025, 12, 1))
                .Returns(new List<SaldoMensalConta> { new() { ContaId = _corrente.Id, SaldoInicial = 100 } });
            ConfigurarTransacoes(_corrente, 12, 2025,
                new Transacao { TipoTransacao = TipoTransacao.Receita, Valor = 50, ContaId = _corrente.Id });
            ConfigurarTransacoes(_cartao, 12, 2025);

            // Act
            await _service.CalcularSaldoInicialAsync(_ambienteId, 1, 2026);

            // Assert
            await _repository.TransacaoRepository.Received(1).GetByMesCompetenciaEContaAsync(_ambienteId, _corrente.Id, 12, 2025);
            _repository.SaldoMensalRepository.Received(1).AddOrUpdate(Arg.Is<SaldoMensalConta>(s =>
                s.ContaId == _corrente.Id && s.Mes == new DateOnly(2026, 1, 1) && s.SaldoInicial == 150));
        }

        [Fact]
        public async Task CalcularSaldoInicialAsync_SemContas_SoSalva()
        {
            // Arrange
            _repository.ContaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Conta>());
            _repository.SaldoMensalRepository.GetAllByAmbienteAsync(_ambienteId, Arg.Any<DateOnly>())
                .Returns(new List<SaldoMensalConta>());

            // Act
            await _service.CalcularSaldoInicialAsync(_ambienteId, 5, 2026);

            // Assert
            _repository.SaldoMensalRepository.DidNotReceive().AddOrUpdate(Arg.Any<SaldoMensalConta>());
            await _repository.Received(1).SaveChangesAsync();
        }
        #endregion
    }
}
