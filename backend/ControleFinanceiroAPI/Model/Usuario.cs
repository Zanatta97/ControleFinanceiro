using Microsoft.AspNetCore.Identity;

namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Usuário do Identity, com nome, refresh token e o último ambiente selecionado.
    /// UserName e Email recebem o mesmo valor no cadastro (UsuarioService.Register).
    /// </summary>
    public class Usuario : IdentityUser
    {
        public string? Nome { get; set; }
        public DateTime DtaCriacao { get; set; } = DateTime.Now;

        /// <summary>
        /// Refresh token vigente. Só existe um por usuário: cada login, refresh ou troca de ambiente
        /// gera outro e invalida o anterior; Revoke o apaga.
        /// </summary>
        public string? RefreshToken { get; set; }

        /// <summary>Validade do refresh token, em UTC. Renovada a cada rotação (sessão deslizante).</summary>
        public DateTime RefreshTokenExpiryTime { get; set; }

        /// <summary>
        /// Último ambiente selecionado. O login já devolve o token com esse ambiente no claim "ambiente_id",
        /// para o usuário não precisar escolher de novo.
        /// </summary>
        public Guid? AmbienteAtivoId { get; set; }
        public Ambiente? AmbienteAtivo { get; set; }
    }
}
