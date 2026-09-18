using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    public class Transacao
    {
        public Guid Id { get; set; }
        public string? Descricao { get; set; }
        public decimal Valor { get; set; }
        public DateTime Data { get; set; }
        public string? Observacao { get; set; }
        public TipoTransacao TipoTransacao { get; set; }
        public Guid CategoriaId { get; set; }
        public Categoria? Categoria { get; set; }
        public Guid ContaId { get; set; }
        public Conta? Conta { get; set; }

        // Preenchida só quando TipoTransacao == Transferencia: é a conta que recebe o valor
        // (ex.: o cartão de crédito no pagamento da fatura). Receita e Despesa deixam nulo.
        public Guid? ContaDestinoId { get; set; }
        public Conta? ContaDestino { get; set; }
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
        public DateOnly MesCompetencia { get; set; }
    }
}
