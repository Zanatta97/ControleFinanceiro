using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Categoria;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    /// <summary>
    /// Categorias do ambiente ativo. Ambiente e autor vêm do token; sem ambiente selecionado as rotas
    /// respondem 403.
    /// </summary>
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

        /// <summary>Categorias de todos os ambientes. Só Admin do sistema; 404 se não houver nenhuma.</summary>
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

        /// <summary>Categorias do ambiente ativo (lista vazia quando não há).</summary>
        [HttpGet("ambiente")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllByAmbiente()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var categorias = await _service.GetAllByAmbienteAsync(ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<CategoriaResponseDTO>>.SuccessResponse(categorias.ToDTOList()));
        }

        /// <summary>Uma categoria do ambiente ativo; 404 se não existir nele.</summary>
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

        /// <summary>Cria a categoria no ambiente ativo.</summary>
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

        /// <summary>Atualiza nome, cor e ícone; 404 se a categoria não existir no ambiente.</summary>
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

        /// <summary>
        /// Exclui a categoria; as transações e orçamentos dela são apagados em cascata pelo banco.
        /// 404 se não existir no ambiente.
        /// </summary>
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
