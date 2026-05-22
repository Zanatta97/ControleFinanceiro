using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces
{
    public interface IOrcamentoRepository : IRepository<Orcamento>
    {
        Task<IEnumerable<Orcamento>> GetAllByUsuarioAsync(string usuarioId);
        Task<IEnumerable<Orcamento>> GetByStatusAsync(string usuarioId, StatusOrcamento status);
    }
}
