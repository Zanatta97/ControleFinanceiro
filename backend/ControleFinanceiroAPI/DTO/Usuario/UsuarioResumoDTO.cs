namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Dados públicos de um usuário (id, nome, e-mail), usados nas listas de membros.</summary>
    public class UsuarioResumoDTO
    {
        public string? Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
    }
}
