namespace ControleFinanceiroAPI.DTO.Orcamento
{
    public class OrcamentoRequestDTO
    {
        public decimal ValorLimite { get; set; }
        public DateTime DataLimite { get; set; }
        public Guid CategoriaId { get; set; }
    }
}
