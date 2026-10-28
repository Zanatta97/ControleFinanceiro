using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.Transacao
{
    /// <summary>Transação com os nomes de categoria e contas (preenchidos só quando a consulta os carregou).</summary>
    public class TransacaoResponseDTO
    {
        public Guid Id { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string? Observacao { get; set; }
        public TipoTransacao TipoTransacao { get; set; }
        public Guid CategoriaId { get; set; }
        public string? CategoriaNome { get; set; }
        public Guid ContaId { get; set; }
        public string? ContaNome { get; set; }
        public Guid? ContaDestinoId { get; set; }
        public string? ContaDestinoNome { get; set; }
        public string? UsuarioId { get; set; }
        public DateOnly MesCompetencia { get; set; }
    }
}
