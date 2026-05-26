using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Net.NetworkInformation;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ControleFinanceiroAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly ILogger<AuthController> _logger;
        private readonly ITokenService _tokenService;
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;

        public AuthController(ILogger<AuthController> logger,
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

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("createRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var roleExist = await _roleManager.RoleExistsAsync(roleName);

            if (!roleExist)
            {
                var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
                
                if (result.Succeeded)
                {
                    return Ok(ApiResponseDTO<object>.SuccessResponse(result));
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDTO<object>.ErrorResponse(
                        $"Erro ao criar a role '{roleName}': {string.Join(", ", result.Errors.Select(e => e.Description))}",
                        StatusCodes.Status500InternalServerError));
                }
            }

            return BadRequest(ApiResponseDTO<object>.ErrorResponse($"Role '{roleName}' já existe.",
                              StatusCodes.Status400BadRequest));
        }

        [HttpPost]
        [Authorize(Policy = "Admin")]
        [Route("assignUserToRole")]
        public async Task<IActionResult> AssignUserToRole(string email, string roleName)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user != null)
            {
                var result = await _userManager.AddToRoleAsync(user, roleName);

                if (result.Succeeded)
                {
                    return Ok(ApiResponseDTO<object>.SuccessResponse(result));

                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError, ApiResponseDTO<object>.ErrorResponse(
                        $"Erro ao atribuir a role '{roleName}' ao usuário '{email}': {string.Join(", ", result.Errors.Select(e => e.Description))}",
                        StatusCodes.Status500InternalServerError));
                } 
            }

            return BadRequest(ApiResponseDTO<object>.ErrorResponse($"Usuário com email '{email}' não encontrado.",
                              StatusCodes.Status400BadRequest));
        }

        [HttpGet("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginRequestDTO usuario)
        {
            var user = await _userManager.FindByNameAsync(usuario.Email);

            if (user is not null && await _userManager.CheckPasswordAsync(user, usuario.Senha))
            {
                var userRoles = await _userManager.GetRolesAsync(user);

                var authClaims  = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName!),
                    new Claim(ClaimTypes.Email, user.Email!),
                    new Claim("id", user.UserName!)
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var token = _tokenService.GenerateAccessToken(authClaims, _configuration);

                var refreshToken = _tokenService.GenerateRefreshToken();

                _ = int.TryParse(_configuration["JWT:RefreshTokenValidityInMinutes"], out int refreshTokenValidityInMinutes);

                user.RefreshTokenExpiryTime = DateTime.UtcNow.AddMinutes(refreshTokenValidityInMinutes);

                user.RefreshToken = refreshToken;

                return Ok(new
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    RefreshToken = refreshToken,
                    Expiration = token.ValidTo
                });
            }

            return Unauthorized();
        }
    }
}
