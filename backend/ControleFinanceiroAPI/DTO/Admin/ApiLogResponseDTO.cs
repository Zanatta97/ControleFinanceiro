namespace ControleFinanceiroAPI.DTO.Admin
{
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
