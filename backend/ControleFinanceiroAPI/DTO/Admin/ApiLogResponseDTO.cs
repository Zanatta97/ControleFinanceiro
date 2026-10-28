namespace ControleFinanceiroAPI.DTO.Admin
{
    /// <summary>Registro de log de requisição exibido na aba "Logs da API" do Admin (sem o stack trace).</summary>
    public class ApiLogResponseDTO
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string Method { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? QueryString { get; set; }
        public int StatusCode { get; set; }
        public string? ExceptionMessage { get; set; }
        public bool IsError { get; set; }
        public long ElapsedMs { get; set; }
        public string? UserId { get; set; }
        public string? RequestBody { get; set; }
        public string? ResponseBody { get; set; }
    }
}
