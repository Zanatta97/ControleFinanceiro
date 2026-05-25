namespace ControleFinanceiroAPI.Logging
{
    // Interface do serviço responsável por persistir logs no banco.
    // Usar uma interface aqui segue o mesmo padrão dos repositórios:
    // os middlewares dependem da abstração, não da implementação concreta.
    public interface IApiLogService
    {
        // Recebe todos os dados do request/response e salva um registro de ApiLog.
        // O parâmetro exception é opcional (null quando não houve erro).
        Task LogAsync(HttpContext context, string requestBody, string responseBody, int statusCode, long elapsedMs, Exception? exception = null);
    }
}
