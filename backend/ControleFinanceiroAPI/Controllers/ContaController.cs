using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Conta;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    /// <summary>
    /// Contas do ambiente ativo. Ambiente e autor vêm do token; sem ambiente selecionado as rotas
    /// respondem 403.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ContaController : ControllerBase
    {
        private readonly IContaService _service;

        public ContaController(IContaService service)
        {
            _service = service;
        }

        /// <summary>Contas de todos os ambientes. Só Admin do sistema; 404 se não houver nenhuma.</summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<ContaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var contas = await _service.GetAllAsync();

            if (!contas.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<ContaResponseDTO>>.ErrorResponse("Nenhuma conta encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<ContaResponseDTO>>.SuccessResponse(contas.ToDTOList()));
        }

        /// <summary>Contas do ambiente ativo (lista vazia quando não há).</summary>
        [HttpGet("ambiente")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<ContaResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllByAmbiente()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var contas = await _service.GetAllByAmbienteAsync(ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<ContaResponseDTO>>.SuccessResponse(contas.ToDTOList()));
        }

        /// <summary>Uma conta do ambiente ativo; 404 se não existir nele.</summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<ContaResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var conta = await _service.GetAsync(id, ambienteId);

            if (conta is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<ContaResponseDTO>.ErrorResponse("Conta não encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<ContaResponseDTO>.SuccessResponse(conta.ToResponseDTO()!));
        }

        /// <summary>Cria a conta no ambiente ativo.</summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDTO<ContaResponseDTO>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] ContaRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var conta = await _service.AddAsync(dto.ToEntity(ambienteId, userId)!);

            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDTO<ContaResponseDTO>.SuccessResponse(conta.ToResponseDTO()!, StatusCodes.Status201Created));
        }

        /// <summary>Atualiza nome, tipo e saldo informado; 404 se a conta não existir no ambiente.</summary>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<ContaResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] ContaRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var conta = await _service.UpdateAsync(id, dto.ToEntity(ambienteId, userId)!, ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<ContaResponseDTO>.SuccessResponse(conta.ToResponseDTO()!));
        }

        /// <summary>
        /// Exclui a conta e, em cascata, as transações de origem nela. Conta que é destino de transferência
        /// não pode ser excluída (FK Restrict; o erro do banco volta como 500). 404 se não existir no ambiente.
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
                    ApiResponseDTO<object>.ErrorResponse("Conta não encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Conta excluída com sucesso."));
        }
    }
}
