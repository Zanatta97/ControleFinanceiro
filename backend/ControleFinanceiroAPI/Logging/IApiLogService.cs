namespace ControleFinanceiroAPI.Logging
{
    // Interface do serviço responsável por persistir logs no banco.
    // Usar uma interface aqui segue o mesmo padrão dos repositórios:
    // os middlewares dependem da abstração, não da implementação concreta.
    /// <summary>Persistência dos logs de requisição (tabela ApiLogs).</summary>
    public interface IApiLogService
    {
        // Recebe todos os dados do request/response e salva um registro de ApiLog.
        // O parâmetro exception é opcional (null quando não houve erro).
        /// <summary>Grava um registro de ApiLog. Falhas na gravação são engolidas: o log nunca derruba a requisição.</summary>
        /// <param name="context">Requisição atual (método, rota, query e usuário).</param>
        /// <param name="requestBody">Corpo da requisição; vazio quando o log completo está desligado.</param>
        /// <param name="responseBody">Corpo da resposta enviada ao cliente.</param>
        /// <param name="statusCode">Status HTTP final.</param>
        /// <param name="elapsedMs">Duração da requisição, em milissegundos.</param>
        /// <param name="exception">Exceção capturada pelo ErrorHandlingMiddleware, se houve.</param>
        Task LogAsync(HttpContext context, string requestBody, string responseBody, int statusCode, long elapsedMs, Exception? exception = null);
    }
}
