using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Transacao
{
    public class TransacaoRequestDTO
    {
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string? Observacao { get; set; }
        public TipoTransacao TipoTransacao { get; set; }
        public Guid CategoriaId { get; set; }
        public Guid ContaId { get; set; }
        public string? UsuarioId { get; set; }
    }
}
