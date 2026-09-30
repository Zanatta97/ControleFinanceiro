namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class ComparativoCategoriaResponseDTO
    {
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public decimal GastoMes { get; set; }
        public decimal GastoMesAnterior { get; set; }

        // Média simples de M-1, M-2 e M-3; mês sem despesa entra como zero
        public decimal MediaTresMesesAnteriores { get; set; }

        // Nulo quando a base de comparação é zero (não há como calcular variação sobre zero)
        public decimal? VariacaoMesAnterior { get; set; }
        public decimal? VariacaoMedia { get; set; }
    }
}
