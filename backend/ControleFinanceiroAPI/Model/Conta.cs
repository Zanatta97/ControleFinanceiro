using ControleFinanceiroAPI.Enums;
using System.ComponentModel.DataAnnotations;

namespace ControleFinanceiroAPI.Model
{
    /// <summary>
    /// Conta financeira do ambiente (corrente, poupança, investimento ou cartão de crédito).
    /// É a origem de toda transação e o destino das transferências.
    /// </summary>
    public class Conta
    {
        public Guid Id { get; set; }
        public string? Nome { get; set; }

        /// <summary>
        /// Tipo da conta. CartaoCredito muda o tratamento nos relatórios: as despesas nela são separadas
        /// em DespesasCartao e ela é a única aceita no relatório de fatura.
        /// </summary>
        public TipoConta TipoConta { get; set; }

        /// <summary>
        /// Saldo informado pelo usuário no cadastro/edição. Não é recalculado pelas transações:
        /// o saldo por mês vem de SaldoMensalConta + movimento da competência (SaldoMensalService).
        /// </summary>
        public decimal Saldo { get; set; }

        /// <summary>Usuário que cadastrou a conta (registro de autoria; o acesso é controlado pelo ambiente).</summary>
        public string? UsuarioId { get; set; }
        public Usuario? Usuario { get; set; }
        public Guid? AmbienteId { get; set; }
        public Ambiente? Ambiente { get; set; }
    }
}
