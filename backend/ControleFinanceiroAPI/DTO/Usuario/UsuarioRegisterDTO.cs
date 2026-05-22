namespace ControleFinanceiroAPI.DTO.Usuario
{
    public class UsuarioRegisterDTO
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }        // texto puro — Identity faz o hash
        public string ConfirmacaoSenha { get; set; }
    }
}
