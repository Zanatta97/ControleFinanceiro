using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Orcamento
{
    public class OrcamentoRequestDTO
    {
        public string? Nome { get; set; }
        public string? Descricao { get; set; }
        public decimal ValorLimite { get; set; }
        public DateTime DataLimite { get; set; }
        public StatusOrcamento StatusOrcamento { get; set; }
        public Guid CategoriaId { get; set; }
        public string? UsuarioId { get; set; }
    }
}
