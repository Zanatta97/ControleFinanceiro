namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Dados de cadastro de usuário (usado pelo Admin).</summary>
    public class UsuarioRegisterDTO
    {
        public string Nome { get; set; }
        public string Email { get; set; }
        public string Senha { get; set; }        // texto puro — Identity faz o hash
        /// <summary>Confirmação da senha. O backend não compara com Senha; só o frontend (Admin) confere.</summary>
        public string ConfirmacaoSenha { get; set; }
    }
}
