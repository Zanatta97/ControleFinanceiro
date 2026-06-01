using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IContaService
    {
        Task<IEnumerable<Conta>> GetAllAsync();
        Task<IEnumerable<Conta>> GetAllByAmbienteAsync(Guid ambienteId);
        Task<Conta?> GetAsync(Guid id, Guid ambienteId);
        Task<Conta?> GetReadOnlyAsync(Guid id, Guid ambienteId);
        Task<Conta> AddAsync(Conta conta);
        Task<Conta> UpdateAsync(Guid id, Conta conta, Guid ambienteId);
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);
    }
}
