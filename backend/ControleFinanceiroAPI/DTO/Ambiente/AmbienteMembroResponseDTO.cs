using ControleFinanceiroAPI.DTO.Usuario;

namespace ControleFinanceiroAPI.DTO.Ambiente
{
    /// <summary>Membro de um ambiente: dados públicos do usuário e o papel dele ("Dono", "Admin" ou "Membro").</summary>
    public class AmbienteMembroResponseDTO
    {
        public UsuarioResumoDTO? Usuario { get; set; }
        public string? Role { get; set; }
    }
}
