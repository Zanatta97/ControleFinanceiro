namespace ControleFinanceiroAPI.DTO.Relatorio
{
    /// <summary>Totais de um mês de competência na evolução anual.</summary>
    public class EvolucaoMensalItemDTO
    {
        public int Mes { get; set; }
        public string NomeMes { get; set; } = string.Empty;
        public decimal TotalReceitas { get; set; }
        public decimal TotalDespesas { get; set; }
        public decimal DespesasCartao { get; set; }
        public decimal DespesasOutras { get; set; }
        public decimal Saldo { get; set; }
    }

    /// <summary>Evolução de receitas, despesas e saldo nos 12 meses de competência do ano.</summary>
    public class EvolucaoMensalResponseDTO
    {
        public int Ano { get; set; }
        public IEnumerable<EvolucaoMensalItemDTO> Meses { get; set; } = [];
    }
}
