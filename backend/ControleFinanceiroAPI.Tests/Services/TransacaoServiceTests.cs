using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class TransacaoServiceTests
    {
        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly TransacaoService _service;
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly Guid _outroAmbienteId = Guid.NewGuid();
        private readonly Conta _contaOrigem;
        private readonly Conta _contaDestino;
        private readonly Conta _contaDeOutroAmbiente;

        public TransacaoServiceTests()
        {
            _contaOrigem = new Conta { Id = Guid.NewGuid(), Nome = "Corrente", AmbienteId = _ambienteId };
            _contaDestino = new Conta { Id = Guid.NewGuid(), Nome = "Cartão", AmbienteId = _ambienteId };
            _contaDeOutroAmbiente = new Conta { Id = Guid.NewGuid(), Nome = "Conta alheia", AmbienteId = _outroAmbienteId };

            _repository.ContaRepository.ComDados(_contaOrigem, _contaDestino, _contaDeOutroAmbiente);

            _service = new TransacaoService(_repository, Substitute.For<ILogger<TransacaoService>>());
        }

        private Transacao CriarTransacao(TipoTransacao tipo = TipoTransacao.Despesa, Guid? contaDestinoId = null, Guid? ambienteId = null)
        {
            return new Transacao
            {
                Id = Guid.NewGuid(),
                Descricao = "Mercado",
                Valor = 100,
                Data = new DateTime(2026, 9, 10),
                TipoTransacao = tipo,
                CategoriaId = Guid.NewGuid(),
                ContaId = _contaOrigem.Id,
                ContaDestinoId = contaDestinoId,
                AmbienteId = ambienteId ?? _ambienteId,
                MesCompetencia = new DateOnly(2026, 9, 1)
            };
        }

        #region Consultas Tests
        [Fact]
        public async Task GetAllAsync_RetornaTodasDoRepositorio()
        {
            // Arrange
            var transacoes = new List<Transacao> { CriarTransacao(), CriarTransacao() };
            _repository.TransacaoRepository.GetAllAsync().Returns(transacoes);

            // Act
            var resultado = await _service.GetAllAsync();

            // Assert
            Assert.Same(transacoes, resultado);
        }

        [Fact]
        public async Task GetAllByAmbienteAsync_RepassaAmbienteAoRepositorio()
        {
            // Arrange
            var transacoes = new List<Transacao> { CriarTransacao() };
            _repository.TransacaoRepository.GetAllByAmbienteAsync(_ambienteId).Returns(transacoes);

            // Act
            var resultado = await _service.GetAllByAmbienteAsync(_ambienteId);

            // Assert
            Assert.Same(transacoes, resultado);
        }

        [Fact]
        public async Task GetByContaAsync_RepassaContaEAmbienteAoRepositorio()
        {
            // Arrange — sem o ambiente a consulta vazaria transações de outro espaço (#69)
            var transacoes = new List<Transacao> { CriarTransacao() };
            _repository.TransacaoRepository.GetByContaAsync(_contaOrigem.Id, _ambienteId).Returns(transacoes);

            // Act
            var resultado = await _service.GetByContaAsync(_contaOrigem.Id, _ambienteId);

            // Assert
            Assert.Same(transacoes, resultado);
            await _repository.TransacaoRepository.Received(1).GetByContaAsync(_contaOrigem.Id, _ambienteId);
        }

        [Fact]
        public async Task GetByPeriodoAsync_RepassaAmbienteEPeriodo()
        {
            // Arrange
            var inicio = new DateTime(2026, 9, 1);
            var fim = new DateTime(2026, 9, 30);
            var transacoes = new List<Transacao> { CriarTransacao() };
            _repository.TransacaoRepository.GetByPeriodoAsync(_ambienteId, inicio, fim).Returns(transacoes);

            // Act
            var resultado = await _service.GetByPeriodoAsync(_ambienteId, inicio, fim);

            // Assert
            Assert.Same(transacoes, resultado);
        }

        [Fact]
        public async Task GetByTipoAsync_RepassaAmbienteETipo()
        {
            // Arrange
            var transacoes = new List<Transacao> { CriarTransacao(TipoTransacao.Receita) };
            _repository.TransacaoRepository.GetByTipoAsync(_ambienteId, TipoTransacao.Receita).Returns(transacoes);

            // Act
            var resultado = await _service.GetByTipoAsync(_ambienteId, TipoTransacao.Receita);

            // Assert
            Assert.Same(transacoes, resultado);
        }

        [Fact]
        public async Task GetAsync_DoMesmoAmbiente_RetornaTransacao()
        {
            // Arrange
            var transacao = CriarTransacao();
            _repository.TransacaoRepository.ComDados(transacao);

            // Act
            var resultado = await _service.GetAsync(transacao.Id, _ambienteId);

            // Assert
            Assert.Same(transacao, resultado);
        }

        [Fact]
        public async Task GetAsync_DeOutroAmbiente_RetornaNulo()
        {
            // Arrange
            var transacao = CriarTransacao(ambienteId: _outroAmbienteId);
            _repository.TransacaoRepository.ComDados(transacao);

            // Act
            var resultado = await _service.GetAsync(transacao.Id, _ambienteId);

            // Assert
            Assert.Null(resultado);
        }

        [Fact]
        public async Task GetReadOnlyAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var transacao = CriarTransacao();
            var deOutroAmbiente = CriarTransacao(ambienteId: _outroAmbienteId);
            _repository.TransacaoRepository.ComDados(transacao, deOutroAmbiente);

            // Act
            var encontrada = await _service.GetReadOnlyAsync(transacao.Id, _ambienteId);
            var bloqueada = await _service.GetReadOnlyAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(transacao, encontrada);
            Assert.Null(bloqueada);
        }
        #endregion

        #region AddAsync Tests
        [Theory]
        [InlineData(TipoTransacao.Receita)]
        [InlineData(TipoTransacao.Despesa)]
        public async Task AddAsync_ReceitaOuDespesaSemContaDestino_AdicionaESalva(TipoTransacao tipo)
        {
            // Arrange
            var transacao = CriarTransacao(tipo);

            // Act
            var resultado = await _service.AddAsync(transacao);

            // Assert
            Assert.Same(transacao, resultado);
            _repository.TransacaoRepository.Received(1).Add(transacao);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_TransferenciaValida_AdicionaESalva()
        {
            // Arrange
            var transacao = CriarTransacao(TipoTransacao.Transferencia, _contaDestino.Id);

            // Act
            var resultado = await _service.AddAsync(transacao);

            // Assert
            Assert.Same(transacao, resultado);
            _repository.TransacaoRepository.Received(1).Add(transacao);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_ComTransacaoNula_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddAsync(null!));
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Theory]
        [InlineData(TipoTransacao.Receita)]
        [InlineData(TipoTransacao.Despesa)]
        public async Task AddAsync_ReceitaOuDespesaComContaDestino_LancaInvalidOperationException(TipoTransacao tipo)
        {
            // Arrange
            var transacao = CriarTransacao(tipo, _contaDestino.Id);

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("Conta de destino só pode ser informada em transferências.", excecao.Message);
            _repository.TransacaoRepository.DidNotReceive().Add(Arg.Any<Transacao>());
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_TransferenciaSemContaDestino_LancaInvalidOperationException()
        {
            // Arrange
            var transacao = CriarTransacao(TipoTransacao.Transferencia, contaDestinoId: null);

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("Transferência exige uma conta de destino.", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_TransferenciaComContaDestinoVazia_LancaInvalidOperationException()
        {
            // Arrange
            var transacao = CriarTransacao(TipoTransacao.Transferencia, Guid.Empty);

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("Transferência exige uma conta de destino.", excecao.Message);
        }

        [Fact]
        public async Task AddAsync_TransferenciaParaAPropriaConta_LancaInvalidOperationException()
        {
            // Arrange
            var transacao = CriarTransacao(TipoTransacao.Transferencia, _contaOrigem.Id);

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("A conta de destino deve ser diferente da conta de origem.", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_TransferenciaParaContaDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange — conta existe, mas pertence a outro espaço
            var transacao = CriarTransacao(TipoTransacao.Transferencia, _contaDeOutroAmbiente.Id);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("Conta de destino não encontrada neste ambiente.", excecao.Message);
            _repository.TransacaoRepository.DidNotReceive().Add(Arg.Any<Transacao>());
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_TransferenciaParaContaInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            var transacao = CriarTransacao(TipoTransacao.Transferencia, Guid.NewGuid());

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("Conta de destino não encontrada neste ambiente.", excecao.Message);
        }

        [Fact]
        public async Task AddAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            var transacao = CriarTransacao();
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act
            var excecao = await Assert.ThrowsAsync<Exception>(() => _service.AddAsync(transacao));

            // Assert
            Assert.Equal("falha no banco", excecao.Message);
        }
        #endregion

        #region AddManyAsync Tests
        [Fact]
        public async Task AddManyAsync_ComParcelasValidas_AdicionaTodasESalvaUmaVez()
        {
            // Arrange
            var parcelas = Enumerable.Range(0, 3).Select(_ => CriarTransacao()).ToList();

            // Act
            var resultado = (await _service.AddManyAsync(parcelas)).ToList();

            // Assert
            Assert.Equal(parcelas, resultado);
            _repository.TransacaoRepository.Received(3).Add(Arg.Any<Transacao>());
            foreach (var parcela in parcelas)
                _repository.TransacaoRepository.Received(1).Add(parcela);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddManyAsync_ComUmaParcelaInvalida_NaoAdicionaNenhuma()
        {
            // Arrange — validação acontece antes de qualquer Add: ou entram todas, ou nenhuma
            var parcelas = new List<Transacao>
            {
                CriarTransacao(),
                CriarTransacao(TipoTransacao.Despesa, _contaDestino.Id),
                CriarTransacao()
            };

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.AddManyAsync(parcelas));

            // Assert
            Assert.Equal("Conta de destino só pode ser informada em transferências.", excecao.Message);
            _repository.TransacaoRepository.DidNotReceive().Add(Arg.Any<Transacao>());
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddManyAsync_TransferenciasParaOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var parcelas = new List<Transacao>
            {
                CriarTransacao(TipoTransacao.Transferencia, _contaDestino.Id),
                CriarTransacao(TipoTransacao.Transferencia, _contaDeOutroAmbiente.Id)
            };

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _service.AddManyAsync(parcelas));
            _repository.TransacaoRepository.DidNotReceive().Add(Arg.Any<Transacao>());
        }

        [Fact]
        public async Task AddManyAsync_ComListaNula_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddManyAsync(null!));
        }

        [Fact]
        public async Task AddManyAsync_ComListaVazia_RetornaVazio()
        {
            // Act
            var resultado = await _service.AddManyAsync([]);

            // Assert
            Assert.Empty(resultado);
            _repository.TransacaoRepository.DidNotReceive().Add(Arg.Any<Transacao>());
        }

        [Fact]
        public async Task AddManyAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(() => _service.AddManyAsync([CriarTransacao()]));
        }
        #endregion

        #region UpdateAsync Tests
        [Fact]
        public async Task UpdateAsync_TransacaoExistente_CopiaCamposESalva()
        {
            // Arrange
            var existente = CriarTransacao();
            _repository.TransacaoRepository.ComDados(existente);

            var alterada = new Transacao
            {
                Descricao = "Farmácia",
                Valor = 55.9m,
                Data = new DateTime(2026, 10, 2),
                Observacao = "nova obs",
                TipoTransacao = TipoTransacao.Transferencia,
                CategoriaId = Guid.NewGuid(),
                ContaId = _contaOrigem.Id,
                ContaDestinoId = _contaDestino.Id,
                MesCompetencia = new DateOnly(2026, 10, 1)
            };

            // Act
            var resultado = await _service.UpdateAsync(existente.Id, alterada, _ambienteId);

            // Assert
            Assert.Same(existente, resultado);
            Assert.Equal("Farmácia", existente.Descricao);
            Assert.Equal(55.9m, existente.Valor);
            Assert.Equal(new DateTime(2026, 10, 2), existente.Data);
            Assert.Equal("nova obs", existente.Observacao);
            Assert.Equal(TipoTransacao.Transferencia, existente.TipoTransacao);
            Assert.Equal(alterada.CategoriaId, existente.CategoriaId);
            Assert.Equal(_contaDestino.Id, existente.ContaDestinoId);
            Assert.Equal(new DateOnly(2026, 10, 1), existente.MesCompetencia);
            Assert.Equal(_ambienteId, existente.AmbienteId); // ambiente não muda
            _repository.TransacaoRepository.Received(1).Update(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_TransferenciaValidaComAmbienteDoParametro_UsaAmbienteInformado()
        {
            // Arrange — a entidade vinda do DTO não traz AmbienteId confiável: vale o do parâmetro
            var existente = CriarTransacao();
            _repository.TransacaoRepository.ComDados(existente);
            var alterada = CriarTransacao(TipoTransacao.Transferencia, _contaDestino.Id);
            alterada.AmbienteId = null;

            // Act
            var resultado = await _service.UpdateAsync(existente.Id, alterada, _ambienteId);

            // Assert
            Assert.Equal(_contaDestino.Id, resultado.ContaDestinoId);
        }

        [Fact]
        public async Task UpdateAsync_TransacaoInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _repository.TransacaoRepository.ComDados();

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(Guid.NewGuid(), CriarTransacao(), _ambienteId));

            // Assert
            Assert.Equal("Transação não encontrada", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_TransacaoDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var deOutroAmbiente = CriarTransacao(ambienteId: _outroAmbienteId);
            _repository.TransacaoRepository.ComDados(deOutroAmbiente);

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(deOutroAmbiente.Id, CriarTransacao(), _ambienteId));
            _repository.TransacaoRepository.DidNotReceive().Update(Arg.Any<Transacao>());
        }

        [Fact]
        public async Task UpdateAsync_ComContaDestinoInvalida_ValidaAntesDeBuscar()
        {
            // Arrange
            var existente = CriarTransacao();
            _repository.TransacaoRepository.ComDados(existente);

            // Act
            var excecao = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.UpdateAsync(existente.Id, CriarTransacao(TipoTransacao.Receita, _contaDestino.Id), _ambienteId));

            // Assert
            Assert.Equal("Conta de destino só pode ser informada em transferências.", excecao.Message);
            Assert.Equal("Mercado", existente.Descricao);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_TransferenciaParaContaDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var existente = CriarTransacao();
            _repository.TransacaoRepository.ComDados(existente);

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(existente.Id, CriarTransacao(TipoTransacao.Transferencia, _contaDeOutroAmbiente.Id), _ambienteId));

            // Assert
            Assert.Equal("Conta de destino não encontrada neste ambiente.", excecao.Message);
        }

        [Fact]
        public async Task UpdateAsync_ComTransacaoNula_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(Guid.NewGuid(), null!, _ambienteId));
        }
        #endregion

        #region DeleteAsync Tests
        [Fact]
        public async Task DeleteAsync_TransacaoExistente_DeletaERetornaTrue()
        {
            // Arrange
            var existente = CriarTransacao();
            _repository.TransacaoRepository.ComDados(existente);

            // Act
            var resultado = await _service.DeleteAsync(existente.Id, _ambienteId);

            // Assert
            Assert.True(resultado);
            _repository.TransacaoRepository.Received(1).Delete(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_TransacaoInexistente_RetornaFalseSemSalvar()
        {
            // Arrange
            _repository.TransacaoRepository.ComDados();

            // Act
            var resultado = await _service.DeleteAsync(Guid.NewGuid(), _ambienteId);

            // Assert
            Assert.False(resultado);
            _repository.TransacaoRepository.DidNotReceive().Delete(Arg.Any<Transacao>());
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_TransacaoDeOutroAmbiente_RetornaFalse()
        {
            // Arrange
            var deOutroAmbiente = CriarTransacao(ambienteId: _outroAmbienteId);
            _repository.TransacaoRepository.ComDados(deOutroAmbiente);

            // Act
            var resultado = await _service.DeleteAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.False(resultado);
            _repository.TransacaoRepository.DidNotReceive().Delete(Arg.Any<Transacao>());
        }

        [Fact]
        public async Task DeleteAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            var existente = CriarTransacao();
            _repository.TransacaoRepository.ComDados(existente);
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(existente.Id, _ambienteId));
        }
        #endregion
    }
}
