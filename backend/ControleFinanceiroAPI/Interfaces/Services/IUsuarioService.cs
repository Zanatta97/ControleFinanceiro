using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Model;
using ControleFinanceiroAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// Autenticação e gestão de usuários do Identity. Os métodos devolvem o ApiResponseDTO pronto,
    /// com o status HTTP em StatusCode, e o controller só o repassa.
    /// </summary>
    public interface IUsuarioService
    {
        /// <summary>Cria uma role do Identity (ex.: "Admin", "User", "Demo").</summary>
        /// <returns>400 se a role já existe; 500 se o Identity recusar a criação.</returns>
        Task<ApiResponseDTO<object>> CreateRole(string roleName);

        /// <summary>Adiciona o usuário do e-mail a uma role do Identity.</summary>
        /// <returns>400 se o usuário não existe; 500 se o Identity recusar (ex.: role inexistente).</returns>
        Task<ApiResponseDTO<object>> AssignUserToRole(string email, string roleName);

        /// <summary>
        /// Confere e-mail e senha e emite access token + refresh token. Se o usuário já tiver um ambiente
        /// ativo salvo, o token sai com o claim "ambiente_id" desse ambiente.
        /// </summary>
        /// <returns>Dados com Token, RefreshToken e Expiration (UTC); 401 se e-mail ou senha não conferem.</returns>
        Task<ApiResponseDTO<object>> Login([FromBody] UsuarioLoginRequestDTO usuario);

        /// <summary>Cadastra um usuário (UserName = e-mail) e o coloca na role "User", se ela existir.</summary>
        /// <returns>400 se o e-mail já está cadastrado; 500 se o Identity recusar (ex.: senha curta).</returns>
        Task<ApiResponseDTO<object>> Register([FromBody] UsuarioRegisterDTO dto);

        /// <summary>
        /// Troca um access token expirado + refresh token válido por um novo par. O refresh token é rotacionado
        /// (o usado deixa de valer) e sua validade é renovada, então a sessão desliza enquanto houver uso.
        /// </summary>
        /// <returns>Novo AccessToken e RefreshToken; 400 se o refresh token não confere com o gravado ou expirou.</returns>
        /// <exception cref="ArgumentNullException">AccessToken ou RefreshToken ausente no corpo.</exception>
        /// <remarks>
        /// Access token com assinatura inválida não chega ao 400: o ITokenService lança exceção, que o
        /// ErrorHandlingMiddleware devolve como 500.
        /// </remarks>
        Task<ApiResponseDTO<object>> RefreshToken(TokenDTO tokenDTO);

        /// <summary>
        /// Apaga o refresh token do usuário (localizado pelo e-mail), forçando novo login quando o access
        /// token atual expirar. O access token já emitido continua válido até expirar.
        /// </summary>
        Task<ApiResponseDTO<object>> Revoke(string username);

        /// <summary>Troca a senha do usuário após conferir a senha atual e a confirmação.</summary>
        /// <returns>400 se a confirmação não confere ou o Identity recusar; 404 se o usuário não existe.</returns>
        Task<ApiResponseDTO<object>> AlterarSenha(string userId, AlterarSenhaDTO dto);
    }
}
