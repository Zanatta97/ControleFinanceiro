using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces
{
    public interface IContaRepository : IRepository<Conta>
    {
        Task<IEnumerable<Conta>> GetAllByUsuarioAsync(string usuarioId);
    }
}
