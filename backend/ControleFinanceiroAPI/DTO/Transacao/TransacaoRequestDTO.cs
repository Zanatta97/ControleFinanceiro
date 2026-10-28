using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.DTO.Transacao
{
    /// <summary>Dados para criar ou editar uma transação. Ambiente e autor vêm do token.</summary>
    public class TransacaoRequestDTO
    {
        [Required(ErrorMessage = "O campo Descrição é obrigatório")]
        public string? Descricao { get; set; }

        /// <summary>
        /// Valor sem sinal (o sentido vem de TipoTransacao; não há validação de mínimo).
        /// Em compra parcelada é o valor de CADA parcela, não o total.
        /// </summary>
        [Required(ErrorMessage = "O campo Valor é obrigatório")]
        public decimal Valor { get; set; }
        
        /// <summary>Data da compra; repetida em todas as parcelas.</summary>
        [Required(ErrorMessage = "O campo Data é obrigatório")]
        public DateTime Data { get; set; }
        
        public string? Observacao { get; set; }
        
        [Required(ErrorMessage = "O campo Tipo de Transação é obrigatório")]
        public TipoTransacao TipoTransacao { get; set; }
        
        [Required(ErrorMessage = "O campo Categoria é obrigatório")]
        public Guid CategoriaId { get; set; }
        
        [Required(ErrorMessage = "O campo Conta é obrigatório")]
        public Guid ContaId { get; set; }

        // Obrigatória quando TipoTransacao == Transferencia; deve ficar nula em Receita e Despesa.
        public Guid? ContaDestinoId { get; set; }

        /// <summary>Competência da transação (ou da 1ª parcela). O frontend envia o dia 1 do mês.</summary>
        [Required(ErrorMessage = "O campo Mês de Competência é obrigatório")]
        public DateOnly MesCompetencia { get; set; }

        /// <summary>
        /// Quantidade de parcelas na criação: acima de 1 gera uma transação por parcela, em competências
        /// consecutivas, com "Parcela N/M" na observação.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "Parcelas deve ser ao menos 1")]
        public int Parcelas { get; set; } = 1;
    }
}
