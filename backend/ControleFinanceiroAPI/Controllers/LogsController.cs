using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Admin;
using ControleFinanceiroAPI.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LogsController : ControllerBase
    {
        private readonly LogSettingsManager _logSettingsManager;
        private readonly AppDbContext _dbContext;

        public LogsController(LogSettingsManager logSettingsManager, AppDbContext dbContext)
        {
            _logSettingsManager = logSettingsManager;
            _dbContext = dbContext;
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

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ListarLogs(
            [FromQuery] DateTime? dataInicio,
            [FromQuery] DateTime? dataFim,
            [FromQuery] string? userId,
            [FromQuery] string? path,
            [FromQuery] int? statusCode,
            [FromQuery] bool apenasErros = false,
            [FromQuery] int limite = 100)
        {
            var query = _dbContext.ApiLogs.AsQueryable();

            if (dataInicio.HasValue)
                query = query.Where(l => l.Timestamp >= dataInicio.Value);
            if (dataFim.HasValue)
                query = query.Where(l => l.Timestamp <= dataFim.Value);
            if (!string.IsNullOrEmpty(userId))
                query = query.Where(l => l.UserId == userId);
            if (!string.IsNullOrEmpty(path))
                query = query.Where(l => l.Path.Contains(path));
            if (statusCode.HasValue)
                query = query.Where(l => l.StatusCode == statusCode.Value);
            if (apenasErros)
                query = query.Where(l => l.IsError);

            var logs = await query
                .OrderByDescending(l => l.Timestamp)
                .Take(Math.Min(limite, 500))
                .Select(l => new ApiLogResponseDTO
                {
                    Id = l.Id,
                    Timestamp = l.Timestamp,
                    Method = l.Method,
                    Path = l.Path,
                    QueryString = l.QueryString,
                    StatusCode = l.StatusCode,
                    ExceptionMessage = l.ExceptionMessage,
                    IsError = l.IsError,
                    ElapsedMs = l.ElapsedMs,
                    UserId = l.UserId,
                    RequestBody = l.RequestBody,
                    ResponseBody = l.ResponseBody,
                })
                .ToListAsync();

            return Ok(ApiResponseDTO<IEnumerable<ApiLogResponseDTO>>.SuccessResponse(logs));
        }
    }
}
