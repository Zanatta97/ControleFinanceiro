namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class MaiorDespesaResponseDTO
    {
        public Guid TransacaoId { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public DateOnly MesCompetencia { get; set; }
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
    }
}
