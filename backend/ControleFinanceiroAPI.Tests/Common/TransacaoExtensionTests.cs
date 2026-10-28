using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Tests.Common
{
    public class TransacaoExtensionTests
    {
        #region TryGetParcela Tests
        [Theory]
        [InlineData("Parcela 1/3", 1, 3)]
        [InlineData("Parcela 3/3", 3, 3)]
        [InlineData("Parcela 1/1", 1, 1)]
        [InlineData("Parcela 10/12", 10, 12)]
        [InlineData("Parcela 2/12 — Notebook da loja", 2, 12)]
        [InlineData("Parcela 7/10qualquer coisa", 7, 10)]
        public void TryGetParcela_ComObservacaoValida_RetornaNumeroETotal(string observacao, int numeroEsperado, int totalEsperado)
        {
            // Arrange
            var transacao = new Transacao { Observacao = observacao };

            // Act
            var reconhecida = transacao.TryGetParcela(out var numero, out var total);

            // Assert
            Assert.True(reconhecida);
            Assert.Equal(numeroEsperado, numero);
            Assert.Equal(totalEsperado, total);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("Compra no mercado")]
        [InlineData("Parcela 0/3")]            // N abaixo de 1
        [InlineData("Parcela 1/0")]            // M abaixo de 1
        [InlineData("Parcela 4/3")]            // N > M
        [InlineData("parcela 1/3")]            // prefixo é sensível a maiúsculas
        [InlineData("Compra Parcela 1/3")]     // prefixo precisa estar no início
        [InlineData(" Parcela 1/3")]
        [InlineData("Parcela 1 / 3")]
        [InlineData("Parcela a/3")]
        [InlineData("Parcela 1/")]
        [InlineData("Parcela 99999999999/99999999999")] // estoura int
        public void TryGetParcela_ComObservacaoInvalida_RetornaFalseEZeros(string? observacao)
        {
            // Arrange
            var transacao = new Transacao { Observacao = observacao };

            // Act
            var reconhecida = transacao.TryGetParcela(out var numero, out var total);

            // Assert
            Assert.False(reconhecida);
            Assert.Equal(0, numero);
            Assert.Equal(0, total);
        }
        #endregion

        #region IsParcela Tests
        [Theory]
        [InlineData("Parcela 2/5", true)]
        [InlineData("Parcela 6/5", false)]
        [InlineData(null, false)]
        [InlineData("Observação comum", false)]
        public void IsParcela_ConformeObservacao_RetornaSeEParcela(string? observacao, bool esperado)
        {
            // Arrange
            var transacao = new Transacao { Observacao = observacao };

            // Act
            var resultado = transacao.IsParcela();

            // Assert
            Assert.Equal(esperado, resultado);
        }
        #endregion

        #region CalcularMovimentoDaConta Tests
        [Fact]
        public void CalcularMovimentoDaConta_ComTodosOsTipos_SomaEntradasESaidasDaConta()
        {
            // Arrange
            var conta = Guid.NewGuid();
            var outraConta = Guid.NewGuid();

            var transacoes = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Receita,       Valor = 1000, ContaId = conta },
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 200,  ContaId = conta },
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 300,  ContaId = conta,      ContaDestinoId = outraConta }, // sai
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 50,   ContaId = outraConta, ContaDestinoId = conta },      // entra
                new() { TipoTransacao = TipoTransacao.Receita,       Valor = 999,  ContaId = outraConta }, // outra conta
                new() { TipoTransacao = TipoTransacao.Despesa,       Valor = 999,  ContaId = outraConta }, // outra conta
            };

            // Act
            var (entradas, saidas) = transacoes.CalcularMovimentoDaConta(conta);

            // Assert
            Assert.Equal(1050, entradas);
            Assert.Equal(500, saidas);
        }

        [Fact]
        public void CalcularMovimentoDaConta_ListaVazia_RetornaZeros()
        {
            // Act
            var (entradas, saidas) = new List<Transacao>().CalcularMovimentoDaConta(Guid.NewGuid());

            // Assert
            Assert.Equal(0, entradas);
            Assert.Equal(0, saidas);
        }

        [Fact]
        public void CalcularMovimentoDaConta_TransferenciaDoPontoDeVistaDoDestino_ContaComoEntrada()
        {
            // Arrange — pagamento de fatura: sai da corrente e entra no cartão
            var corrente = Guid.NewGuid();
            var cartao = Guid.NewGuid();
            var pagamento = new List<Transacao>
            {
                new() { TipoTransacao = TipoTransacao.Transferencia, Valor = 700, ContaId = corrente, ContaDestinoId = cartao }
            };

            // Act
            var movimentoCorrente = pagamento.CalcularMovimentoDaConta(corrente);
            var movimentoCartao = pagamento.CalcularMovimentoDaConta(cartao);

            // Assert
            Assert.Equal((0m, 700m), movimentoCorrente);
            Assert.Equal((700m, 0m), movimentoCartao);
        }
        #endregion
    }
}
