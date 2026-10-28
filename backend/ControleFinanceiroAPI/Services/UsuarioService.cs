using ControleFinanceiroAPI.Controllers;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Services
{
    /// <summary>Implementação de <see cref="IUsuarioService"/> sobre o ASP.NET Core Identity.</summary>
    public class UsuarioService : IUsuarioService
    {
        private readonly ILogger<UsuarioService> _logger;
        private readonly ITokenService _tokenService;
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public UsuarioService(ILogger<UsuarioService> logger,
                              ITokenService tokenService,
                              UserManager<Usuario> userManager,
                              RoleManager<IdentityRole> roleManager,
                              IConfiguration configuration)
        {
            _logger = logger;
            _tokenService = tokenService;
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
        }

        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> CreateRole(string roleName)
        {
            var roleExist = await _roleManager.RoleExistsAsync(roleName);

            if (!roleExist)
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));

                if (result.Succeeded)
                {
                    return ApiResponseDTO<object>.SuccessResponse(result);
                }
                else
                {
                    return ApiResponseDTO<object>.ErrorResponse(
                        $"Erro ao criar a role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}",
                        StatusCodes.Status500InternalServerError);
                }
            }

            return ApiResponseDTO<object>.ErrorResponse($"Role '{roleName}' já existe.",
                              StatusCodes.Status400BadRequest);
        }

        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> AssignUserToRole(string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    return ApiResponseDTO<object>.SuccessResponse(result);

                }
                else
                {
                    return ApiResponseDTO<object>.ErrorResponse(
                        $"Erro ao atribuir a role '{roleName}' ao usuário '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}",
                        StatusCodes.Status500InternalServerError);
                }
            }

            return ApiResponseDTO<object>.ErrorResponse($"Usuário com email '{email}' não encontrado.",
                              StatusCodes.Status400BadRequest);
        }

        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> Login([FromBody] UsuarioLoginRequestDTO usuario)
        {
            var user = await _userManager.FindByEmailAsync(usuario.Email);

            if (user is not null && await _userManager.CheckPasswordAsync(user, usuario.Senha))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim("id", user.Id),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                // Sem ambiente salvo, o token sai sem o claim e o frontend leva o usuário à seleção de
                // ambiente (POST /api/Ambiente/selecionar/{id}). Com ambiente salvo, reaproveita o último
                // selecionado — sem conferir de novo se o usuário ainda é membro dele.
                if (user.AmbienteAtivoId.HasValue)
                {
                    authClaims.Add(new Claim("ambiente_id", user.AmbienteAtivoId.Value.ToString()));
                }

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = _tokenService.GenerateAccessToken(authClaims, _configuration);

                var refreshToken = _tokenService.GenerateRefreshToken();

                // Configuração ausente ou inválida deixa a validade em 0 minutos: o refresh já nasce expirado
                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes);

                // Persistir token e validade é o que permite o refresh depois (sem isso o refresh sempre
                // falhava e o usuário era deslogado ao expirar o access token — issue #62).
                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

                user.RefreshToken = refreshToken;

                await _userManager.UpdateAsync(user);

                return ApiResponseDTO<object>.SuccessResponse(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo
                });
            }

            return ApiResponseDTO<object>.ErrorResponse("Erro ao fazer login", StatusCodes.Status401Unauthorized);
        }
        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> Register([FromBody] UsuarioRegisterDTO dto)
        {
            var userExists = await _userManager.FindByEmailAsync(dto.Email);

            if (userExists != null)
            {
                return ApiResponseDTO<object>.ErrorResponse(
                        $"Usuário de e-mail {dto.Email} já existe.",
                        StatusCodes.Status400BadRequest);
            }

            Usuario usuario = new()
            {
                Email = dto.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = dto.Email,
                Nome = dto.Nome
            };

            var result = await _userManager.CreateAsync(usuario, dto.Senha);

            if (!result.Succeeded)
            {
                // Comportamento atual: result.Errors é uma coleção, e a interpolação grava o nome do tipo
                // (não as descrições dos erros) no log e na mensagem devolvida ao frontend.
                _logger.LogError($"Erro na criação do usuário: {result.Errors}");
                return ApiResponseDTO<object>.ErrorResponse(
                        $"Erro na criação do usuário: {result.Errors}",
                        StatusCodes.Status500InternalServerError);
            }

            // Se a role "User" não existir no banco, o usuário é criado sem role nenhuma (sem erro)
            if (await _roleManager.RoleExistsAsync("User"))
                await _userManager.AddToRoleAsync(usuario, "User");

            return ApiResponseDTO<object>.SuccessResponse("Usuário criado com sucesso");

        }
        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> RefreshToken(TokenDTO tokenDTO)
        {
            if (tokenDTO is null)
            {
                return ApiResponseDTO<object>.ErrorResponse("Token inválido", StatusCodes.Status400BadRequest);
            }

            string accessToken = tokenDTO.AccessToken ?? throw new ArgumentNullException(nameof(tokenDTO.AccessToken));
            string refreshToken = tokenDTO.RefreshToken ?? throw new ArgumentNullException(nameof(tokenDTO.RefreshToken));

            // O access token chega expirado de propósito: só a assinatura é conferida, para saber de quem é
            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken, _configuration);

            // GetPrincipalFromExpiredToken lança exceção em vez de devolver null; este ramo não é alcançado hoje
            if (principal is null)
            {
                return ApiResponseDTO<object>.ErrorResponse("Token de acesso inválido", StatusCodes.Status400BadRequest);
            }

            // O claim Name guarda o UserName, que o cadastro iguala ao e-mail — daí a busca por e-mail
            string username = principal.Identity!.Name!;

            var user = await _userManager.FindByEmailAsync(username);

            // Só existe um refresh token válido por usuário (o último emitido). Um token antigo, já
            // rotacionado, cai aqui — por isso o frontend precisa fazer um único refresh por vez (api/client.ts).
            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return ApiResponseDTO<object>.ErrorResponse("Token de atualização inválido", StatusCodes.Status400BadRequest);
            }

            // Os claims são copiados do token expirado, inclusive "ambiente_id" e as roles: o ambiente selecionado
            // se mantém, e mudanças de role ou de participação no ambiente só aparecem num novo login/seleção.
            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims.ToList(), _configuration);

            // Rotação: o refresh token usado deixa de valer e a validade recomeça (sessão deslizante)
            var newRefreshToken = _tokenService.GenerateRefreshToken();

            _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

            await _userManager.UpdateAsync(user);

            return ApiResponseDTO<object>.SuccessResponse(new
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken,
            });

        }

        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> Revoke(string username)
        {
            // username é o e-mail (UserName e Email são iguais desde o cadastro)
            var user = await _userManager.FindByEmailAsync(username);

            if (user == null) return ApiResponseDTO<object>.ErrorResponse("Usuário Inválido", StatusCodes.Status400BadRequest);

            user.RefreshToken = null;

            await _userManager.UpdateAsync(user);

            return ApiResponseDTO<object>.SuccessResponse("Token revogado com sucesso");

        }

        /// <inheritdoc />
        public async Task<ApiResponseDTO<object>> AlterarSenha(string userId, AlterarSenhaDTO dto)
        {
            if (dto.NovaSenha != dto.ConfirmacaoNovaSenha)
                return ApiResponseDTO<object>.ErrorResponse("A nova senha e a confirmação não coincidem.", StatusCodes.Status400BadRequest);

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound);

            var result = await _userManager.ChangePasswordAsync(user, dto.SenhaAtual, dto.NovaSenha);
            if (!result.Succeeded)
            {
                var erro = result.Errors.FirstOrDefault()?.Description ?? "Erro ao alterar a senha.";
                return ApiResponseDTO<object>.ErrorResponse(erro, StatusCodes.Status400BadRequest);
            }

            return ApiResponseDTO<object>.SuccessResponse("Senha alterada com sucesso.");
        }
    }
}
