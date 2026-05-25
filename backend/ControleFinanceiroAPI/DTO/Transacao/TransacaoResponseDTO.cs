namespace ControleFinanceiroAPI.DTO.Transacao
{
    public class TransacaoResponseDTO
    {
        public Guid Id { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string? Observacao { get; set; }
        public string? TipoTransacao { get; set; }
        public Guid CategoriaId { get; set; }
        public string? CategoriaNome { get; set; }
        public Guid ContaId { get; set; }
        public string? ContaNome { get; set; }
    }
}
