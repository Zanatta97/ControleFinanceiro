using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces
{
    public interface ITransacaoRepository : IRepository<Transacao>
    {
        Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId);
        Task<IEnumerable<Transacao>> GetByPeriodoAsync(string usuarioId, DateTime inicio, DateTime fim);
        Task<IEnumerable<Transacao>> GetByTipoAsync(string usuarioId, TipoTransacao tipo);
    }
}
