namespace ControleFinanceiroAPI.DTO.Usuario
{
    public class UsuarioAdminResponseDTO
    {
        public string Id { get; set; } = null!;
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public DateTime? DtaCriacao { get; set; }
        public bool Bloqueado { get; set; }
        public List<string> Roles { get; set; } = [];
    }
}
