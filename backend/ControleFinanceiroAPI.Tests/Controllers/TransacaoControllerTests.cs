using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Transacao;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace ControleFinanceiroAPI.Tests.Controllers
{
    public class TransacaoControllerTests
    {
        private readonly ITransacaoService _service = Substitute.For<ITransacaoService>();
        private readonly Guid _ambienteId = Guid.NewGuid();
        private readonly TransacaoController _controller;

        public TransacaoControllerTests()
        {
            _controller = new TransacaoController(_service).ComUsuario(_ambienteId);
        }

        private static Transacao CriarTransacao(string descricao = "Mercado")
        {
            return new Transacao { Id = Guid.NewGuid(), Descricao = descricao, Valor = 100, TipoTransacao = TipoTransacao.Despesa };
        }

        private static TransacaoRequestDTO CriarRequest(int parcelas = 1)
        {
            return new TransacaoRequestDTO
            {
                Descricao = "Notebook",
                Valor = 500,
                Data = new DateTime(2026, 9, 15),
                TipoTransacao = TipoTransacao.Despesa,
                CategoriaId = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                MesCompetencia = new DateOnly(2026, 9, 1),
                Parcelas = parcelas
            };
        }

        #region GetAll Tests
        [Fact]
        public async Task GetAll_SemTransacoes_Retorna404()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Transacao>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.False(corpo.Success);
            Assert.Equal("Nenhuma transação encontrada.", corpo.ErrorMessage);
        }

        [Fact]
        public async Task GetAll_ComTransacoes_Retorna200ComDTOs()
        {
            // Arrange
            _service.GetAllAsync().Returns(new List<Transacao> { CriarTransacao("A"), CriarTransacao("B") });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.GetAll());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.True(corpo.Success);
            Assert.Equal(["A", "B"], corpo.Dados!.Select(t => t.Descricao));
        }
        #endregion

        #region Consultas do ambiente Tests
        [Fact]
        public async Task GetAllByAmbiente_UsaAmbienteDoToken()
        {
            // Arrange
            _service.GetAllByAmbienteAsync(_ambienteId).Returns(new List<Transacao> { CriarTransacao() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.GetAllByAmbiente());

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
        }

        [Fact]
        public async Task GetAllByAmbiente_SemAmbienteSelecionado_LancaUnauthorizedAccessException()
        {
            // Arrange — usuário logado sem ter selecionado ambiente
            var controller = new TransacaoController(_service).ComUsuario(ambienteId: null);

            // Act / Assert — o middleware converte em 403
            await Assert.ThrowsAsync<UnauthorizedAccessException>(() => controller.GetAllByAmbiente());
            await _service.DidNotReceive().GetAllByAmbienteAsync(Arg.Any<Guid>());
        }

        [Fact]
        public async Task GetByConta_RepassaContaEAmbienteAoServico()
        {
            // Arrange
            var contaId = Guid.NewGuid();
            _service.GetByContaAsync(contaId, _ambienteId).Returns(new List<Transacao> { CriarTransacao() });

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.GetByConta(contaId));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Single(corpo.Dados!);
            await _service.Received(1).GetByContaAsync(contaId, _ambienteId);
        }

        [Fact]
        public async Task GetByPeriodo_RepassaPeriodoEAmbiente()
        {
            // Arrange
            var inicio = new DateTime(2026, 9, 1);
            var fim = new DateTime(2026, 9, 30);
            _service.GetByPeriodoAsync(_ambienteId, inicio, fim).Returns(new List<Transacao>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.GetByPeriodo(inicio, fim));

            // Assert — lista vazia é 200, não 404
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Empty(corpo.Dados!);
            await _service.Received(1).GetByPeriodoAsync(_ambienteId, inicio, fim);
        }

        [Fact]
        public async Task GetByTipo_RepassaTipoEAmbiente()
        {
            // Arrange
            _service.GetByTipoAsync(_ambienteId, TipoTransacao.Receita).Returns(new List<Transacao> { CriarTransacao() });

            // Act
            var (status, _) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.GetByTipo(TipoTransacao.Receita));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            await _service.Received(1).GetByTipoAsync(_ambienteId, TipoTransacao.Receita);
        }
        #endregion

        #region GetById Tests
        [Fact]
        public async Task GetById_Existente_Retorna200()
        {
            // Arrange
            var transacao = CriarTransacao();
            _service.GetAsync(transacao.Id, _ambienteId).Returns(transacao);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<TransacaoResponseDTO>(await _controller.GetById(transacao.Id));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal(transacao.Id, corpo.Dados!.Id);
        }

        [Fact]
        public async Task GetById_Inexistente_Retorna404()
        {
            // Arrange
            _service.GetAsync(Arg.Any<Guid>(), _ambienteId).Returns((Transacao?)null);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<TransacaoResponseDTO>(await _controller.GetById(Guid.NewGuid()));

            // Assert
            Assert.Equal(StatusCodes.Status404NotFound, status);
            Assert.Equal("Transação não encontrada.", corpo.ErrorMessage);
        }
        #endregion

        #region Create Tests
        [Fact]
        public async Task Create_AVista_ChamaAddAsyncERetorna201ComUmItem()
        {
            // Arrange
            _service.AddAsync(Arg.Any<Transacao>()).Returns(ci => ci.Arg<Transacao>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.Create(CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status201Created, status);
            Assert.Equal(StatusCodes.Status201Created, corpo.StatusCode);
            Assert.Single(corpo.Dados!);
            await _service.Received(1).AddAsync(Arg.Is<Transacao>(t =>
                t.AmbienteId == _ambienteId && t.UsuarioId == ControllerTestHelper.UsuarioId && t.Observacao == null));
            await _service.DidNotReceive().AddManyAsync(Arg.Any<IEnumerable<Transacao>>());
        }

        [Fact]
        public async Task Create_ComParcelasZero_TrataComoAVista()
        {
            // Arrange
            _service.AddAsync(Arg.Any<Transacao>()).Returns(ci => ci.Arg<Transacao>());

            // Act
            await _controller.Create(CriarRequest(parcelas: 0));

            // Assert
            await _service.Received(1).AddAsync(Arg.Any<Transacao>());
        }

        [Fact]
        public async Task Create_Parcelado_GeraUmaTransacaoPorParcelaERetorna201()
        {
            // Arrange
            List<Transacao>? enviadas = null;
            _service.AddManyAsync(Arg.Do<IEnumerable<Transacao>>(t => enviadas = t.ToList()))
                .Returns(ci => ci.Arg<IEnumerable<Transacao>>());

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<IEnumerable<TransacaoResponseDTO>>(await _controller.Create(CriarRequest(parcelas: 3)));

            // Assert
            Assert.Equal(StatusCodes.Status201Created, status);
            Assert.Equal(3, corpo.Dados!.Count());
            Assert.NotNull(enviadas);
            Assert.Equal(["Parcela 1/3", "Parcela 2/3", "Parcela 3/3"], enviadas.Select(t => t.Observacao));
            Assert.Equal([new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 1), new DateOnly(2026, 11, 1)], enviadas.Select(t => t.MesCompetencia));
            Assert.All(enviadas, t => Assert.Equal(_ambienteId, t.AmbienteId));
            await _service.DidNotReceive().AddAsync(Arg.Any<Transacao>());
        }
        #endregion

        #region Update Tests
        [Fact]
        public async Task Update_RepassaIdEAmbienteERetorna200()
        {
            // Arrange
            var id = Guid.NewGuid();
            var atualizada = CriarTransacao("Atualizada");
            _service.UpdateAsync(id, Arg.Any<Transacao>(), _ambienteId).Returns(atualizada);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<TransacaoResponseDTO>(await _controller.Update(id, CriarRequest()));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Atualizada", corpo.Dados!.Descricao);
        }

        [Fact]
        public async Task Update_TransacaoInexistente_PropagaKeyNotFoundException()
        {
            // Arrange
            _service.UpdateAsync(Arg.Any<Guid>(), Arg.Any<Transacao>(), _ambienteId)
                .ThrowsAsync(new KeyNotFoundException("Transação não encontrada"));

            // Act / Assert — o ErrorHandlingMiddleware devolve 404
            await Assert.ThrowsAsync<KeyNotFoundException>(() => _controller.Update(Guid.NewGuid(), CriarRequest()));
        }
        #endregion

        #region Delete Tests
        [Fact]
        public async Task Delete_Existente_Retorna200ComMensagem()
        {
            // Arrange
            var id = Guid.NewGuid();
            _service.DeleteAsync(id, _ambienteId).Returns(true);

            // Act
            var (status, corpo) = ControllerTestHelper.LerResposta<object>(await _controller.Delete(id));

            // Assert
            Assert.Equal(StatusCodes.Status200OK, status);
            Assert.Equal("Transação excluída com sucesso.", corpo.StatusMessage);
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
            Assert.Equal("Transação não encontrada.", corpo.ErrorMessage);
        }
        #endregion
    }
}
