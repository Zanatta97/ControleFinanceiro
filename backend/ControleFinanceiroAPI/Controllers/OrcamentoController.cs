using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Orcamento;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    /// <summary>
    /// Orçamentos do ambiente ativo. O consumo de cada orçamento fica em GET /api/Relatorio/orcamentos.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class OrcamentoController : ControllerBase
    {
        private readonly IOrcamentoService _service;

        public OrcamentoController(IOrcamentoService service)
        {
            _service = service;
        }

        /// <summary>Orçamentos de todos os ambientes. Só Admin do sistema; 404 se não houver nenhum.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var orcamentos = await _service.GetAllAsync();

            if (!orcamentos.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>.ErrorResponse("Nenhum orçamento encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>.SuccessResponse(orcamentos.ToDTOList()));
        }

        /// <summary>Orçamentos do ambiente ativo, de qualquer status.</summary>
        [HttpGet("ambiente")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllByAmbiente()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var orcamentos = await _service.GetAllByAmbienteAsync(ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>.SuccessResponse(orcamentos.ToDTOList()));
        }

        /// <summary>Orçamentos do ambiente ativo com o status informado (1 Ativo, 0 Inativo).</summary>
        [HttpGet("status/{status}")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByStatus(StatusOrcamento status)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var orcamentos = await _service.GetByStatusAsync(ambienteId, status);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<OrcamentoResponseDTO>>.SuccessResponse(orcamentos.ToDTOList()));
        }

        /// <summary>Um orçamento do ambiente ativo; 404 se não existir nele.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<OrcamentoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var orcamento = await _service.GetAsync(id, ambienteId);

            if (orcamento is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<OrcamentoResponseDTO>.ErrorResponse("Orçamento não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<OrcamentoResponseDTO>.SuccessResponse(orcamento.ToResponseDTO()!));
        }

        /// <summary>Cria o orçamento no ambiente ativo.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDTO<OrcamentoResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] OrcamentoRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var orcamento = await _service.AddAsync(dto.ToEntity(ambienteId, userId)!);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDTO<OrcamentoResponseDTO>.SuccessResponse(orcamento.ToResponseDTO()!, StatusCodes.Status201Created));
        }

        /// <summary>Atualiza o orçamento; 404 se não existir no ambiente.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<OrcamentoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] OrcamentoRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var orcamento = await _service.UpdateAsync(id, dto.ToEntity(ambienteId, userId)!, ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<OrcamentoResponseDTO>.SuccessResponse(orcamento.ToResponseDTO()!));
        }

        /// <summary>Exclui o orçamento; 404 se não existir no ambiente.</summary>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(Guid id)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var deletado = await _service.DeleteAsync(id, ambienteId);

            if (!deletado)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<object>.ErrorResponse("Orçamento não encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Orçamento excluído com sucesso."));
        }
    }
}
