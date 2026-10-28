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
    /// <summary>
    /// Painel de administração do sistema: usuários, roles do Identity e ambientes de todos.
    /// Todo o controller exige a role "Admin" do Identity.
    /// </summary>
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

        /// <summary>
        /// Cria um usuário (mesma regra do POST /api/Auth/register). Desde a issue #47 o cadastro de
        /// usuários é exclusivo do Admin; não há auto-cadastro.
        /// </summary>
        [HttpPost("usuarios")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status201Created)]
        public async Task<IActionResult> CriarUsuario([FromBody] UsuarioRegisterDTO dto)
        {
            var resultado = await _usuarioService.Register(dto);
            return StatusCode(resultado.StatusCode, resultado);
        }

        /// <summary>Todos os usuários, por nome, com roles e situação de bloqueio.</summary>
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

        /// <summary>Nomes das roles do Identity cadastradas, em ordem alfabética.</summary>
        [HttpGet("roles")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<string>>), StatusCodes.Status200OK)]
        public IActionResult GetRoles()
        {
            var roles = _roleManager.Roles.Select(r => r.Name!).OrderBy(r => r).ToList();
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<string>>.SuccessResponse(roles));
        }

        /// <summary>
        /// Substitui as roles do usuário pela lista enviada (lista vazia remove todas).
        /// A mudança só chega ao token do usuário no próximo login ou troca de ambiente (o refresh copia as roles antigas).
        /// </summary>
        [HttpPut("usuarios/{id}/roles")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtribuirRoles(string id, [FromBody] AtribuirRolesDTO dto)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound));

            // Comportamento atual: o resultado de RemoveFromRolesAsync/AddToRolesAsync não é conferido —
            // uma falha (ex.: role inexistente) ainda responde 200 "Roles atualizadas com sucesso."
            var rolesAtuais = await _userManager.GetRolesAsync(usuario);
            await _userManager.RemoveFromRolesAsync(usuario, rolesAtuais);

            if (dto.Roles.Count > 0)
                await _userManager.AddToRolesAsync(usuario, dto.Roles);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Roles atualizadas com sucesso."));
        }

        /// <summary>
        /// Bloqueia o usuário por tempo indeterminado (lockout do Identity até DateTimeOffset.MaxValue).
        /// </summary>
        /// <remarks>
        /// O login atual confere a senha com UserManager.CheckPasswordAsync, que não consulta o lockout,
        /// e o refresh também não o consulta: na prática o bloqueio não impede o acesso hoje.
        /// </remarks>
        [HttpPut("usuarios/{id}/bloquear")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> BloquearUsuario(string id)
        {
            var usuario = await _userManager.FindByIdAsync(id);
            if (usuario is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Usuário não encontrado.", StatusCodes.Status404NotFound));

            // O lockout só vale se estiver habilitado para o usuário; por isso habilita antes de definir a data
            await _userManager.SetLockoutEnabledAsync(usuario, true);
            await _userManager.SetLockoutEndDateAsync(usuario, DateTimeOffset.MaxValue);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Usuário bloqueado."));
        }

        /// <summary>Remove o bloqueio (data de fim do lockout volta a null).</summary>
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

        /// <summary>Exclui o usuário; 500 com as mensagens do Identity se a exclusão falhar.</summary>
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

        /// <summary>Todos os ambientes do sistema, com membros e papéis.</summary>
        [HttpGet("ambientes")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAmbientes()
        {
            var ambientes = await _ambienteService.GetAllAsync();
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(ambientes.ToDTOList()));
        }

        /// <summary>Exclui qualquer ambiente, sem exigir que o Admin seja membro; 404 se não existir.</summary>
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
