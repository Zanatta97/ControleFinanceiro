using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface IOrcamentoRepository : IRepository<Orcamento>
    {
        Task<IEnumerable<Orcamento>> GetAllByUsuarioAsync(string usuarioId);
        Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status);
        Task<IEnumerable<Orcamento>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
