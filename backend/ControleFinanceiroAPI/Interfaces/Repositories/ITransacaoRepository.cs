using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface ITransacaoRepository : IRepository<Transacao>
    {
        Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId);
        Task<IEnumerable<Transacao>> GetByContaAndPeriodoAsync(Guid contaId, Guid ambienteId, DateTime inicio, DateTime fim);
        Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim);
        Task<IEnumerable<Transacao>> GetByMesCompetenciaAsync(Guid ambienteId, int mes, int ano);
        Task<IEnumerable<Transacao>> GetByMesCompetenciaEContaAsync(Guid ambienteId, Guid contaId, int mes, int ano);
        Task<IEnumerable<Transacao>> GetAPartirDeMesCompetenciaAsync(Guid ambienteId, Guid contaId, DateOnly mesInicio);
        Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo);
        Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
