using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.DTO.Transacao
{
    public class TransacaoRequestDTO
    {
        [Required(ErrorMessage = "O campo Descrição é obrigatório")]
        public string? Descricao { get; set; }

        [Required(ErrorMessage = "O campo Valor é obrigatório")]
        public decimal Valor { get; set; }
        
        [Required(ErrorMessage = "O campo Data é obrigatório")]
        public DateTime Data { get; set; }
        
        [Required(ErrorMessage = "O campo Observação é obrigatório")]
        public string? Observacao { get; set; }
        
        [Required(ErrorMessage = "O campo Tipo de Transação é obrigatório")]
        public TipoTransacao TipoTransacao { get; set; }
        
        [Required(ErrorMessage = "O campo Categoria é obrigatório")]
        public Guid CategoriaId { get; set; }
        
        [Required(ErrorMessage = "O campo Conta é obrigatório")]
        public Guid ContaId { get; set; }

        [Required(ErrorMessage = "O campo Mês de Competência é obrigatório")]
        public DateOnly MesCompetencia { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Parcelas deve ser ao menos 1")]
        public int Parcelas { get; set; } = 1;
    }
}
