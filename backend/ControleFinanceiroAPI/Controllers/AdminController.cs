using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private readonly UserManager<Usuario> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IAmbienteService _ambienteService;
        private readonly IUsuarioService _usuarioService;

        public AdminController(
            UserManager<Usuario> userManager,
            RoleManager<IdentityRole> roleManager,
            IAmbienteService ambienteService,
            IUsuarioService usuarioService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _ambienteService = ambienteService;
            _usuarioService = usuarioService;
        }

        [HttpPost("usuarios")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CriarUsuario([FromBody] UsuarioRegisterDTO dto)
        {
            var resultado = await _usuarioService.Register(dto);
            return StatusCode(resultado.StatusCode, resultado);
        }

        [HttpGet("usuarios")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<UsuarioAdminResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetUsuarios()
        {
            var usuarios = await _userManager.Users.OrderBy(u => u.Nome).ToListAsync();

            var dtos = new List<UsuarioAdminResponseDTO>();
            foreach (var u in usuarios)
            {
                var roles = await _userManager.GetRolesAsync(u);
                dtos.Add(new UsuarioAdminResponseDTO
                {
                    Id = u.Id,
                    Nome = u.Nome,
                    Email = u.Email,
                    DtaCriacao = u.DtaCriacao,
                    Bloqueado = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                    Roles = roles.ToList()
                });
            }

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<UsuarioAdminResponseDTO>>.SuccessResponse(dtos));
        }

        [HttpGet("roles")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<string>>), StatusCodes.Status200OK)]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToList();
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<string>>.SuccessResponse(roles));
        }

        [HttpPut("usuarios/{id}/roles")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtribuirRoles(string id, [FromBody] AtribuirRolesDTO dto)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound));

            var rolesAtuais = await _userManager.GetRolesAsync(usuario);
            await _userManager.RemoveFromRolesAsync(usuario, rolesAtuais);

            if (dto.Roles.Count > 0)
                await _userManager.AddToRolesAsync(usuario, dto.Roles);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Roles atualizadas com sucesso."));
        }

        [HttpPut("usuarios/{id}/bloquear")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BloquearUsuario(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound));

            await _userManager.SetLockoutEnabledAsync(usuario, true);
            await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Usuário bloqueado."));
        }

        [HttpPut("usuarios/{id}/desbloquear")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DesbloquearUsuario(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound));

            await _userManager.SetLockoutEndDateAsync(usuario, null);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Usuário desbloqueado."));
        }

        [HttpDelete("usuarios/{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteUsuario(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound));

            var result = await _userManager.DeleteAsync(usuario);
            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponseDTO<object>.ErrorResponse(
                        string.Join(", ", result.Errors.Select(e => e.Description)),
                        StatusCodes.Status500InternalServerError));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Usuário excluído."));
        }

        [HttpGet("ambientes")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAmbientes()
        {
            var ambientes = await _ambienteService.GetAllAsync();
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(ambientes.ToDTOList()));
        }

        [HttpDelete("ambientes/{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> DeleteAmbiente(Guid id)
        {
            // userId vazio — admin pode forçar a deleção
            var deletado = await _ambienteService.DeleteAsync(id, string.Empty);
            if (!deletado)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Ambiente não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Ambiente excluído."));
        }
    }
}
