namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class FaturaCompetenciaResponseDTO
    {
        public Guid ContaId { get; set; }
        public string NomeConta { get; set; } = string.Empty;
        public int Mes { get; set; }
        public int Ano { get; set; }
        public decimal TotalDespesas { get; set; }

        // Transferências com destino no cartão, na mesma competência (pagamentos da fatura)
        public decimal TotalPago { get; set; }

        // Total de despesas − total pago; negativo indica pagamento acima do valor da fatura
        public decimal SaldoEmAberto { get; set; }
    }
}
