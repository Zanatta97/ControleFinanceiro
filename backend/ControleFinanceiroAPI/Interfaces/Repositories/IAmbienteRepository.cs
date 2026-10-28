using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>Acesso a ambientes e à tabela de membros (AmbienteMembro).</summary>
    public interface IAmbienteRepository : IRepository<Ambiente>
    {
        /// <summary>Ambientes dos quais o usuário é membro, com todos os membros e seus usuários carregados.</summary>
        Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId);

        /// <summary>Usuários membros do ambiente.</summary>
        Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId);

        /// <summary>
        /// Vínculo do usuário com o ambiente, rastreado pelo EF (alterar a Role e salvar persiste a mudança).
        /// É a checagem de participação usada pelo AmbienteService.
        /// </summary>
        /// <returns>O vínculo, ou null se o usuário não for membro.</returns>
        Task<AmbienteMembro?> GetMembroAsync(Guid ambienteId, string userId);

        /// <summary>Marca a inclusão do membro (efetivada no SaveChangesAsync).</summary>
        void AddMembro(AmbienteMembro membro);

        /// <summary>Marca a remoção do membro (efetivada no SaveChangesAsync).</summary>
        void RemoveMembro(AmbienteMembro membro);
    }
}
