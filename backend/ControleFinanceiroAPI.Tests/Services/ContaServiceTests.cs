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
    public class ContaServiceTests
    {
        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly ContaService _service;
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly Guid _outroAmbienteId = Guid.NewGuid();

        public ContaServiceTests()
        {
            _service = new ContaService(_repository, Substitute.For<ILogger<ContaService>>());
        }

        private Conta CriarConta(Guid? ambienteId = null)
        {
            return new Conta
            {
                Id = Guid.NewGuid(),
                Nome = "Corrente",
                TipoConta = TipoConta.Corrente,
                Saldo = 100,
                AmbienteId = ambienteId ?? _ambienteId
            };
        }

        #region Consultas Tests
        [Fact]
        public async Task GetAllAsync_RetornaTodasDoRepositorio()
        {
            // Arrange
            var contas = new List<Conta> { CriarConta(), CriarConta(_outroAmbienteId) };
            _repository.ContaRepository.GetAllAsync().Returns(contas);

            // Act
            var resultado = await _service.GetAllAsync();

            // Assert
            Assert.Same(contas, resultado);
        }

        [Fact]
        public async Task GetAllByAmbienteAsync_RepassaAmbienteAoRepositorio()
        {
            // Arrange
            var contas = new List<Conta> { CriarConta() };
            _repository.ContaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(contas);

            // Act
            var resultado = await _service.GetAllByAmbienteAsync(_ambienteId);

            // Assert
            Assert.Same(contas, resultado);
        }

        [Fact]
        public async Task GetAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var conta = CriarConta();
            var deOutroAmbiente = CriarConta(_outroAmbienteId);
            _repository.ContaRepository.ComDados(conta, deOutroAmbiente);

            // Act
            var encontrada = await _service.GetAsync(conta.Id, _ambienteId);
            var bloqueada = await _service.GetAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(conta, encontrada);
            Assert.Null(bloqueada);
        }

        [Fact]
        public async Task GetReadOnlyAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var conta = CriarConta();
            var deOutroAmbiente = CriarConta(_outroAmbienteId);
            _repository.ContaRepository.ComDados(conta, deOutroAmbiente);

            // Act
            var encontrada = await _service.GetReadOnlyAsync(conta.Id, _ambienteId);
            var bloqueada = await _service.GetReadOnlyAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(conta, encontrada);
            Assert.Null(bloqueada);
        }
        #endregion

        #region AddAsync Tests
        [Fact]
        public async Task AddAsync_ComConta_AdicionaESalva()
        {
            // Arrange
            var conta = CriarConta();

            // Act
            var resultado = await _service.AddAsync(conta);

            // Assert
            Assert.Same(conta, resultado);
            _repository.ContaRepository.Received(1).Add(conta);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_ComContaNula_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.AddAsync(null!));
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(() => _service.AddAsync(CriarConta()));
        }
        #endregion

        #region UpdateAsync Tests
        [Fact]
        public async Task UpdateAsync_ContaExistente_CopiaCamposESalva()
        {
            // Arrange
            var existente = CriarConta();
            _repository.ContaRepository.ComDados(existente);
            var alterada = new Conta { Nome = "Nubank", TipoConta = TipoConta.CartaoCredito, Saldo = -50 };

            // Act
            var resultado = await _service.UpdateAsync(existente.Id, alterada, _ambienteId);

            // Assert
            Assert.Same(existente, resultado);
            Assert.Equal("Nubank", existente.Nome);
            Assert.Equal(TipoConta.CartaoCredito, existente.TipoConta);
            Assert.Equal(-50, existente.Saldo);
            Assert.Equal(_ambienteId, existente.AmbienteId);
            _repository.ContaRepository.Received(1).Update(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_ContaInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _repository.ContaRepository.ComDados();

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(Guid.NewGuid(), CriarConta(), _ambienteId));

            // Assert
            Assert.Equal("Conta não encontrada", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_ContaDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var deOutroAmbiente = CriarConta(_outroAmbienteId);
            _repository.ContaRepository.ComDados(deOutroAmbiente);

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(deOutroAmbiente.Id, CriarConta(), _ambienteId));
            _repository.ContaRepository.DidNotReceive().Update(Arg.Any<Conta>());
        }

        [Fact]
        public async Task UpdateAsync_ComContaNula_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(Guid.NewGuid(), null!, _ambienteId));
        }
        #endregion

        #region DeleteAsync Tests
        [Fact]
        public async Task DeleteAsync_ContaExistente_DeletaERetornaTrue()
        {
            // Arrange
            var existente = CriarConta();
            _repository.ContaRepository.ComDados(existente);

            // Act
            var resultado = await _service.DeleteAsync(existente.Id, _ambienteId);

            // Assert
            Assert.True(resultado);
            _repository.ContaRepository.Received(1).Delete(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ContaInexistente_RetornaFalseSemSalvar()
        {
            // Arrange
            _repository.ContaRepository.ComDados();

            // Act
            var resultado = await _service.DeleteAsync(Guid.NewGuid(), _ambienteId);

            // Assert
            Assert.False(resultado);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_ContaDeOutroAmbiente_RetornaFalse()
        {
            // Arrange
            var deOutroAmbiente = CriarConta(_outroAmbienteId);
            _repository.ContaRepository.ComDados(deOutroAmbiente);

            // Act
            var resultado = await _service.DeleteAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.False(resultado);
            _repository.ContaRepository.DidNotReceive().Delete(Arg.Any<Conta>());
        }

        [Fact]
        public async Task DeleteAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            var existente = CriarConta();
            _repository.ContaRepository.ComDados(existente);
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(existente.Id, _ambienteId));
        }
        #endregion
    }
}
