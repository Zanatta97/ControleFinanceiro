namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class EvolucaoMensalItemDTO
    {
        public int Mes { get; set; }
        public string NomeMes { get; set; } = string.Empty;
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal Saldo { get; set; }
    }

    public class EvolucaoMensalResponseDTO
    {
        public int Ano { get; set; }
        public IEnumerable<EvolucaoMensalItemDTO> Meses { get; set; } = [];
    }
}
