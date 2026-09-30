namespace ControleFinanceiroAPI.DTO.Relatorio
{
    public class MatrizCategoriaValorMesDTO
    {
        public int Mes { get; set; }
        public string NomeMes { get; set; } = string.Empty;
        public decimal Valor { get; set; }
    }

    public class MatrizCategoriaLinhaDTO
    {
        public Guid CategoriaId { get; set; }
        public string NomeCategoria { get; set; } = string.Empty;
        public string? Cor { get; set; }

        // Sempre 12 itens, de janeiro a dezembro; mês sem despesa vem com valor zero
        public IEnumerable<MatrizCategoriaValorMesDTO> Meses { get; set; } = [];
        public decimal TotalAno { get; set; }
    }

    public class MatrizCategoriaMesResponseDTO
    {
        public int Ano { get; set; }
        public IEnumerable<MatrizCategoriaLinhaDTO> Categorias { get; set; } = [];

        // Soma de todas as categorias em cada mês (linha de total da matriz)
        public IEnumerable<MatrizCategoriaValorMesDTO> TotaisPorMes { get; set; } = [];
        public decimal TotalAno { get; set; }
    }
}
