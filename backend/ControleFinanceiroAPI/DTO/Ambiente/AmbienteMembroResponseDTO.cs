using ControleFinanceiroAPI.DTO.Usuario;

namespace ControleFinanceiroAPI.DTO.Ambiente
{
    public class AmbienteMembroResponseDTO
    {
        public UsuarioResumoDTO? Usuario { get; set; }
        public string? Role { get; set; }
    }
}
