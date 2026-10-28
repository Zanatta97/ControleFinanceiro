using ControleFinanceiroAPI.DTO.Transacao;
using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Relatorio
{
    /// <summary>Extrato de uma conta no período (pela Data), do ponto de vista dela: transferência de saída conta como saída, de entrada como entrada.</summary>
    public class ExtratoContaResponseDTO
    {
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
        public TipoConta TipoConta { get; set; }
        public decimal SaldoAtual { get; set; }
        public DateTime DataInicio { get; set; }
        public DateTime DataFim { get; set; }
        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public IEnumerable<TransacaoResponseDTO> Transacoes { get; set; } = [];
    }
}
