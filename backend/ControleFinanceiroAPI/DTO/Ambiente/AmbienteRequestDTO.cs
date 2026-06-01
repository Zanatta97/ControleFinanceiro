using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.DTO.Ambiente
{
    public class AmbienteRequestDTO
    {
        [Required(ErrorMessage = "O Nome do Ambiente é obrigatório")]
        [MaxLength(100, ErrorMessage = "O Nome deve ter no máximo 100 caracteres")]
        public string Nome { get; set; } = string.Empty;
    }
}
