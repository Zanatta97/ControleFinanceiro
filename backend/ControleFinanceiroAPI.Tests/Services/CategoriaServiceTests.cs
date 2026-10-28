using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ControleFinanceiroAPI.Tests.Services
{
    public class CategoriaServiceTests
    {
        private readonly IUnityOfWork _repository = Substitute.For<IUnityOfWork>();
        private readonly CategoriaService _service;
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly Guid _outroAmbienteId = Guid.NewGuid();

        public CategoriaServiceTests()
        {
            _service = new CategoriaService(_repository, Substitute.For<ILogger<CategoriaService>>());
        }

        private Categoria CriarCategoria(Guid? ambienteId = null)
        {
            return new Categoria
            {
                Id = Guid.NewGuid(),
                Nome = "Mercado",
                Cor = "#00ff00",
                UrlIcone = "mercado.png",
                AmbienteId = ambienteId ?? _ambienteId
            };
        }

        #region Consultas Tests
        [Fact]
        public async Task GetAllAsync_RetornaTodasDoRepositorio()
        {
            // Arrange
            var categorias = new List<Categoria> { CriarCategoria(), CriarCategoria(_outroAmbienteId) };
            _repository.CategoriaRepository.GetAllAsync().Returns(categorias);

            // Act
            var resultado = await _service.GetAllAsync();

            // Assert
            Assert.Same(categorias, resultado);
        }

        [Fact]
        public async Task GetAllByAmbienteAsync_RepassaAmbienteAoRepositorio()
        {
            // Arrange
            var categorias = new List<Categoria> { CriarCategoria() };
            _repository.CategoriaRepository.GetAllByAmbienteAsync(_ambienteId).Returns(categorias);

            // Act
            var resultado = await _service.GetAllByAmbienteAsync(_ambienteId);

            // Assert
            Assert.Same(categorias, resultado);
        }

        [Fact]
        public async Task GetAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var categoria = CriarCategoria();
            var deOutroAmbiente = CriarCategoria(_outroAmbienteId);
            _repository.CategoriaRepository.ComDados(categoria, deOutroAmbiente);

            // Act
            var encontrada = await _service.GetAsync(categoria.Id, _ambienteId);
            var bloqueada = await _service.GetAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(categoria, encontrada);
            Assert.Null(bloqueada);
        }

        [Fact]
        public async Task GetReadOnlyAsync_FiltraPorIdEAmbiente()
        {
            // Arrange
            var categoria = CriarCategoria();
            var deOutroAmbiente = CriarCategoria(_outroAmbienteId);
            _repository.CategoriaRepository.ComDados(categoria, deOutroAmbiente);

            // Act
            var encontrada = await _service.GetReadOnlyAsync(categoria.Id, _ambienteId);
            var bloqueada = await _service.GetReadOnlyAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.Same(categoria, encontrada);
            Assert.Null(bloqueada);
        }
        #endregion

        #region AddAsync Tests
        [Fact]
        public async Task AddAsync_ComCategoria_AdicionaESalva()
        {
            // Arrange
            var categoria = CriarCategoria();

            // Act
            var resultado = await _service.AddAsync(categoria);

            // Assert
            Assert.Same(categoria, resultado);
            _repository.CategoriaRepository.Received(1).Add(categoria);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task AddAsync_ComCategoriaNula_LancaArgumentNullException()
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
            await Assert.ThrowsAsync<Exception>(() => _service.AddAsync(CriarCategoria()));
        }
        #endregion

        #region UpdateAsync Tests
        [Fact]
        public async Task UpdateAsync_CategoriaExistente_CopiaCamposESalva()
        {
            // Arrange
            var existente = CriarCategoria();
            _repository.CategoriaRepository.ComDados(existente);
            var alterada = new Categoria { Nome = "Lazer", Cor = "#0000ff", UrlIcone = "lazer.png" };

            // Act
            var resultado = await _service.UpdateAsync(existente.Id, alterada, _ambienteId);

            // Assert
            Assert.Same(existente, resultado);
            Assert.Equal("Lazer", existente.Nome);
            Assert.Equal("#0000ff", existente.Cor);
            Assert.Equal("lazer.png", existente.UrlIcone);
            Assert.Equal(_ambienteId, existente.AmbienteId);
            _repository.CategoriaRepository.Received(1).Update(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_CategoriaInexistente_LancaKeyNotFoundException()
        {
            // Arrange
            _repository.CategoriaRepository.ComDados();

            // Act
            var excecao = await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(Guid.NewGuid(), CriarCategoria(), _ambienteId));

            // Assert
            Assert.Equal("Categoria não encontrada", excecao.Message);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task UpdateAsync_CategoriaDeOutroAmbiente_LancaKeyNotFoundException()
        {
            // Arrange
            var deOutroAmbiente = CriarCategoria(_outroAmbienteId);
            _repository.CategoriaRepository.ComDados(deOutroAmbiente);

            // Act / Assert
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                _service.UpdateAsync(deOutroAmbiente.Id, CriarCategoria(), _ambienteId));
            _repository.CategoriaRepository.DidNotReceive().Update(Arg.Any<Categoria>());
        }

        [Fact]
        public async Task UpdateAsync_ComCategoriaNula_LancaArgumentNullException()
        {
            // Act / Assert
            await Assert.ThrowsAsync<ArgumentNullException>(() => _service.UpdateAsync(Guid.NewGuid(), null!, _ambienteId));
        }
        #endregion

        #region DeleteAsync Tests
        [Fact]
        public async Task DeleteAsync_CategoriaExistente_DeletaERetornaTrue()
        {
            // Arrange
            var existente = CriarCategoria();
            _repository.CategoriaRepository.ComDados(existente);

            // Act
            var resultado = await _service.DeleteAsync(existente.Id, _ambienteId);

            // Assert
            Assert.True(resultado);
            _repository.CategoriaRepository.Received(1).Delete(existente);
            await _repository.Received(1).SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_CategoriaInexistente_RetornaFalseSemSalvar()
        {
            // Arrange
            _repository.CategoriaRepository.ComDados();

            // Act
            var resultado = await _service.DeleteAsync(Guid.NewGuid(), _ambienteId);

            // Assert
            Assert.False(resultado);
            await _repository.DidNotReceive().SaveChangesAsync();
        }

        [Fact]
        public async Task DeleteAsync_CategoriaDeOutroAmbiente_RetornaFalse()
        {
            // Arrange
            var deOutroAmbiente = CriarCategoria(_outroAmbienteId);
            _repository.CategoriaRepository.ComDados(deOutroAmbiente);

            // Act
            var resultado = await _service.DeleteAsync(deOutroAmbiente.Id, _ambienteId);

            // Assert
            Assert.False(resultado);
            _repository.CategoriaRepository.DidNotReceive().Delete(Arg.Any<Categoria>());
        }

        [Fact]
        public async Task DeleteAsync_FalhaAoSalvar_PropagaExcecao()
        {
            // Arrange
            var existente = CriarCategoria();
            _repository.CategoriaRepository.ComDados(existente);
            _repository.SaveChangesAsync().ThrowsAsync(new Exception("falha no banco"));

            // Act / Assert
            await Assert.ThrowsAsync<Exception>(() => _service.DeleteAsync(existente.Id, _ambienteId));
        }
        #endregion
    }
}
