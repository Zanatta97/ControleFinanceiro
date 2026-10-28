using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// Emissão e leitura dos tokens JWT. Lê a seção "JWT" da configuração (SecretKey,
    /// TokenValidityInMinutes, ValidIssuer, ValidAudience).
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Gera o access token assinado com HMAC-SHA256, válido por JWT:TokenValidityInMinutes a partir de agora (UTC).
        /// </summary>
        /// <param name="claims">Claims do usuário (nome, e-mail, "id", roles e, se houver, "ambiente_id").</param>
        /// <param name="_config">Configuração com a seção JWT.</param>
        /// <exception cref="InvalidOperationException">JWT:SecretKey ausente na configuração.</exception>
        JwtSecurityToken GenerateAccessToken(IEnumerable<Claim> claims, IConfiguration _config);

        /// <summary>Gera um refresh token opaco: 128 bytes aleatórios criptograficamente seguros, em Base64.</summary>
        string GenerateRefreshToken();

        /// <summary>
        /// Lê os claims de um access token já expirado, para o refresh. Valida a assinatura e o algoritmo,
        /// mas não a validade, o emissor nem a audiência.
        /// </summary>
        /// <exception cref="Microsoft.IdentityModel.Tokens.SecurityTokenException">Token não é JWT HMAC-SHA256.</exception>
        /// <exception cref="InvalidOperationException">JWT:SecretKey ausente na configuração.</exception>
        /// <remarks>Assinatura inválida também lança exceção (do JwtSecurityTokenHandler); nunca devolve null.</remarks>
        ClaimsPrincipal GetPrincipalFromExpiredToken (string token, IConfiguration _config);
    }
}
