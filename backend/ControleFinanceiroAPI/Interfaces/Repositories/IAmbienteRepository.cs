using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface IAmbienteRepository : IRepository<Ambiente>
    {
        Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId);
        Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId);
        Task<AmbienteMembro?> GetMembroAsync(Guid ambienteId, string userId);
        void AddMembro(AmbienteMembro membro);
        void RemoveMembro(AmbienteMembro membro);
    }
}
