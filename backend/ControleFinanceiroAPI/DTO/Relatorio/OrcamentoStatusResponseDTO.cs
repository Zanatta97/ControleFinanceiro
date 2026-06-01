using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class OrcamentoStatusResponseDTO
    {
        public Guid Id { get; set; }
        public string NomeOrcamento { get; set; } = string.Empty;
        public string? Descricao { get; set; }
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public decimal ValorLimite { get; set; }
        public decimal ValorGasto { get; set; }
        public decimal Percentual { get; set; }
        public DateTime DataLimite { get; set; }
        public StatusOrcamento Status { get; set; }
    }
}
