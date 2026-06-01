using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.DTO.Conta
{
    public class ContaRequestDTO
    {
        [Required(ErrorMessage = "O Nome da Conta é obrigatório")]
        public string Nome { get; set; } = string.Empty;
        [Required(ErrorMessage = "O Tipo da Conta é obrigatório")]
        public TipoConta TipoConta { get; set; }
        public decimal Saldo { get; set; } = 0;
    }
}
