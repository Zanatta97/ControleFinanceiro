namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Credenciais de login (e-mail e senha).</summary>
    public class UsuarioLoginRequestDTO
    {
        public string Email { get; set; }
        public string Senha { get; set; }
    }
}
