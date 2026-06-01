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
        private readonly IUsuarioService _service;

        public AuthController(IUsuarioService service)
        {
            _service = service;
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("createRole")]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            var resultado = await _service.CreateRole(roleName);

            return StatusCode(resultado.StatusCode, resultado);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [Route("assignUserToRole")]
        public async Task<IActionResult> AssignUserToRole(string email, string roleName)
        {
            var resultado = await _service.AssignUserToRole(email, roleName);

            return StatusCode(resultado.StatusCode, resultado);
        }

        [HttpPost]
        [Route("login")]
        public async Task<IActionResult> Login([FromBody] UsuarioLoginRequestDTO usuario)
        {
            var resultado = await _service.Login(usuario);

            return StatusCode(resultado.StatusCode, resultado);
        }

        [HttpPost]
        [Route("register")]
        public async Task<IActionResult> Register([FromBody] UsuarioRegisterDTO dto)
        {
            var resultado = await _service.Register(dto);

            return StatusCode(resultado.StatusCode, resultado);

        }

        [HttpPost]
        [Route("refresh-token")]
        public async Task<IActionResult> RefreshToken(TokenDTO tokenDTO)
        {
            var resultado = await _service.RefreshToken(tokenDTO);

            return StatusCode(resultado.StatusCode, resultado);

        }

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
