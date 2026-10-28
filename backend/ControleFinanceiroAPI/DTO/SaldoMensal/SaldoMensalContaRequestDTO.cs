namespace ControleFinanceiroAPI.DTO.SaldoMensal
{
    /// <summary>Saldo inicial digitado para uma conta num mês (conta e mês vêm da rota/query).</summary>
    public class SaldoMensalContaRequestDTO
    {
        public decimal SaldoInicial { get; set; }
    }
}
