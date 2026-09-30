using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class GastosPorContaResponseDTO
    {
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
        public TipoConta? TipoConta{ get; set; }
        public decimal TotalGasto { get; set; }
        public decimal Percentual { get; set; }
    }
}
