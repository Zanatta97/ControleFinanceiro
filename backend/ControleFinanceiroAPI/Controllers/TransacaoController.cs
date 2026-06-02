using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Transacao;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransacaoController : ControllerBase
    {
        private readonly ITransacaoService _service;

        public TransacaoController(ITransacaoService service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetAll()
        {
            var transacoes = await _service.GetAllAsync();

            if (!transacoes.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.ErrorResponse("Nenhuma transação encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(transacoes.ToDTOList()));
        }

        [HttpGet("ambiente")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllByAmbiente()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var transacoes = await _service.GetAllByAmbienteAsync(ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(transacoes.ToDTOList()));
        }

        [HttpGet("conta/{contaId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByConta(Guid contaId)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var transacoes = await _service.GetByContaAsync(contaId, ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(transacoes.ToDTOList()));
        }

        [HttpGet("periodo")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByPeriodo([FromQuery] DateTime inicio, [FromQuery] DateTime fim)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var transacoes = await _service.GetByPeriodoAsync(ambienteId, inicio, fim);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(transacoes.ToDTOList()));
        }

        [HttpGet("tipo/{tipo}")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByTipo(TipoTransacao tipo)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var transacoes = await _service.GetByTipoAsync(ambienteId, tipo);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(transacoes.ToDTOList()));
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<TransacaoResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var transacao = await _service.GetAsync(id, ambienteId);

            if (transacao is null)
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<TransacaoResponseDTO>.ErrorResponse("Transação não encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<TransacaoResponseDTO>.SuccessResponse(transacao.ToResponseDTO()!));
        }

        [HttpPost]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>), StatusCodes.Status201Created)]
        public async Task<IActionResult> Create([FromBody] TransacaoRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var parcelas = Math.Max(1, dto.Parcelas);

            if (parcelas == 1)
            {
                var transacao = await _service.AddAsync(dto.ToEntity(ambienteId, userId)!);
                return StatusCode(StatusCodes.Status201Created,
                    ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(
                        new[] { transacao.ToResponseDTO()! }, StatusCodes.Status201Created));
            }

            var entidades = Enumerable.Range(0, parcelas)
                .Select(i => dto.ToEntity(ambienteId, userId, i)!)
                .ToList();

            var criadas = await _service.AddManyAsync(entidades);
            return StatusCode(StatusCodes.Status201Created,
                ApiResponseDTO<IEnumerable<TransacaoResponseDTO>>.SuccessResponse(
                    criadas.ToDTOList(), StatusCodes.Status201Created));
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(ApiResponseDTO<TransacaoResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Update(Guid id, [FromBody] TransacaoRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var userId = User.GetUserId();
            var transacao = await _service.UpdateAsync(id, dto.ToEntity(ambienteId, userId)!, ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<TransacaoResponseDTO>.SuccessResponse(transacao.ToResponseDTO()!));
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
                    ApiResponseDTO<object>.ErrorResponse("Transação não encontrada.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Transação excluída com sucesso."));
        }
    }
}
