using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IAmbienteService
    {
        Task<IEnumerable<Ambiente>> GetAllAsync();
        Task<Ambiente?> GetAsync(Guid id, string userId);
        Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId);
        Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId);
        Task<Ambiente> AddAsync(Ambiente ambiente, string userId);
        Task<Ambiente> UpdateAsync(Guid id, Ambiente ambiente, string userId);
        Task<bool> DeleteAsync(Guid id, string userId);
        Task AdicionarMembroAsync(Guid ambienteId, string solicitanteUserId, string membroEmail);
        Task RemoverMembroAsync(Guid ambienteId, string solicitanteUserId, string membroUserId);
        Task AtribuirDonoAsync(Guid ambienteId, string solicitanteUserId, string membroUserId);
        Task<TokenDTO> SelecionarAmbienteAsync(Guid ambienteId, string userId);
    }
}
