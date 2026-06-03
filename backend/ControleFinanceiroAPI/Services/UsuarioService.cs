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

                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes);

                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

                user.RefreshToken = refreshToken;

                return ApiResponseDTO<object>.SuccessResponse(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo
                });
            }

            return ApiResponseDTO<object>.ErrorResponse("Erro ao fazer login", StatusCodes.Status401Unauthorized);
        }
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
                _logger.LogError($"Erro na criação do usuário: {result.Errors}");
                return ApiResponseDTO<object>.ErrorResponse(
                        $"Erro na criação do usuário: {result.Errors}",
                        StatusCodes.Status500InternalServerError);
            }

            if (await _roleManager.RoleExistsAsync("User"))
                await _userManager.AddToRoleAsync(usuario, "User");

            return ApiResponseDTO<object>.SuccessResponse("Usuário criado com sucesso");

        }
        public async Task<ApiResponseDTO<object>> RefreshToken(TokenDTO tokenDTO)
        {
            if (tokenDTO is null)
            {
                return ApiResponseDTO<object>.ErrorResponse("Token inválido", StatusCodes.Status400BadRequest);
            }

            string accessToken = tokenDTO.AccessToken ?? throw new ArgumentNullException(nameof(tokenDTO.AccessToken));
            string refreshToken = tokenDTO.RefreshToken ?? throw new ArgumentNullException(nameof(tokenDTO.RefreshToken));

            var principal = _tokenService.GetPrincipalFromExpiredToken(accessToken, _configuration);

            if (principal is null)
            {
                return ApiResponseDTO<object>.ErrorResponse("Token de acesso inválido", StatusCodes.Status400BadRequest);
            }

            string username = principal.Identity!.Name!;

            var user = await _userManager.FindByEmailAsync(username);

            if (user is null || user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return ApiResponseDTO<object>.ErrorResponse("Token de atualização inválido", StatusCodes.Status400BadRequest);
            }

            var newAccessToken = _tokenService.GenerateAccessToken(principal.Claims.ToList(), _configuration);

            var newRefreshToken = _tokenService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;

            await _userManager.UpdateAsync(user);

            return ApiResponseDTO<object>.SuccessResponse(new
            {
                AccessToken = new JwtSecurityTokenHandler().WriteToken(newAccessToken),
                RefreshToken = newRefreshToken,
            });

        }

        public async Task<ApiResponseDTO<object>> Revoke(string username)
        {
            var user = await _userManager.FindByEmailAsync(username);

            if (user == null) return ApiResponseDTO<object>.ErrorResponse("Usuário Inválido", StatusCodes.Status400BadRequest);

            user.RefreshToken = null;

            await _userManager.UpdateAsync(user);

            return ApiResponseDTO<object>.SuccessResponse("Token revogado com sucesso");

        }

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
