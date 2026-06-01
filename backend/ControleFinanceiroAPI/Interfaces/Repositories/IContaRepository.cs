using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface IContaRepository : IRepository<Conta>
    {
        Task<IEnumerable<Conta>> GetAllByUsuarioAsync(string usuarioId);
        Task<IEnumerable<Conta>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
