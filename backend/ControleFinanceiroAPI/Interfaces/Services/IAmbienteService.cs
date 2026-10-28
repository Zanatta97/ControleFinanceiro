using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// Ambientes, seus membros e a troca do ambiente ativo (que reemite o token).
    /// </summary>
    /// <remarks>
    /// Permissões dentro do ambiente vêm de AmbienteMembro.Role: "Dono" edita, exclui e transfere a posse;
    /// "Dono" e "Admin" gerenciam membros; "Membro" só usa. A role "Admin" do Identity (administrador do
    /// sistema) também pode editar, excluir e transferir a posse de qualquer ambiente.
    /// UnauthorizedAccessException vira 403, KeyNotFoundException vira 404 e InvalidOperationException
    /// vira 409 no ErrorHandlingMiddleware.
    /// </remarks>
    public interface IAmbienteService
    {
        /// <summary>Todos os ambientes do sistema, com membros e usuários (uso do Admin).</summary>
        Task<IEnumerable<Ambiente>> GetAllAsync();

        /// <summary>Busca um ambiente, desde que <paramref name="userId"/> seja membro dele.</summary>
        /// <returns>O ambiente, ou null se não existir ou se o usuário não for membro.</returns>
        Task<Ambiente?> GetAsync(Guid id, string userId);

        /// <summary>Ambientes dos quais o usuário é membro, com a lista de membros de cada um.</summary>
        Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId);

        /// <summary>Usuários membros do ambiente. Não confere se quem pede é membro.</summary>
        Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId);

        /// <summary>Cria o ambiente e registra o criador como membro "Dono".</summary>
        /// <exception cref="KeyNotFoundException">Usuário criador não encontrado.</exception>
        Task<Ambiente> AddAsync(Ambiente ambiente, string userId);

        /// <summary>Renomeia o ambiente. Só o Dono ou um Admin do sistema.</summary>
        /// <exception cref="UnauthorizedAccessException">Solicitante não é Dono nem Admin do sistema.</exception>
        /// <exception cref="KeyNotFoundException">Solicitante ou ambiente não encontrado.</exception>
        Task<Ambiente> UpdateAsync(Guid id, Ambiente ambiente, string userId);

        /// <summary>
        /// Exclui o ambiente. Só o Dono ou um Admin do sistema; <paramref name="userId"/> vazio
        /// pula a checagem (exclusão forçada pelo AdminController).
        /// </summary>
        /// <returns>false quando o ambiente não existe.</returns>
        /// <exception cref="UnauthorizedAccessException">Solicitante não é Dono nem Admin do sistema.</exception>
        Task<bool> DeleteAsync(Guid id, string userId);

        /// <summary>Adiciona o usuário do e-mail como "Membro". Só Dono ou Admin do ambiente.</summary>
        /// <exception cref="UnauthorizedAccessException">Solicitante não é membro ou é só "Membro".</exception>
        /// <exception cref="KeyNotFoundException">Nenhum usuário com esse e-mail.</exception>
        /// <exception cref="InvalidOperationException">O usuário já é membro.</exception>
        Task AdicionarMembroAsync(Guid ambienteId, string solicitanteUserId, string membroEmail);

        /// <summary>Remove um membro. Só Dono ou Admin do ambiente; o Dono não pode ser removido.</summary>
        /// <exception cref="UnauthorizedAccessException">Solicitante não é membro ou é só "Membro".</exception>
        /// <exception cref="KeyNotFoundException">O usuário não é membro do ambiente.</exception>
        /// <exception cref="InvalidOperationException">Tentativa de remover o Dono.</exception>
        Task RemoverMembroAsync(Guid ambienteId, string solicitanteUserId, string membroUserId);

        /// <summary>
        /// Transfere a posse: o membro indicado vira "Dono" e o membro que é o criador do ambiente
        /// (Ambiente.UsuarioId) é rebaixado a "Admin".
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">Solicitante não é Dono nem Admin do sistema.</exception>
        /// <exception cref="KeyNotFoundException">Solicitante não encontrado ou indicado não é membro.</exception>
        Task AtribuirDonoAsync(Guid ambienteId, string solicitanteUserId, string membroUserId);

        /// <summary>
        /// Torna o ambiente o ativo do usuário e emite um novo par de tokens com o claim "ambiente_id".
        /// </summary>
        /// <returns>Novo access token e novo refresh token; o refresh token anterior deixa de valer.</returns>
        /// <exception cref="UnauthorizedAccessException">O usuário não é membro do ambiente.</exception>
        /// <exception cref="KeyNotFoundException">Usuário não encontrado.</exception>
        Task<TokenDTO> SelecionarAmbienteAsync(Guid ambienteId, string userId);
    }
}
