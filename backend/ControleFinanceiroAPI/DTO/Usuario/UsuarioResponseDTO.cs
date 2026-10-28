namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Dados do usuário. Sem uso nas respostas atuais da API (só em DTOMapper.ToResponseDTO).</summary>
    public class UsuarioResponseDTO
    {
        public string? Id { get; set; }
        public string? Nome { get; set; }
        public string? Email { get; set; }
        public DateTime DtaCriacao { get; set; }
        /// <summary>Preenchido pelo DTOMapper com o refresh token gravado no usuário (não é o access token).</summary>
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
    }
}
