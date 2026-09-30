using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class ProjecaoContaDTO
    {
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
        public TipoConta TipoConta { get; set; }

        // Saldo final do mês base (alvo − 1): saldo inicial salvo (0 se não houver) + movimento da competência
        public decimal SaldoInicialPrevisto { get; set; }

        // Movimento já lançado na competência alvo (inclui parcelas e transferências)
        public decimal EntradasLancadas { get; set; }
        public decimal SaidasLancadas { get; set; }

        // Média dos 3 meses anteriores nas categorias escolhidas, menos o que já foi lançado no alvo (mínimo 0)
        public decimal FixosEstimados { get; set; }
        public decimal RecebimentosEstimados { get; set; }

        public decimal SaldoFinalPrevisto { get; set; }

        // Só para cartão de crédito: despesas já lançadas no cartão no alvo + fixos estimados no cartão
        public decimal? FaturaPrevista { get; set; }
    }

    public class ProjecaoProximoMesResponseDTO
    {
        public int Mes { get; set; }
        public int Ano { get; set; }

        // Receitas lançadas no alvo + recebimentos estimados (transferência não entra)
        public decimal ReceitasPrevistas { get; set; }

        // Despesas lançadas no alvo + fixos estimados (transferência não entra)
        public decimal DespesasPrevistas { get; set; }

        // Despesas parceladas já lançadas na competência alvo
        public decimal TotalParcelas { get; set; }

        // ReceitasPrevistas − DespesasPrevistas
        public decimal ResultadoPrevisto { get; set; }

        // Soma do SaldoFinalPrevisto de todas as contas (transferências entre contas se anulam)
        public decimal SaldoPrevisto { get; set; }

        public IEnumerable<ProjecaoContaDTO> Contas { get; set; } = [];
    }
}
