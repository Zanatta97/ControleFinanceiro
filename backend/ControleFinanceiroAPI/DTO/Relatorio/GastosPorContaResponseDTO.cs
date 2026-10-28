using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Relatorio
{
    /// <summary>Despesas de uma conta no filtro e o percentual sobre o total.</summary>
    public class GastosPorContaResponseDTO
    {
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
        public TipoConta? TipoConta{ get; set; }
        public decimal TotalGasto { get; set; }
        public decimal Percentual { get; set; }
    }
}
