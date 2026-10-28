using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Lançamento financeiro do ambiente: receita, despesa ou transferência entre contas.
    /// Cada parcela de uma compra parcelada é uma Transacao própria (ver DTOMapper.ToEntity).
    /// </summary>
    public class Transacao
    {
        public Guid Id { get; set; }
        public string? Descricao { get; set; }

        /// <summary>Valor sem sinal; o sentido (entrada/saída) vem de <see cref="TipoTransacao"/>.</summary>
        public decimal Valor { get; set; }

        /// <summary>
        /// Data da compra/lançamento (calendário). Nas parcelas cadastradas pelo app, todas guardam a data
        /// original da compra; o mês em que cada uma pesa é <see cref="MesCompetencia"/>.
        /// </summary>
        public DateTime Data { get; set; }

        /// <summary>
        /// Texto livre. Em compras parceladas começa com "Parcela N/M", e é por esse prefixo que os
        /// relatórios reconhecem a parcela (TransacaoExtension.TryGetParcela) — não há campo próprio.
        /// </summary>
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

        /// <summary>
        /// Mês ao qual a transação pertence financeiramente, independente de <see cref="Data"/>
        /// (ex.: compra no cartão em 15/06 com vencimento em 15/07 → competência 07). Resumo, evolução,
        /// saldo mensal e projeções filtram por ele.
        /// O frontend envia sempre o dia 1; os relatórios comparam só mês e ano.
        /// </summary>
        public DateOnly MesCompetencia { get; set; }
    }
}
