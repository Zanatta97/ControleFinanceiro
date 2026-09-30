namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class FrequenciaCategoriaResponseDTO
    {
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public int Quantidade { get; set; }
        public decimal TotalGasto { get; set; }
        public decimal TicketMedio { get; set; }
    }
}
