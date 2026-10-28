using ControleFinanceiroAPI.DTO.Ambiente;
using ControleFinanceiroAPI.DTO.Categoria;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Conta;
using ControleFinanceiroAPI.DTO.Orcamento;
using ControleFinanceiroAPI.DTO.Transacao;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Tests.DTO
{
    public class DTOMapperTests
    {
        private const string UsuarioId = "usuario-1";
        private readonly Guid _ambienteId = Guid.NewGuid();

        #region Usuario Tests
        [Fact]
        public void ToResumoDTO_ComUsuario_MapeiaIdNomeEEmail()
        {
            // Arrange
            var usuario = new Usuario { Id = UsuarioId, Nome = "Fulano", Email = "fulano@teste.com" };

            // Act
            var dto = usuario.ToResumoDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(UsuarioId, dto.Id);
            Assert.Equal("Fulano", dto.Nome);
            Assert.Equal("fulano@teste.com", dto.Email);
        }

        [Fact]
        public void ToResumoDTO_ComUsuarioNulo_RetornaNulo()
        {
            // Act
            var dto = ((Usuario)null!).ToResumoDTO();

            // Assert
            Assert.Null(dto);
        }

        [Fact]
        public void ToResponseDTO_ComUsuario_MapeiaCamposERefreshTokenNoToken()
        {
            // Arrange
            var criacao = new DateTime(2026, 1, 10);
            var usuario = new Usuario
            {
                Id = UsuarioId,
                Nome = "Fulano",
                Email = "fulano@teste.com",
                DtaCriacao = criacao,
                RefreshToken = "refresh"
            };

            // Act
            var dto = usuario.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(UsuarioId, dto.Id);
            Assert.Equal("Fulano", dto.Nome);
            Assert.Equal("fulano@teste.com", dto.Email);
            Assert.Equal(criacao, dto.DtaCriacao);
            Assert.Equal("refresh", dto.Token);
        }

        [Fact]
        public void ToResponseDTO_ComUsuarioNulo_RetornaNulo()
        {
            // Act / Assert
            Assert.Null(((Usuario)null!).ToResponseDTO());
        }
        #endregion

        #region Ambiente Tests
        [Fact]
        public void ToEntity_ComAmbienteRequest_MapeiaNome()
        {
            // Arrange
            var dto = new AmbienteRequestDTO { Nome = "Família" };

            // Act
            var entidade = dto.ToEntity();

            // Assert
            Assert.NotNull(entidade);
            Assert.Equal("Família", entidade.Nome);
        }

        [Fact]
        public void ToEntity_ComAmbienteRequestNulo_RetornaNulo()
        {
            // Act / Assert
            Assert.Null(((AmbienteRequestDTO)null!).ToEntity());
        }

        [Fact]
        public void ToResponseDTO_ComAmbienteEMembros_MapeiaMembrosComUsuario()
        {
            // Arrange
            var ambiente = new Ambiente
            {
                Id = _ambienteId,
                Nome = "Família",
                DataCriacao = new DateTime(2026, 2, 1),
                Membros =
                [
                    new AmbienteMembro { UsuarioId = UsuarioId, Role = "Dono", Usuario = new Usuario { Id = UsuarioId, Nome = "Fulano" } },
                    new AmbienteMembro { UsuarioId = "usuario-2", Role = "Membro" } // sem navegação carregada
                ]
            };

            // Act
            var dto = ambiente.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(_ambienteId, dto.Id);
            Assert.Equal("Família", dto.Nome);
            Assert.Equal(new DateTime(2026, 2, 1), dto.DataCriacao);
            Assert.Equal(2, dto.Membros.Count);
            Assert.Equal("Dono", dto.Membros[0].Role);
            Assert.Equal("Fulano", dto.Membros[0].Usuario!.Nome);
            Assert.Equal("Membro", dto.Membros[1].Role);
            Assert.Null(dto.Membros[1].Usuario);
        }

        [Fact]
        public void ToResponseDTO_ComAmbienteSemListaDeMembros_RetornaListaVazia()
        {
            // Arrange
            var ambiente = new Ambiente { Id = _ambienteId, Membros = null! };

            // Act
            var dto = ambiente.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Empty(dto.Membros);
        }

        [Fact]
        public void ToResponseDTO_ComAmbienteMembroNulo_RetornaNulo()
        {
            // Act / Assert
            Assert.Null(((AmbienteMembro)null!).ToResponseDTO());
            Assert.Null(((Ambiente)null!).ToResponseDTO());
        }

        [Fact]
        public void ToDTOList_ComAmbientes_MapeiaTodos()
        {
            // Arrange
            var ambientes = new List<Ambiente> { new() { Nome = "A" }, new() { Nome = "B" } };

            // Act
            var dtos = ambientes.ToDTOList().ToList();

            // Assert
            Assert.Equal(["A", "B"], dtos.Select(d => d.Nome));
        }

        [Fact]
        public void ToDTOList_ComAmbientesVazioOuNulo_RetornaListaVazia()
        {
            // Act / Assert
            Assert.Empty(new List<Ambiente>().ToDTOList());
            Assert.Empty(((IEnumerable<Ambiente>)null!).ToDTOList());
        }
        #endregion

        #region Categoria Tests
        [Fact]
        public void ToEntity_ComCategoriaRequest_MapeiaCamposAmbienteEUsuario()
        {
            // Arrange
            var dto = new CategoriaRequestDTO { Nome = "Mercado", Cor = "#ff0000", UrlIcone = "icone.png" };

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId);

            // Assert
            Assert.NotNull(entidade);
            Assert.Equal("Mercado", entidade.Nome);
            Assert.Equal("#ff0000", entidade.Cor);
            Assert.Equal("icone.png", entidade.UrlIcone);
            Assert.Equal(_ambienteId, entidade.AmbienteId);
            Assert.Equal(UsuarioId, entidade.UsuarioId);
        }

        [Fact]
        public void ToResponseDTO_ComCategoria_MapeiaCampos()
        {
            // Arrange
            var categoria = new Categoria { Id = Guid.NewGuid(), Nome = "Mercado", Cor = "#ff0000", UrlIcone = "icone.png", UsuarioId = UsuarioId };

            // Act
            var dto = categoria.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(categoria.Id, dto.Id);
            Assert.Equal("Mercado", dto.Nome);
            Assert.Equal("#ff0000", dto.Cor);
            Assert.Equal("icone.png", dto.UrlIcone);
            Assert.Equal(UsuarioId, dto.UsuarioId);
        }

        [Fact]
        public void Categoria_ComNulos_RetornaNuloOuListaVazia()
        {
            // Act / Assert
            Assert.Null(((CategoriaRequestDTO)null!).ToEntity(_ambienteId, UsuarioId));
            Assert.Null(((Categoria)null!).ToResponseDTO());
            Assert.Empty(((IEnumerable<Categoria>)null!).ToDTOList());
            Assert.Empty(new List<Categoria>().ToDTOList());
        }

        [Fact]
        public void ToDTOList_ComCategorias_MapeiaTodas()
        {
            // Arrange
            var categorias = new List<Categoria> { new() { Nome = "A" }, new() { Nome = "B" } };

            // Act / Assert
            Assert.Equal(["A", "B"], categorias.ToDTOList().Select(c => c.Nome));
        }
        #endregion

        #region Conta Tests
        [Fact]
        public void ToEntity_ComContaRequest_MapeiaCamposAmbienteEUsuario()
        {
            // Arrange
            var dto = new ContaRequestDTO { Nome = "Nubank", TipoConta = TipoConta.CartaoCredito, Saldo = 150.5m };

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId);

            // Assert
            Assert.NotNull(entidade);
            Assert.Equal("Nubank", entidade.Nome);
            Assert.Equal(TipoConta.CartaoCredito, entidade.TipoConta);
            Assert.Equal(150.5m, entidade.Saldo);
            Assert.Equal(_ambienteId, entidade.AmbienteId);
            Assert.Equal(UsuarioId, entidade.UsuarioId);
        }

        [Fact]
        public void ToResponseDTO_ComConta_MapeiaCampos()
        {
            // Arrange
            var conta = new Conta { Id = Guid.NewGuid(), Nome = "Itaú", TipoConta = TipoConta.Corrente, Saldo = 10, UsuarioId = UsuarioId };

            // Act
            var dto = conta.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(conta.Id, dto.Id);
            Assert.Equal("Itaú", dto.Nome);
            Assert.Equal(TipoConta.Corrente, dto.TipoConta);
            Assert.Equal(10, dto.Saldo);
            Assert.Equal(UsuarioId, dto.UsuarioId);
        }

        [Fact]
        public void Conta_ComNulos_RetornaNuloOuListaVazia()
        {
            // Act / Assert
            Assert.Null(((ContaRequestDTO)null!).ToEntity(_ambienteId, UsuarioId));
            Assert.Null(((Conta)null!).ToResponseDTO());
            Assert.Empty(((IEnumerable<Conta>)null!).ToDTOList());
            Assert.Empty(new List<Conta>().ToDTOList());
        }

        [Fact]
        public void ToDTOList_ComContas_MapeiaTodas()
        {
            // Arrange
            var contas = new List<Conta> { new() { Nome = "A" }, new() { Nome = "B" } };

            // Act / Assert
            Assert.Equal(["A", "B"], contas.ToDTOList().Select(c => c.Nome));
        }
        #endregion

        #region Orcamento Tests
        [Fact]
        public void ToEntity_ComOrcamentoRequest_MapeiaCamposAmbienteEUsuario()
        {
            // Arrange
            var categoriaId = Guid.NewGuid();
            var dto = new OrcamentoRequestDTO
            {
                Nome = "Mercado do mês",
                Descricao = "Limite de mercado",
                ValorLimite = 800,
                DataLimite = new DateTime(2026, 9, 30),
                StatusOrcamento = StatusOrcamento.Ativo,
                CategoriaId = categoriaId
            };

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId);

            // Assert
            Assert.NotNull(entidade);
            Assert.Equal("Mercado do mês", entidade.Nome);
            Assert.Equal("Limite de mercado", entidade.Descricao);
            Assert.Equal(800, entidade.ValorLimite);
            Assert.Equal(new DateTime(2026, 9, 30), entidade.DataLimite);
            Assert.Equal(StatusOrcamento.Ativo, entidade.StatusOrcamento);
            Assert.Equal(categoriaId, entidade.CategoriaId);
            Assert.Equal(_ambienteId, entidade.AmbienteId);
            Assert.Equal(UsuarioId, entidade.UsuarioId);
        }

        [Fact]
        public void ToResponseDTO_ComOrcamento_MapeiaCampos()
        {
            // Arrange
            var orcamento = new Orcamento
            {
                Id = Guid.NewGuid(),
                Nome = "Lazer",
                Descricao = "Saídas",
                ValorLimite = 300,
                DataLimite = new DateTime(2026, 12, 31),
                StatusOrcamento = StatusOrcamento.Inativo,
                CategoriaId = Guid.NewGuid(),
                UsuarioId = UsuarioId
            };

            // Act
            var dto = orcamento.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(orcamento.Id, dto.Id);
            Assert.Equal("Lazer", dto.Nome);
            Assert.Equal("Saídas", dto.Descricao);
            Assert.Equal(300, dto.ValorLimite);
            Assert.Equal(orcamento.DataLimite, dto.DataLimite);
            Assert.Equal(StatusOrcamento.Inativo, dto.StatusOrcamento);
            Assert.Equal(orcamento.CategoriaId, dto.CategoriaId);
            Assert.Equal(UsuarioId, dto.UsuarioId);
        }

        [Fact]
        public void Orcamento_ComNulos_RetornaNuloOuListaVazia()
        {
            // Act / Assert
            Assert.Null(((OrcamentoRequestDTO)null!).ToEntity(_ambienteId, UsuarioId));
            Assert.Null(((Orcamento)null!).ToResponseDTO());
            Assert.Empty(((IEnumerable<Orcamento>)null!).ToDTOList());
            Assert.Empty(new List<Orcamento>().ToDTOList());
        }

        [Fact]
        public void ToDTOList_ComOrcamentos_MapeiaTodos()
        {
            // Arrange
            var orcamentos = new List<Orcamento> { new() { Nome = "A" }, new() { Nome = "B" } };

            // Act / Assert
            Assert.Equal(["A", "B"], orcamentos.ToDTOList().Select(o => o.Nome));
        }
        #endregion

        #region Transacao Tests
        private static TransacaoRequestDTO CriarTransacaoRequest(int parcelas = 1, string? observacao = null)
        {
            return new TransacaoRequestDTO
            {
                Descricao = "Notebook",
                Valor = 500,
                Data = new DateTime(2026, 9, 15),
                Observacao = observacao,
                TipoTransacao = TipoTransacao.Despesa,
                CategoriaId = Guid.NewGuid(),
                ContaId = Guid.NewGuid(),
                ContaDestinoId = null,
                MesCompetencia = new DateOnly(2026, 10, 1),
                Parcelas = parcelas
            };
        }

        [Fact]
        public void ToEntity_ComTransacaoAVista_MapeiaCamposSemAlterarObservacaoNemCompetencia()
        {
            // Arrange
            var dto = CriarTransacaoRequest(observacao: "Compra à vista");

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId);

            // Assert
            Assert.NotNull(entidade);
            Assert.Equal("Notebook", entidade.Descricao);
            Assert.Equal(500, entidade.Valor);
            Assert.Equal(dto.Data, entidade.Data);
            Assert.Equal("Compra à vista", entidade.Observacao);
            Assert.Equal(TipoTransacao.Despesa, entidade.TipoTransacao);
            Assert.Equal(dto.CategoriaId, entidade.CategoriaId);
            Assert.Equal(dto.ContaId, entidade.ContaId);
            Assert.Null(entidade.ContaDestinoId);
            Assert.Equal(_ambienteId, entidade.AmbienteId);
            Assert.Equal(UsuarioId, entidade.UsuarioId);
            Assert.Equal(new DateOnly(2026, 10, 1), entidade.MesCompetencia);
        }

        [Fact]
        public void ToEntity_ComTransferencia_MapeiaContaDestino()
        {
            // Arrange
            var dto = CriarTransacaoRequest();
            dto.TipoTransacao = TipoTransacao.Transferencia;
            dto.ContaDestinoId = Guid.NewGuid();

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId);

            // Assert
            Assert.Equal(dto.ContaDestinoId, entidade!.ContaDestinoId);
        }

        [Theory]
        [InlineData(0, "Parcela 1/3", 2026, 10)]
        [InlineData(1, "Parcela 2/3", 2026, 11)]
        [InlineData(2, "Parcela 3/3", 2026, 12)]
        public void ToEntity_ComParcelaSemObservacao_GravaPrefixoEAvancaCompetencia(int parcelaIndex, string observacaoEsperada,
                                                                                     int anoEsperado, int mesEsperado)
        {
            // Arrange
            var dto = CriarTransacaoRequest(parcelas: 3);

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId, parcelaIndex);

            // Assert
            Assert.Equal(observacaoEsperada, entidade!.Observacao);
            Assert.Equal(new DateOnly(anoEsperado, mesEsperado, 1), entidade.MesCompetencia);
            Assert.Equal(500, entidade.Valor); // valor da parcela é o informado, sem dividir
        }

        [Fact]
        public void ToEntity_ComParcelaEObservacao_ConcatenaPrefixoComObservacaoOriginal()
        {
            // Arrange
            var dto = CriarTransacaoRequest(parcelas: 3, observacao: "Loja X");

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId, 1);

            // Assert
            Assert.Equal("Parcela 2/3 — Loja X", entidade!.Observacao);
        }

        [Fact]
        public void ToEntity_ComParcelaEObservacaoEmBranco_GravaSoOPrefixo()
        {
            // Arrange
            var dto = CriarTransacaoRequest(parcelas: 2, observacao: "   ");

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId, 0);

            // Assert
            Assert.Equal("Parcela 1/2", entidade!.Observacao);
        }

        [Fact]
        public void ToEntity_ComParcelasQueViramOAno_AvancaCompetenciaParaAnoSeguinte()
        {
            // Arrange — competência inicial 11/2026, terceira parcela cai em 01/2027
            var dto = CriarTransacaoRequest(parcelas: 3);
            dto.MesCompetencia = new DateOnly(2026, 11, 1);

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId, 2);

            // Assert
            Assert.Equal(new DateOnly(2027, 1, 1), entidade!.MesCompetencia);
            Assert.Equal("Parcela 3/3", entidade.Observacao);
        }

        [Fact]
        public void ToEntity_ParcelaGerada_EReconhecidaPelaTransacaoExtension()
        {
            // Arrange — o formato gravado pelo mapper precisa ser o que os relatórios reconhecem
            var dto = CriarTransacaoRequest(parcelas: 12, observacao: "Geladeira");

            // Act
            var entidade = dto.ToEntity(_ambienteId, UsuarioId, 4);
            var reconhecida = ControleFinanceiroAPI.Common.Extensions.TransacaoExtension.TryGetParcela(entidade!, out var numero, out var total);

            // Assert
            Assert.True(reconhecida);
            Assert.Equal(5, numero);
            Assert.Equal(12, total);
        }

        [Fact]
        public void ToResponseDTO_ComTransacaoENavegacoes_MapeiaNomes()
        {
            // Arrange
            var transacao = new Transacao
            {
                Id = Guid.NewGuid(),
                Descricao = "Pagamento fatura",
                Valor = 700,
                Data = new DateTime(2026, 9, 10),
                Observacao = "obs",
                TipoTransacao = TipoTransacao.Transferencia,
                CategoriaId = Guid.NewGuid(),
                Categoria = new Categoria { Nome = "Cartão" },
                ContaId = Guid.NewGuid(),
                Conta = new Conta { Nome = "Corrente" },
                ContaDestinoId = Guid.NewGuid(),
                ContaDestino = new Conta { Nome = "Nubank" },
                UsuarioId = UsuarioId,
                MesCompetencia = new DateOnly(2026, 9, 1)
            };

            // Act
            var dto = transacao.ToResponseDTO();

            // Assert
            Assert.NotNull(dto);
            Assert.Equal(transacao.Id, dto.Id);
            Assert.Equal("Pagamento fatura", dto.Descricao);
            Assert.Equal(700, dto.Valor);
            Assert.Equal(transacao.Data, dto.Data);
            Assert.Equal("obs", dto.Observacao);
            Assert.Equal(TipoTransacao.Transferencia, dto.TipoTransacao);
            Assert.Equal(transacao.CategoriaId, dto.CategoriaId);
            Assert.Equal("Cartão", dto.CategoriaNome);
            Assert.Equal(transacao.ContaId, dto.ContaId);
            Assert.Equal("Corrente", dto.ContaNome);
            Assert.Equal(transacao.ContaDestinoId, dto.ContaDestinoId);
            Assert.Equal("Nubank", dto.ContaDestinoNome);
            Assert.Equal(UsuarioId, dto.UsuarioId);
            Assert.Equal(transacao.MesCompetencia, dto.MesCompetencia);
        }

        [Fact]
        public void ToResponseDTO_ComTransacaoSemNavegacoes_DeixaNomesNulos()
        {
            // Arrange
            var transacao = new Transacao { Id = Guid.NewGuid() };

            // Act
            var dto = transacao.ToResponseDTO();

            // Assert
            Assert.Null(dto!.CategoriaNome);
            Assert.Null(dto.ContaNome);
            Assert.Null(dto.ContaDestinoNome);
        }

        [Fact]
        public void Transacao_ComNulos_RetornaNuloOuListaVazia()
        {
            // Act / Assert
            Assert.Null(((TransacaoRequestDTO)null!).ToEntity(_ambienteId, UsuarioId));
            Assert.Null(((Transacao)null!).ToResponseDTO());
            Assert.Empty(((IEnumerable<Transacao>)null!).ToDTOList());
            Assert.Empty(new List<Transacao>().ToDTOList());
        }

        [Fact]
        public void ToDTOList_ComTransacoes_MapeiaTodas()
        {
            // Arrange
            var transacoes = new List<Transacao> { new() { Descricao = "A" }, new() { Descricao = "B" } };

            // Act / Assert
            Assert.Equal(["A", "B"], transacoes.ToDTOList().Select(t => t.Descricao));
        }
        #endregion
    }
}
