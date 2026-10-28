namespace ControleFinanceiroAPI.Common.Pagination
{
    /// <summary>
    /// Parâmetros de paginação (página e tamanho). Nenhum endpoint usa esta classe hoje: as listas
    /// voltam completas e a paginação é feita no frontend (hook usePagination).
    /// </summary>
    public class PaginationParameters
    {
        const int MaxPageSize = 100;

        /// <summary>Página desejada, começando em 1.</summary>
        public int PageNumber { get; set; } = 1;
        private int _pageSize = MaxPageSize;

        /// <summary>Itens por página. Valores acima de 100 são reduzidos a 100.</summary>
        public int PageSize
        {
            get => _pageSize;
            set => _pageSize = (value > MaxPageSize) ? MaxPageSize : value;
        }
    }
}
