using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Ambiente;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Usuario;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AmbienteController : ControllerBase
    {
        private readonly IAmbienteService _service;

        public AmbienteController(IAmbienteService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var ambientes = await _service.GetAllAsync();

            if (!ambientes.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.ErrorResponse("Nenhum ambiente encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.SuccessResponse(ambientes.ToDTOList()));
        }

        [HttpGet("user-ambientes")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAllByUsuario()
        {
            var userId = User.GetUserId();
            var ambientes = await _service.GetAllByUsuarioAsync(userId);

            if (!ambientes.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.ErrorResponse("Nenhum ambiente encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<AmbienteResponseDTO>>.SuccessResponse(ambientes.ToDTOList()));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<AmbienteResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = User.GetUserId();
            var ambiente = await _service.GetAsync(id, userId);

            if (ambiente is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<AmbienteResponseDTO>.ErrorResponse("Ambiente não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<AmbienteResponseDTO>.SuccessResponse(ambiente.ToResponseDTO()!));
        }

        [HttpGet("{id}/membros")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<UsuarioResumoDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetMembros(Guid id)
        {
            var usuarios = await _service.GetAllByAmbienteAsync(id);

            if (!usuarios.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<UsuarioResumoDTO>>.ErrorResponse("Nenhum membro encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<UsuarioResumoDTO>>.SuccessResponse(usuarios.Select(u => u.ToResumoDTO()!)));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDTO<AmbienteResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] AmbienteRequestDTO dto)
        {
            var userId = User.GetUserId();
            var ambiente = await _service.AddAsync(dto.ToEntity()!, userId);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDTO<AmbienteResponseDTO>.SuccessResponse(ambiente.ToResponseDTO()!, StatusCodes.Status201Created));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<AmbienteResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] AmbienteRequestDTO dto)
        {
            var userId = User.GetUserId();
            var ambiente = await _service.UpdateAsync(id, dto.ToEntity()!, userId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<AmbienteResponseDTO>.SuccessResponse(ambiente.ToResponseDTO()!));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var userId = User.GetUserId();
            var deletado = await _service.DeleteAsync(id, userId);

            if (!deletado)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Ambiente não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Ambiente excluído com sucesso."));
        }

        [HttpPost("{id}/membros")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AdicionarMembro(Guid id, [FromQuery] string membroEmail)
        {
            var userId = User.GetUserId();
            await _service.AdicionarMembroAsync(id, userId, membroEmail);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Membro adicionado com sucesso."));
        }

        [HttpDelete("{id}/membros/{membroId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> RemoverMembro(Guid id, string membroId)
        {
            var userId = User.GetUserId();
            await _service.RemoverMembroAsync(id, userId, membroId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Membro removido com sucesso."));
        }

        [HttpPut("{id}/dono")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> AtribuirDono(Guid id, [FromQuery] string membroId)
        {
            var userId = User.GetUserId();
            await _service.AtribuirDonoAsync(id, userId, membroId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Dono atribuído com sucesso."));
        }

        [HttpPost("selecionar/{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<TokenDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SelecionarAmbiente(Guid id)
        {
            var userId = User.GetUserId();
            var token = await _service.SelecionarAmbienteAsync(id, userId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<TokenDTO>.SuccessResponse(token));
        }
    }
}
