using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface ITransacaoService
    {
        Task<IEnumerable<Transacao>> GetAllAsync();
        Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId);
        Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId, Guid ambienteId);
        Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim);
        Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo);
        Task<Transacao?> GetAsync(Guid id, Guid ambienteId);
        Task<Transacao?> GetReadOnlyAsync(Guid id, Guid ambienteId);
        Task<Transacao> AddAsync(Transacao transacao);
        Task<IEnumerable<Transacao>> AddManyAsync(IEnumerable<Transacao> transacoes);
        Task<Transacao> UpdateAsync(Guid id, Transacao transacao, Guid ambienteId);
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);
    }
}
