using System.Text.Json;

namespace ControleFinanceiroAPI.DTO.Common
{
    public class ApiResponseDTO<T>
    {
        public int StatusCode { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public bool Success { get; set; }
        public string? StatusMessage { get; set; }
        public DateTime Timestamp { get; set; }
        public T? Dados { get; set; }

        public override string ToString()
        {
            return JsonSerializer.Serialize(this);
        }

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
