namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Usuário como aparece no painel de Admin, com roles e situação de bloqueio.</summary>
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
