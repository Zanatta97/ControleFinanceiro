namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Saldo inicial de uma conta em um mês de competência. O saldo final não é gravado: é sempre
    /// calculado como SaldoInicial + entradas − saídas da competência (SaldoMensalService).
    /// Pode ser digitado pelo usuário ou calculado a partir do saldo final do mês anterior.
    /// </summary>
    public class SaldoMensalConta
    {
        public Guid Id { get; set; }
        public Guid ContaId { get; set; }
        public Conta? Conta { get; set; }
        public Guid AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
        public DateOnly Mes { get; set; }   // sempre dia 1
        public decimal SaldoInicial { get; set; }
    }
}
