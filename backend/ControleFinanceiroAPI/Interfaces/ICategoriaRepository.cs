using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces
{
    public interface ICategoriaRepository : IRepository<Categoria>
    {
        Task<IEnumerable<Categoria>> GetAllByUsuarioAsync(string usuarioId);
    }
}
