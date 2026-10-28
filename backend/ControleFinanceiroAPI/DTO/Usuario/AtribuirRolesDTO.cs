namespace ControleFinanceiroAPI.DTO.Usuario
{
    /// <summary>Lista completa de roles do Identity que o usuário deve ter (substitui as atuais).</summary>
    public class AtribuirRolesDTO
    {
        public List<string> Roles { get; set; } = [];
    }
}
