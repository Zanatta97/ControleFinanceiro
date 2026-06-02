using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.SaldoMensal;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
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

        [HttpGet]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<SaldoMensalContaResponseDTO>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetByMes([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var resultado = await _service.GetByMesAsync(ambienteId, mes, ano);
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<SaldoMensalContaResponseDTO>>.SuccessResponse(resultado));
        }

        [HttpPut("{contaId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status200OK)]
        public async Task<IActionResult> SalvarSaldoInicial(Guid contaId, [FromQuery] int mes, [FromQuery] int ano, [FromBody] SaldoMensalContaRequestDTO dto)
        {
            var ambienteId = User.GetAmbienteAtivo();
            await _service.SalvarSaldoInicialAsync(ambienteId, contaId, mes, ano, dto.SaldoInicial);
            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<object>.SuccessResponse(null!, statusMessage: "Saldo inicial salvo com sucesso."));
        }

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
