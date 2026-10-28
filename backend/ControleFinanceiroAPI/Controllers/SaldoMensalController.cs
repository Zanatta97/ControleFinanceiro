using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.SaldoMensal;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    /// <summary>
    /// Saldo das contas do ambiente ativo por mês de competência (tabela "Saldo das Contas" do Dashboard).
    /// mes/ano inválidos não são validados aqui: viram 500 no ErrorHandlingMiddleware.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SaldoMensalController : ControllerBase
    {
        private readonly ISaldoMensalService _service;

        public SaldoMensalController(ISaldoMensalService service)
        {
            _service = service;
        }

        /// <summary>Saldo inicial, entradas, saídas e saldo final de cada conta no mês.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<SaldoMensalContaResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByMes([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var resultado = await _service.GetByMesAsync(ambienteId, mes, ano);
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<SaldoMensalContaResponseDTO>>.SuccessResponse(resultado));
        }

        /// <summary>Grava o saldo inicial digitado para a conta no mês (substitui o anterior).</summary>
        [HttpPut("{contaId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SalvarSaldoInicial(Guid contaId, [FromQuery] int mes, [FromQuery] int ano, [FromBody] SaldoMensalContaRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            await _service.SalvarSaldoInicialAsync(ambienteId, contaId, mes, ano, dto.SaldoInicial);
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Saldo inicial salvo com sucesso."));
        }

        /// <summary>
        /// Recalcula o saldo inicial de todas as contas a partir do mês anterior (sobrescreve valores digitados)
        /// e já devolve os saldos do mês atualizados.
        /// </summary>
        [HttpPost("calcular")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<SaldoMensalContaResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> Calcular([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            await _service.CalcularSaldoInicialAsync(ambienteId, mes, ano);
            var resultado = await _service.GetByMesAsync(ambienteId, mes, ano);
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<SaldoMensalContaResponseDTO>>.SuccessResponse(resultado,
                    statusMessage: "Saldo inicial calculado com sucesso."));
        }
    }
}
