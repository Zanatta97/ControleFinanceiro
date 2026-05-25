using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.Logging;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        // LogSettingsManager é Singleton, então a instância injetada aqui é a mesma
        // que o RequestLoggingMiddleware usa — alterar aqui reflete imediatamente no middleware.
        private readonly LogSettingsManager _logSettingsManager;

        public LogsController(LogSettingsManager logSettingsManager)
        {
            _logSettingsManager = logSettingsManager;
        }

        // Retorna o estado atual: se o log completo está ativo ou não.
        // Útil para o frontend saber em qual estado está antes de renderizar o botão.
        [HttpGet("status")]
        public IActionResult ObterStatus()
        {
            var status = new { logAllRequests = _logSettingsManager.LogAllRequests };
            return Ok(ApiResponseDTO<object>.SuccessResponse(status));
        }

        // Ativa o log completo: a partir deste request, todos os requests e respostas
        // passarão a ser gravados no banco.
        [HttpPost("ativar")]
        public IActionResult AtivarLog()
        {
            _logSettingsManager.Ativar();
            return Ok(ApiResponseDTO<object>.SuccessResponse(
                data: null, 
                statusMessage: "Log completo ativado. Todos os requests serão registrados."
            ));
        }

        // Desativa o log completo: volta ao comportamento padrão,
        // gravando apenas requests que resultaram em erro (status >= 400).
        [HttpPost("desativar")]
        public IActionResult DesativarLog()
        {
            _logSettingsManager.Desativar();
            return Ok(ApiResponseDTO<object>.SuccessResponse(
                data: null,
                statusMessage: "Log completo desativado. Apenas erros serão registrados."
            ));
        }
    }
}
