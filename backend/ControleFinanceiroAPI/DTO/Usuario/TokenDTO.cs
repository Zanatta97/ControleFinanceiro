namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Par de tokens: devolvido pela seleção de ambiente e pelo refresh, e enviado no refresh.</summary>
    public class TokenDTO
    {
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
    }
}
