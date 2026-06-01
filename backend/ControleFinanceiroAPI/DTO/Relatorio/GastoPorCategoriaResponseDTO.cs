namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class GastoPorCategoriaResponseDTO
    {
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public decimal TotalGasto { get; set; }
        public decimal Percentual { get; set; }
    }
}
