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
    public class OrcamentoServiceTests
    {
        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly OrcamentoService _service;
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly Guid _outroAmbienteId = Guid.NewGuid();

        public OrcamentoServiceTests()
        {
            _service = new OrcamentoService(_repository, Substitute.For<ILogger<OrcamentoService>>());
        }

        private Orcamento CriarOrcamento(Guid? ambienteId = null)
        {
            return new Orcamento
            {
                Id = Guid.NewGuid(),
                Nome = "Mercado",
                Descricao = "Limite do mercado",
                ValorLimite = 800,
                DataLimite = new DateTime(2026, 9, 30),
                StatusOrcamento = StatusOrcamento.Ativo,
                CategoriaId = Guid.NewGuid(),
                AmbienteId = ambienteId ?? _ambienteId
            };
        }

        #region Consultas Tests
        [Fact]
        public async Task GetAllAsync_RetornaTodosDoRepositorio()
        {
            // Arrange
            var orcamentos = new List<Orcamento> { CriarOrcamento(), CriarOrcamento(_outroAmbienteId) };
            _repository.OrcamentoRepository.GetAllAsync().Returns(orcamentos);

            // Act
            var resultado = await _service.GetAllAsync();

            // Assert
            Assert.Same(orcamentos, resultado);
        }

        [Fact]
        public async Task GetAllByAmbienteAsync_RepassaAmbienteAoRepositorio()
        {
            // Arrange
            var orcamentos = new List<Orcamento> { CriarOrcamento() };
            _repository.OrcamentoRepository.GetAllByAmbienteAsync(_ambienteId).Returns(orcamentos);

            // Act
            var resultado = await _service.GetAllByAmbienteAsync(_ambienteId);

            // Assert
            Assert.Same(orcamentos, resultado);
        }

        [Theory]
        [InlineData(StatusOrcamento.Ativo)]
        [InlineData(StatusOrcamento.Inativo)]
        public async Task GetByStatusAsync_RepassaAmbienteEStatus(StatusOrcamento status)
        {
            // Arrange
            var orcamentos = new List<Orcamento> { CriarOrcamento() };
            _repository.OrcamentoRepository.GetByStatusAsync(_ambienteId, status).Returns(orcamentos);

            // Act
            var resultado = await _service.GetByStatusAsync(_ambienteId, status);

            // Assert
            Assert.Same(orcamentos, resultado);
        }

        [Fact]
        public async Task GetAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var orcamento = CriarOrcamento();
            var deOutroAmbiente = CriarOrcamento(_outroAmbienteId);
            _repository.OrcamentoRepository.ComDados(orcamento, deOutroAmbiente);

            // Act
            var encontrado = await _service.GetAsync(orcamento.Id, _ambienteId);
            var bloqueado = await _service.GetAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(orcamento, encontrado);
            Assert.Null(bloqueado);
        }

        [Fact]
        public async Task GetReadOnlyAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var orcamento = CriarOrcamento();
            var deOutroAmbiente = CriarOrcamento(_outroAmbienteId);
            _repository.OrcamentoRepository.ComDados(orcamento, deOutroAmbiente);

            // Act
            var encontrado = await _service.GetReadOnlyAsync(orcamento.Id, _ambienteId);
            var bloqueado = await _service.GetReadOnlyAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(orcamento, encontrado);
            Assert.Null(bloqueado);
        }
        #endregion

        #region AddAsync Tests
        [Fact]
        public async Task AddAsync_ComOrcamento_AdicionaESalva()
        {
            // Arrange
            var orcamento = CriarOrcamento();

            // Act
            var resultado = await _service.AddAsync(orcamento);

            // Assert
            Assert.Same(orcamento, resultado);
            _repository.OrcamentoRepository.Received(1).Add(orcamento);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_ComOrcamentoNulo_LancaArgumentNullException()
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
            await Assert.ThrowsAsync<Exception>(() => _service.AddAsync(CriarOrcamento()));
        }
        #endregion

        #region UpdateAsync Tests
        [Fact]
        public async Task UpdateAsync_OrcamentoExistente_CopiaCamposESalva()
        {
            // Arrange
            var existente = CriarOrcamento();
            _repository.OrcamentoRepository.ComDados(existente);
            var alterado = new Orcamento
            {
                Nome = "Lazer",
                Descricao = "Saídas",
                ValorLimite = 300,
                DataLimite = new DateTime(2026, 12, 31),
                StatusOrcamento = StatusOrcamento.Inativo,
                CategoriaId = Guid.NewGuid()
            };

            // Act
            var resultado = await _service.UpdateAsync(existente.Id, alterado, _ambienteId);

            // Assert
            Assert.Same(existente, resultado);
            Assert.Equal("Lazer", existente.Nome);
            Assert.Equal("Saídas", existente.Descricao);
            Assert.Equal(300, existente.ValorLimite);
            Assert.Equal(new DateTime(2026, 12, 31), existente.DataLimite);
            Assert.Equal(StatusOrcamento.Inativo, existente.StatusOrcamento);
            Assert.Equal(alterado.CategoriaId, existente.CategoriaId);
            Assert.Equal(_ambienteId, existente.AmbienteId);
            _repository.OrcamentoRepository.Received(1).Update(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_OrcamentoInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _repository.OrcamentoRepository.ComDados();

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(Guid.NewGuid(), CriarOrcamento(), _ambienteId));

            // Assert
            Assert.Equal("Orçamento não encontrado", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_OrcamentoDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var deOutroAmbiente = CriarOrcamento(_outroAmbienteId);
            _repository.OrcamentoRepository.ComDados(deOutroAmbiente);

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(deOutroAmbiente.Id, CriarOrcamento(), _ambienteId));
            _repository.OrcamentoRepository.DidNotReceive().Update(Arg.Any<Orcamento>());
        }

        [Fact]
        public async Task UpdateAsync_ComOrcamentoNulo_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(Guid.NewGuid(), null!, _ambienteId));
        }
        #endregion

        #region DeleteAsync Tests
        [Fact]
        public async Task DeleteAsync_OrcamentoExistente_DeletaERetornaTrue()
        {
            // Arrange
            var existente = CriarOrcamento();
            _repository.OrcamentoRepository.ComDados(existente);

            // Act
            var resultado = await _service.DeleteAsync(existente.Id, _ambienteId);

            // Assert
            Assert.True(resultado);
            _repository.OrcamentoRepository.Received(1).Delete(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_OrcamentoInexistente_RetornaFalseSemSalvar()
        {
            // Arrange
            _repository.OrcamentoRepository.ComDados();

            // Act
            var resultado = await _service.DeleteAsync(Guid.NewGuid(), _ambienteId);

            // Assert
            Assert.False(resultado);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_OrcamentoDeOutroAmbiente_RetornaFalse()
        {
            // Arrange
            var deOutroAmbiente = CriarOrcamento(_outroAmbienteId);
            _repository.OrcamentoRepository.ComDados(deOutroAmbiente);

            // Act
            var resultado = await _service.DeleteAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.False(resultado);
            _repository.OrcamentoRepository.DidNotReceive().Delete(Arg.Any<Orcamento>());
        }

        [Fact]
        public async Task DeleteAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            var existente = CriarOrcamento();
            _repository.OrcamentoRepository.ComDados(existente);
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(existente.Id, _ambienteId));
        }
        #endregion
    }
}
