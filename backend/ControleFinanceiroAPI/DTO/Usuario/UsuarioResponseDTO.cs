namespace ControleFinanceiroAPI.DTO.Usuario
{
    public class UsuarioResponseDTO
    {
        public string? Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public DateTime DtaCriacao { get; set; }
        public string? Token { get; set; }
    }
}
