using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IOrcamentoService
    {
        Task<IEnumerable<Orcamento>> GetAllAsync();
        Task<IEnumerable<Orcamento>> GetAllByAmbienteAsync(Guid ambienteId);
        Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status);
        Task<Orcamento?> GetAsync(Guid id, Guid ambienteId);
        Task<Orcamento?> GetReadOnlyAsync(Guid id, Guid ambienteId);
        Task<Orcamento> AddAsync(Orcamento orcamento);
        Task<Orcamento> UpdateAsync(Guid id, Orcamento orcamento, Guid ambienteId);
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);
    }
}
