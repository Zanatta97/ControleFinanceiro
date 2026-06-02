using Microsoft.AspNetCore.Identity;

namespace ControleFinanceiroAPI.Model
{
    public class Usuario : IdentityUser
    {
        public string? Nome { get; set; }
        public DateTime DtaCriacao { get; set; } = DateTime.Now;
        public string? RefreshToken { get; set; }
        public DateTime RefreshTokenExpiryTime { get; set; }
        public Guid? AmbienteAtivoId { get; set; }
        public Ambiente? AmbienteAtivo { get; set; }
    }
}
