using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.DTO.Categoria
{
    public class CategoriaRequestDTO
    {
        [Required(ErrorMessage = "O Nome da Categoria é obrigatório")]
        public string Nome { get; set; } = string.Empty;
        [Required(ErrorMessage = "A Cor da Categoria é obrigatória")]
        public string Cor { get; set; } = string.Empty;
        public string UrlIcone { get; set; } = string.Empty;
    }
}
