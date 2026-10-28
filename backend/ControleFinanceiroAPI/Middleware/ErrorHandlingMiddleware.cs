using ControleFinanceiroAPI.DTO.Common;
using System.Net;
using System.Text.Json;

namespace ControleFinanceiroAPI.Middleware
{
    /// <summary>
    /// Converte exceções não tratadas em resposta JSON no formato ApiResponseDTO. É a convenção que permite
    /// aos services sinalizar erro de negócio lançando exceção, sem montar a resposta HTTP:
    /// <list type="bullet">
    /// <item>KeyNotFoundException → 404 (registro não existe no ambiente)</item>
    /// <item>UnauthorizedAccessException → 403 (autenticado, mas sem permissão ou sem ambiente selecionado)</item>
    /// <item>InvalidOperationException → 409 (regra de negócio violada, ex.: transferência sem destino)</item>
    /// <item>ArgumentNullException → 400</item>
    /// <item>qualquer outra → 500 com mensagem genérica (a mensagem real não vai ao cliente; fica no ApiLog)</item>
    /// </list>
    /// </summary>
    /// <remarks>
    /// Comportamento atual: ArgumentException comum (não Null) cai no 500. Só o RelatorioController a captura
    /// e devolve 400; nos demais endpoints (ex.: DateOnly com mês inválido no SaldoMensal) vira 500.
    /// Tipos derivados também casam: ObjectDisposedException, por exemplo, é InvalidOperationException → 409.
    /// </remarks>
    public class ErrorHandlingMiddleware
    {
        // _next representa o próximo componente no pipeline do ASP.NET Core.
        // Chamar _next(context) é como dizer "passa o request para o próximo middleware ou controller".
        private readonly RequestDelegate _next;

        public ErrorHandlingMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Continua o pipeline normalmente
                await _next(context);
            }
            catch (Exception ex)
            {
                // Chegamos aqui quando uma exceção NÃO tratada sobe pelo pipeline.
                // Ex: NullReferenceException, DbException, etc. que nenhum controller capturou.

                // Armazena a exceção no dicionário do HttpContext para que o
                // RequestLoggingMiddleware (que é mais externo) consiga acessá-la
                // e incluir a mensagem e stack trace no registro de log.
                context.Items["Exception"] = ex;

                await HandleExceptionAsync(context);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context)
        {
            context.Response.ContentType = "application/json";

            var exception = context.Items["Exception"] as Exception;

            // A ordem importa: o switch para no primeiro tipo compatível, inclusive por herança. Um caso para
            // ArgumentException acrescentado acima de ArgumentNullException passaria a capturar as duas.
            var (statusCode, message) = exception switch
            {
                KeyNotFoundException ex         => ((int)HttpStatusCode.NotFound, ex.Message),
                UnauthorizedAccessException ex  => ((int)HttpStatusCode.Forbidden, ex.Message),
                InvalidOperationException ex    => ((int)HttpStatusCode.Conflict, ex.Message),
                ArgumentNullException ex        => ((int)HttpStatusCode.BadRequest, ex.Message),
                _                              => ((int)HttpStatusCode.InternalServerError,
                                                    "Erro interno do servidor. Por favor, tente novamente mais tarde.")
            };

            context.Response.StatusCode = statusCode;

            var response = ApiResponseDTO<object>.ErrorResponse(message, statusCode);

            var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(json);
        }
    }
}
