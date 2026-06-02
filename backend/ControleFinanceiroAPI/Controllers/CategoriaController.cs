using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Categoria;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CategoriaController : ControllerBase
    {
        private readonly ICategoriaService _service;

        public CategoriaController(ICategoriaService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var categorias = await _service.GetAllAsync();

            if (!categorias.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>.ErrorResponse("Nenhuma categoria encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>.SuccessResponse(categorias.ToDTOList()));
        }

        [HttpGet("ambiente")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllByAmbiente()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var categorias = await _service.GetAllByAmbienteAsync(ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>.SuccessResponse(categorias.ToDTOList()));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<CategoriaResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var categoria = await _service.GetAsync(id, ambienteId);

            if (categoria is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<CategoriaResponseDTO>.ErrorResponse("Categoria não encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<CategoriaResponseDTO>.SuccessResponse(categoria.ToResponseDTO()!));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDTO<CategoriaResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] CategoriaRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var categoria = await _service.AddAsync(dto.ToEntity(ambienteId, userId)!);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDTO<CategoriaResponseDTO>.SuccessResponse(categoria.ToResponseDTO()!, StatusCodes.Status201Created));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<CategoriaResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CategoriaRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var categoria = await _service.UpdateAsync(id, dto.ToEntity(ambienteId, userId)!, ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<CategoriaResponseDTO>.SuccessResponse(categoria.ToResponseDTO()!));
        }

        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var deletado = await _service.DeleteAsync(id, ambienteId);

            if (!deletado)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Categoria não encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Categoria excluída com sucesso."));
        }
    }
}
