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

        /// <summary>
        /// Parcelas da competência alvo que ainda não estão lançadas nesta conta, inferidas da última parcela
        /// lançada de cada compra (mesmo valor, uma por mês). Parcela já lançada no alvo fica só em SaidasLancadas.
        /// </summary>
        public decimal ParcelasProjetadas { get; set; }

        // Inicial + entradas lançadas + recebimentos estimados − saídas lançadas − fixos estimados − parcelas projetadas
        public decimal SaldoFinalPrevisto { get; set; }

        // Só para cartão de crédito: despesas já lançadas no cartão no alvo + fixos estimados + parcelas projetadas no cartão
        public decimal? FaturaPrevista { get; set; }
    }

    public class ProjecaoProximoMesResponseDTO
    {
        public int Mes { get; set; }
        public int Ano { get; set; }

        // Receitas lançadas no alvo + recebimentos estimados (transferência não entra)
        public decimal ReceitasPrevistas { get; set; }

        // Despesas lançadas no alvo + fixos estimados + parcelas projetadas (transferência não entra)
        public decimal DespesasPrevistas { get; set; }

        // Parcelas da competência alvo: lançadas + projetadas (a parte projetada está em ParcelasProjetadas)
        public decimal TotalParcelas { get; set; }

        /// <summary>
        /// Soma das ParcelasProjetadas de todas as contas: parcelas do alvo inferidas, ainda não lançadas.
        /// TotalParcelas − ParcelasProjetadas = parcelas já lançadas no alvo.
        /// </summary>
        public decimal ParcelasProjetadas { get; set; }

        // ReceitasPrevistas − DespesasPrevistas
        public decimal ResultadoPrevisto { get; set; }

        // Soma do SaldoFinalPrevisto de todas as contas (transferências entre contas se anulam)
        public decimal SaldoPrevisto { get; set; }

        public IEnumerable<ProjecaoContaDTO> Contas { get; set; } = [];
    }
}
