namespace ControleFinanceiroAPI.Model
{
    // Entidade que representa um registro de log salvo no banco de dados.
    // Cada linha na tabela ApiLogs será um request que passou pela API.
    public class ApiLog
    {
        public int Id { get; set; }

        // Momento exato em que o request foi recebido (UTC para evitar problemas de fuso horário)
        public DateTime Timestamp { get; set; } = DateTime.Now;

        // Método HTTP: GET, POST, PUT, DELETE, etc.
        public string Method { get; set; } = string.Empty;

        // Rota acessada, ex: /api/categorias
        public string Path { get; set; } = string.Empty;

        // Parâmetros da URL, ex: ?pagina=1&limite=10
        public string? QueryString { get; set; }

        // Corpo do request (JSON enviado pelo cliente). Só é preenchido quando LogAllRequests = true
        public string? RequestBody { get; set; }

        // Código HTTP retornado: 200, 400, 401, 404, 500, etc.
        public int StatusCode { get; set; }

        // Corpo da resposta enviada ao cliente
        public string? ResponseBody { get; set; }

        // Mensagem da exceção, caso tenha ocorrido um erro não tratado
        public string? ExceptionMessage { get; set; }

        // Stack trace completo da exceção (útil para debugar o ponto exato do erro)
        public string? StackTrace { get; set; }

        // true quando StatusCode >= 400 ou quando houve exceção
        public bool IsError { get; set; }

        // Tempo total que o request levou para ser processado, em milissegundos
        public long ElapsedMs { get; set; }

        // Id do usuário autenticado que fez o request (null se não autenticado)
        public string? UserId { get; set; }
    }
}
