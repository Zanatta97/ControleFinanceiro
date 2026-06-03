namespace ControleFinanceiroAPI.DTO.Usuario
{
    public class AlterarSenhaDTO
    {
        public required string SenhaAtual { get; set; }
        public required string NovaSenha { get; set; }
        public required string ConfirmacaoNovaSenha { get; set; }
    }
}
