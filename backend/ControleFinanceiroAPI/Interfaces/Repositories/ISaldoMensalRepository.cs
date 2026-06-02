using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface ISaldoMensalRepository
    {
        Task<SaldoMensalConta?> GetAsync(Guid contaId, Guid ambienteId, DateOnly mes);
        Task<IEnumerable<SaldoMensalConta>> GetAllByAmbienteAsync(Guid ambienteId, DateOnly mes);
        void AddOrUpdate(SaldoMensalConta saldo);
    }
}
