using ControleFinanceiroAPI.Logging;
using System.Diagnostics;
using System.Text;

namespace ControleFinanceiroAPI.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;

        // LogSettingsManager é Singleton: a mesma instância registrada no Program.cs
        // é injetada aqui. Quando o LogsController chamar Ativar() ou Desativar(),
        // essa instância já refletirá o novo valor imediatamente.
        private readonly LogSettingsManager _logSettingsManager;

        public RequestLoggingMiddleware(RequestDelegate next, LogSettingsManager logSettingsManager)
        {
            _next = next;
            _logSettingsManager = logSettingsManager;
        }

        // O ASP.NET Core suporta injetar serviços Scoped diretamente no InvokeAsync.
        // Isso é necessário porque o middleware em si é Singleton,
        // mas IApiLogService precisa ser resolvido por request (para pegar o scope correto).
        public async Task InvokeAsync(HttpContext context, IApiLogService logService)
        {
            var stopwatch = Stopwatch.StartNew();

            // Lê o estado atual do toggle: true = loga tudo, false = só erros
            var logAll = _logSettingsManager.LogAllRequests;

            // EnableBuffering() permite que o Body da request seja lido mais de uma vez.
            // Por padrão o Body é um stream de leitura única — após o middleware ler,
            // o controller não conseguiria mais ler. Com buffering, podemos resetar a posição.
            context.Request.EnableBuffering();

            var requestBody = string.Empty;
            if (logAll)
            {
                requestBody = await ReadStreamAsync(context.Request.Body);
                // Reseta a posição para o início, para que o controller leia normalmente
                context.Request.Body.Position = 0;
            }

            // O stream de resposta padrão do ASP.NET é write-only (não permite leitura).
            // Para capturar o que foi escrito, substituímos temporariamente por um MemoryStream,
            // que permite leitura e escrita. Ao final, copiamos de volta para o stream original.
            var originalResponseBody = context.Response.Body;
            using var responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;

            try
            {
                // Continua o pipeline: passa pelo ErrorHandlingMiddleware e depois pelos controllers
                await _next(context);
            }
            finally
            {
                // O bloco finally garante que o log e a cópia da resposta sempre acontecem,
                // mesmo que algum middleware anterior tenha lançado uma exceção.
                stopwatch.Stop();

                // Lê o conteúdo que foi escrito no buffer (a resposta da API)
                responseBuffer.Position = 0;
                var responseBody = await ReadStreamAsync(responseBuffer);

                // Copia a resposta do buffer para o stream original (que vai ao cliente)
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalResponseBody);
                context.Response.Body = originalResponseBody;

                var isError = context.Response.StatusCode >= 400;

                // Decide se deve logar:
                // - LogAllRequests = true → loga tudo
                // - LogAllRequests = false → loga só erros (4xx e 5xx)
                var shouldLog = logAll || isError;

                if (shouldLog)
                {
                    // Recupera a exceção que o ErrorHandlingMiddleware pode ter armazenado
                    var exception = context.Items["Exception"] as Exception;

                    // Quando não está logando tudo, omite o body da request para
                    // evitar gravar dados sensíveis (senhas, tokens) em logs de erro
                    var logRequestBody = logAll ? requestBody : string.Empty;

                    await logService.LogAsync(
                        context,
                        logRequestBody,
                        responseBody,
                        context.Response.StatusCode,
                        stopwatch.ElapsedMilliseconds,
                        exception
                    );
                }
            }
        }

        private static async Task<string> ReadStreamAsync(Stream stream)
        {
            if (!stream.CanRead || stream.Length == 0)
                return string.Empty;

            // leaveOpen: true → não fecha o stream ao terminar a leitura,
            // pois ele ainda será usado (seja para resetar a posição ou copiar)
            using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
            return await reader.ReadToEndAsync();
        }
    }
}
