namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Troca de senha do usuário logado.</summary>
    public class AlterarSenhaDTO
    {
        public required string SenhaAtual { get; set; }
        public required string NovaSenha { get; set; }
        public required string ConfirmacaoNovaSenha { get; set; }
    }
}
