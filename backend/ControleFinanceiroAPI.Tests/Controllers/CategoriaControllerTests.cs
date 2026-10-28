using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Categoria;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class CategoriaControllerTests
    {
        private readonly ICategoriaService _service = Substitute.For<ICategoriaService>();
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly CategoriaController _controller;

        public CategoriaControllerTests()
        {
            _controller = new CategoriaController(_service).ComUsuario(_ambienteId);
        }

        private static Categoria CriarCategoria(string nome = "Mercado")
        {
            return new Categoria { Id = Guid.NewGuid(), Nome = nome, Cor = "#fff" };
        }

        private static CategoriaRequestDTO CriarRequest()
        {
            return new CategoriaRequestDTO { Nome = "Lazer", Cor = "#000", UrlIcone = "lazer.png" };
        }

        #region GetAll Tests
        [Fact]
        public async Task GetAll_SemCategorias_Retorna404()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Categoria>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<CategoriaResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Nenhuma categoria encontrada.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetAll_ComCategorias_Retorna200()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Categoria> { CriarCategoria("A"), CriarCategoria("B") });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<CategoriaResponseDTO>>(await _controller.GetAll());

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
            _service.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Categoria> { CriarCategoria() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<CategoriaResponseDTO>>(await _controller.GetAllByAmbiente());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
        }

        [Fact]
        public async Task GetAllByAmbiente_SemAmbienteSelecionado_LancaUnauthorizedAccessException()
        {
            // Arrange
            var controller = new CategoriaController(_service).ComUsuario(ambienteId: null);

            // Act / Assert
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetAllByAmbiente());
        }
        #endregion

        #region GetById Tests
        [Fact]
        public async Task GetById_Existente_Retorna200()
        {
            // Arrange
            var categoria = CriarCategoria();
            _service.GetAsync(categoria.Id, _ambienteId).Returns(categoria);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<CategoriaResponseDTO>(await _controller.GetById(categoria.Id));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(categoria.Id, corpo.Dados!.Id);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            // Arrange
            _service.GetAsync(Arg.Any<Guid>(), _ambienteId).Returns((Categoria?)null);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<CategoriaResponseDTO>(await _controller.GetById(Guid.NewGuid()));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Categoria não encontrada.", corpo.ErrorMessage);
        }
        #endregion

        #region Create Tests
        [Fact]
        public async Task Create_MapeiaComAmbienteEUsuarioERetorna201()
        {
            // Arrange
            _service.AddAsync(Arg.Any<Categoria>()).Returns(ci => ci.Arg<Categoria>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<CategoriaResponseDTO>(await _controller.Create(CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status201Created, status);
            Assert.Equal("Lazer", corpo.Dados!.Nome);
            await _service.Received(1).AddAsync(Arg.Is<Categoria>(c =>
                c.AmbienteId == _ambienteId && c.UsuarioId == ControllerTestHelper.UsuarioId));
        }
        #endregion

        #region Update Tests
        [Fact]
        public async Task Update_RepassaIdEAmbienteERetorna200()
        {
            // Arrange
            var id = Guid.NewGuid();
            _service.UpdateAsync(id, Arg.Any<Categoria>(), _ambienteId).Returns(CriarCategoria("Atualizada"));

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<CategoriaResponseDTO>(await _controller.Update(id, CriarRequest()));

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
            Assert.Equal("Categoria excluída com sucesso.", corpo.StatusMessage);
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
            Assert.Equal("Categoria não encontrada.", corpo.ErrorMessage);
        }
        #endregion
    }
}
