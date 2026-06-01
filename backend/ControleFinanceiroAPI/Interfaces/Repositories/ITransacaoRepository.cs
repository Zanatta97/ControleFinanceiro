using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface ITransacaoRepository : IRepository<Transacao>
    {
        Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId);
        Task<IEnumerable<Transacao>> GetByContaAndPeriodoAsync(Guid contaId, Guid ambienteId, DateTime inicio, DateTime fim);
        Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim);
        Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo);
        Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
