using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Relatorio;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _service;

        public RelatorioController(IRelatorioService service)
        {
            _service = service;
        }

        [HttpGet("resumo")]
        [ProducesResponseType(typeof(ApiResponseDTO<ResumoFinanceiroResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumo([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var resumo = await _service.GetResumoAsync(ambienteId, mes, ano);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<ResumoFinanceiroResponseDTO>.SuccessResponse(resumo));
        }

        [HttpGet("por-categoria")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetGastoPorCategoria([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var gastos = await _service.GetGastoPorCategoriaAsync(ambienteId, mes, ano);

            if (!gastos.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>.ErrorResponse("Nenhuma despesa encontrada no período.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>.SuccessResponse(gastos));
        }

        [HttpGet("evolucao-mensal")]
        [ProducesResponseType(typeof(ApiResponseDTO<EvolucaoMensalResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvolucaoMensal([FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var evolucao = await _service.GetEvolucaoMensalAsync(ambienteId, ano);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<EvolucaoMensalResponseDTO>.SuccessResponse(evolucao));
        }

        [HttpGet("orcamentos")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<OrcamentoStatusResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrcamentosStatus()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var orcamentos = await _service.GetOrcamentosStatusAsync(ambienteId);

            if (!orcamentos.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<OrcamentoStatusResponseDTO>>.ErrorResponse("Nenhum orçamento encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<OrcamentoStatusResponseDTO>>.SuccessResponse(orcamentos));
        }

        [HttpGet("extrato/conta/{contaId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<ExtratoContaResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetExtratoConta(Guid contaId, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var extrato = await _service.GetExtratoContaAsync(ambienteId, contaId, dataInicio, dataFim);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<ExtratoContaResponseDTO>.SuccessResponse(extrato));
        }
    }
}
