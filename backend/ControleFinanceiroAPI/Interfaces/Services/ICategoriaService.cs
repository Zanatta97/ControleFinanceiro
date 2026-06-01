using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface ICategoriaService
    {
        Task<IEnumerable<Categoria>> GetAllAsync();
        Task<IEnumerable<Categoria>> GetAllByAmbienteAsync(Guid ambienteId);
        Task<Categoria?> GetAsync(Guid id, Guid ambienteId);
        Task<Categoria?> GetReadOnlyAsync(Guid id, Guid ambienteId);
        Task<Categoria> AddAsync(Categoria categoria);
        Task<Categoria> UpdateAsync(Guid id, Categoria categoria, Guid ambienteId);
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);

    }
}
