namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class ResumoFinanceiroResponseDTO
    {
        public int Mes { get; set; }
        public int Ano { get; set; }
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal Saldo { get; set; }
    }
}
