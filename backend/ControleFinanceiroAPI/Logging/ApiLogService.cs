using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Model;
using System.Security.Claims;

namespace ControleFinanceiroAPI.Logging
{
    public class ApiLogService : IApiLogService
    {
        // IServiceScopeFactory permite criar um escopo de DI manualmente.
        // Precisamos disso porque o ApiLogService é Singleton (uma única instância para toda a vida da app),
        // mas o AppDbContext é Scoped (uma instância por request).
        // Singleton não pode depender diretamente de Scoped — causaria erro de lifetime.
        // A solução é criar um escopo novo toda vez que formos salvar um log.
        private readonly IServiceScopeFactory _scopeFactory;

        public ApiLogService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        public async Task LogAsync(HttpContext context, string requestBody, string responseBody, int statusCode, long elapsedMs, Exception? exception = null)
        {
            try
            {
                // Cria um escopo isolado: garante um DbContext "limpo" para o log,
                // independente do estado do DbContext que está processando o request.
                // Isso é crucial para casos onde o erro que queremos logar veio
                // justamente de uma falha no banco de dados — o DbContext original
                // pode estar em estado inválido, mas este aqui é novo.
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var log = new ApiLog
                {
                    Timestamp = DateTime.UtcNow,
                    Method = context.Request.Method,
                    Path = context.Request.Path,
                    QueryString = context.Request.QueryString.Value,
                    RequestBody = requestBody,
                    StatusCode = statusCode,
                    ResponseBody = responseBody,
                    ExceptionMessage = exception?.Message,
                    StackTrace = exception?.StackTrace,
                    // IsError é true se houve exceção OU se o status indica falha (4xx / 5xx)
                    IsError = exception != null || statusCode >= 400,
                    ElapsedMs = elapsedMs,
                    // Lê o Id do usuário do token JWT (se estiver autenticado)
                    UserId = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                };

                await dbContext.ApiLogs.AddAsync(log);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Logging nunca deve derrubar a aplicação.
                // Se por algum motivo o próprio log falhar (ex: banco fora do ar),
                // engolimos o erro silenciosamente para não impactar o usuário.
            }
        }
    }
}
