using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.DTO.Orcamento
{
    public class OrcamentoRequestDTO
    {
        [Required(ErrorMessage = "O Nome é obrigatório")]
        public string? Nome { get; set; }
        [Required(ErrorMessage = "O campo Descrição é obrigatório")]
        public string? Descricao { get; set; }
        [Required(ErrorMessage = "O campo Valor Limite é obrigatório")]
        public decimal ValorLimite { get; set; }
        [Required(ErrorMessage = "O campo Data Limite é obrigatório")]
        public DateTime DataLimite { get; set; }
        [Required(ErrorMessage = "O campo Status do Orçamento é obrigatório")]
        public StatusOrcamento StatusOrcamento { get; set; }
        [Required(ErrorMessage = "O campo Categoria é obrigatório")]
        public Guid CategoriaId { get; set; }
    }
}
