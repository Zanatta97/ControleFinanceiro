using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Conta
{
    public class ContaResponseDTO
    {
        public Guid Id { get; set; }
        public string Nome { get; set; } = string.Empty;
        public TipoConta TipoConta { get; set; }
        public decimal Saldo { get; set; }
        public string? UsuarioId { get; set; }
    }
}
