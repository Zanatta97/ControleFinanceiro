using System.Text.Json;

namespace ControleFinanceiroAPI.DTO.Common
{
    /// <summary>
    /// Envelope padrão de todas as respostas da API, de sucesso e de erro (inclusive as geradas pelo
    /// ErrorHandlingMiddleware e pelo BloquearDemoAttribute). O frontend lê o conteúdo em <see cref="Dados"/>
    /// e a mensagem de falha em <see cref="ErrorMessage"/>.
    /// </summary>
    /// <typeparam name="T">Tipo do conteúdo devolvido em <see cref="Dados"/>.</typeparam>
    public class ApiResponseDTO<T>
    {
        /// <summary>
        /// Status HTTP repetido no corpo. Quem define o status real da resposta é o controller
        /// (StatusCode(...)); o valor aqui não é aplicado automaticamente.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>Mensagem de erro; vazia nas respostas de sucesso.</summary>
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Success { get; set; }

        /// <summary>Mensagem opcional de sucesso (ex.: "Conta excluída com sucesso.").</summary>
        public string? StatusMessage { get; set; }

        /// <summary>Momento em que a resposta foi montada, no horário local do servidor.</summary>
        public DateTime Timestamp { get; set; }
        public T? Dados { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

        /// <summary>Monta uma resposta de sucesso com o conteúdo em <see cref="Dados"/>.</summary>
        /// <param name="data">Conteúdo da resposta (pode ser null em operações sem retorno, ex.: exclusão).</param>
        /// <param name="statusCode">Status repetido no corpo; padrão 200.</param>
        /// <param name="statusMessage">Mensagem opcional para o usuário.</param>
        public static ApiResponseDTO<T> SuccessResponse(T data, int statusCode = 200, string? statusMessage = null)
        {
            return new ApiResponseDTO<T>
            {
                StatusCode = statusCode,
                Success = true,
                StatusMessage = statusMessage,
                Timestamp = DateTime.Now,
                Dados = data
            };
        }

        /// <summary>Monta uma resposta de erro, sem conteúdo em <see cref="Dados"/>.</summary>
        /// <param name="errorMessage">Mensagem exibida ao usuário pelo frontend.</param>
        /// <param name="statusCode">Status repetido no corpo; padrão 500.</param>
        public static ApiResponseDTO<T> ErrorResponse(string errorMessage, int statusCode = 500)
        {
            return new ApiResponseDTO<T>
            {
                StatusCode = statusCode,
                Success = false,
                ErrorMessage = errorMessage,
                Timestamp = DateTime.Now
            };
        }
    }
}
