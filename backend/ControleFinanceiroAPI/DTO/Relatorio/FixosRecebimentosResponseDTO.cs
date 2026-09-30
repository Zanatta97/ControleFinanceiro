namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class FixosRecebimentosCategoriaDTO
    {
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }
        public decimal Total { get; set; }

        // "Fixo" (soma das despesas) ou "Recebimento" (soma das receitas)
        public string Grupo { get; set; } = string.Empty;
    }

    public class FixosRecebimentosResponseDTO
    {
        public int Mes { get; set; }
        public int Ano { get; set; }

        // Receitas nas categorias de recebimento
        public decimal Recebimentos { get; set; }

        // Receitas fora das categorias de recebimento
        public decimal OutrasReceitas { get; set; }

        // Despesas nas categorias fixas, parceladas ou não
        public decimal GastosFixos { get; set; }

        // Despesas parceladas fora das categorias fixas (a parcela em categoria fixa já está em GastosFixos)
        public decimal Parcelas { get; set; }

        // Demais despesas
        public decimal GastosVariaveis { get; set; }

        public decimal TotalDespesas { get; set; }

        // (Recebimentos + OutrasReceitas) − TotalDespesas
        public decimal Saldo { get; set; }

        // (GastosFixos + Parcelas) / Recebimentos × 100; nulo quando não houve recebimento
        public decimal? PercentualComprometido { get; set; }

        // GastosFixos / Recebimentos × 100; nulo quando não houve recebimento
        public decimal? PercentualFixosSobreRecebimentos { get; set; }

        public IEnumerable<FixosRecebimentosCategoriaDTO> Categorias { get; set; } = [];
    }
}
