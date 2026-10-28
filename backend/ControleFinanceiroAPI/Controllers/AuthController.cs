using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Filters;
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
    /// <summary>
    /// Login, refresh de token e gestão de roles. Sem [Authorize] na classe: login, demo e refresh-token
    /// são anônimos; os demais declaram a própria exigência. Cada ação repassa o ApiResponseDTO do service
    /// usando o StatusCode que vem nele.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUsuarioService _service;
        private readonly IDemoService _demoService;

        public AuthController(IUsuarioService service, IDemoService demoService)
        {
            _service = service;
            _demoService = demoService;
        }

        /// <summary>Entra no modo demonstração, sem senha; recria os dados de exemplo uma vez por dia.</summary>
        [HttpPost]
        [Route("demo")]
        public async Task<IActionResult> LoginDemo()
        {
            var resultado = await _demoService.LoginDemoAsync();

            return StatusCode(resultado.StatusCode, resultado);
        }

        /// <summary>Cria uma role do Identity. Só Admin.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("createRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var resultado = await _service.CreateRole(roleName);

            return StatusCode(resultado.StatusCode, resultado);
        }

        /// <summary>Adiciona um usuário (por e-mail) a uma role. Só Admin.</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("assignUserToRole")]
        public async Task<IActionResult> AssignUserToRole(string email, string roleName)
        {
            var resultado = await _service.AssignUserToRole(email, roleName);

            return StatusCode(resultado.StatusCode, resultado);
        }

        /// <summary>
        /// Login por e-mail e senha. O token só traz "ambiente_id" se o usuário já tiver selecionado um
        /// ambiente antes; sem ele, o frontend leva à seleção de ambiente.
        /// </summary>
        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginRequestDTO usuario)
        {
            var resultado = await _service.Login(usuario);

            return StatusCode(resultado.StatusCode, resultado);
        }

        /// <summary>Cadastra um usuário. Só Admin desde a issue #47 (sem auto-cadastro).</summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UsuarioRegisterDTO dto)
        {
            var resultado = await _service.Register(dto);

            return StatusCode(resultado.StatusCode, resultado);

        }

        /// <summary>Troca a senha do usuário logado. Bloqueado no modo demonstração.</summary>
        [HttpPost]
        [Authorize]
        [BloquearDemo]
        [Route("alterar-senha")]
        public async Task<IActionResult> AlterarSenha([FromBody] AlterarSenhaDTO dto)
        {
            var userId = User.FindFirstValue("id");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var resultado = await _service.AlterarSenha(userId, dto);
            return StatusCode(resultado.StatusCode, resultado);
        }

        /// <summary>
        /// Troca access token expirado + refresh token por um novo par. Anônimo de propósito: é chamado
        /// justamente quando o access token já não autentica (interceptor do frontend em api/client.ts).
        /// </summary>
        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenDTO tokenDTO)
        {
            var resultado = await _service.RefreshToken(tokenDTO);

            return StatusCode(resultado.StatusCode, resultado);

        }

        /// <summary>Revoga o refresh token do usuário (username = e-mail). Só Admin.</summary>
        [HttpPost]
        [Route("revoke/{username}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Revoke(string username)
        {
            var resultado = await _service.Revoke(username);

            return StatusCode(resultado.StatusCode, resultado);

        }
    }
}
