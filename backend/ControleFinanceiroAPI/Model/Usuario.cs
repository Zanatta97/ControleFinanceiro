using Microsoft.AspNetCore.Identity;

namespace ControleFinanceiroAPI.Model
{
    public class Usuario : IdentityUser
    {
        public string? Nome { get; set; }
        public DateTime DtaCriacao { get; set; } = DateTime.UtcNow;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
    }
}
