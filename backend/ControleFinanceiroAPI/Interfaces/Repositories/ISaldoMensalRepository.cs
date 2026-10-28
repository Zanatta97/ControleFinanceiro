using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>
    /// Saldos iniciais mensais por conta. <c>mes</c> é sempre o dia 1 do mês: a comparação é por igualdade
    /// de data, então outro dia não encontra o registro.
    /// </summary>
    public interface ISaldoMensalRepository
    {
        /// <summary>Saldo inicial da conta no mês; null se não houver. Sem chamadas no código atual.</summary>
        Task<SaldoMensalConta?> GetAsync(Guid contaId, Guid ambienteId, DateOnly mes);

        /// <summary>Saldos iniciais gravados para as contas do ambiente no mês.</summary>
        Task<IEnumerable<SaldoMensalConta>> GetAllByAmbienteAsync(Guid ambienteId, DateOnly mes);

        /// <summary>
        /// Inclui o saldo, ou só troca o SaldoInicial se já houver registro para a mesma conta, ambiente e mês.
        /// Efetivado no SaveChangesAsync.
        /// </summary>
        void AddOrUpdate(SaldoMensalConta saldo);
    }
}
