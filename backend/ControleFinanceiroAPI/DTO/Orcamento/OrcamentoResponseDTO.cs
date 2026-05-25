namespace ControleFinanceiroAPI.DTO.Orcamento
{
    public class OrcamentoResponseDTO
    {
        public Guid Id { get; set; }
        public decimal ValorLimite { get; set; }
        public DateTime DataLimite { get; set; }
        public string StatusOrcamento { get; set; } = string.Empty;
        public Guid CategoriaId { get; set; }
    }
}
