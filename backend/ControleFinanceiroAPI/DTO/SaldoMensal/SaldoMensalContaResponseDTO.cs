using ControleFinanceiroAPI.Enums;

namespace ControleFinanceiroAPI.DTO.SaldoMensal
{
    /// <summary>Saldo de uma conta numa competência: inicial + entradas − saídas = final; Diferenca = final − inicial.</summary>
    public class SaldoMensalContaResponseDTO
    {
        public Guid ContaId { get; set; }
        public string? NomeConta { get; set; }
        public TipoConta TipoConta { get; set; }
        public decimal SaldoInicial { get; set; }
        public decimal TotalEntradas { get; set; }
        public decimal TotalSaidas { get; set; }
        public decimal SaldoFinal { get; set; }
        public decimal Diferenca { get; set; }
    }
}
