namespace ControleFinanceiroAPI.Model
{
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
