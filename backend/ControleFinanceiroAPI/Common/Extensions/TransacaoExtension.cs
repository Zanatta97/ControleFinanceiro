using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;
using System.Text.RegularExpressions;

namespace ControleFinanceiroAPI.Common.Extensions
{
    public static partial class TransacaoExtension
    {
        // Mesmo formato gravado pelo DTOMapper.ToEntity: "Parcela N/M" ou "Parcela N/M — <observação original>"
        [GeneratedRegex(@"^Parcela (\d+)/(\d+)")]
        private static partial Regex ParcelaRegex();

        /// <summary>
        /// Reconhece uma parcela pela observação ("Parcela N/M", gravada no cadastro da compra parcelada).
        /// Não há campo de parcela no modelo: se o usuário editar a observação e apagar esse prefixo,
        /// a transação deixa de ser reconhecida como parcela pelos relatórios.
        /// </summary>
        /// <param name="transacao">Transação a analisar.</param>
        /// <param name="atual">Número da parcela (N); 0 quando não é parcela.</param>
        /// <param name="total">Total de parcelas da compra (M); 0 quando não é parcela.</param>
        /// <returns>true quando a observação traz uma parcela válida (1 &lt;= N &lt;= M).</returns>
        public static bool TryGetParcela(this Transacao transacao, out int atual, out int total)
        {
            atual = 0;
            total = 0;

            if (string.IsNullOrEmpty(transacao.Observacao))
                return false;

            var match = ParcelaRegex().Match(transacao.Observacao);
            if (!match.Success
                || !int.TryParse(match.Groups[1].Value, out var n)
                || !int.TryParse(match.Groups[2].Value, out var m)
                || n < 1 || m < 1 || n > m)
                return false;

            atual = n;
            total = m;
            return true;
        }

        /// <summary>
        /// Atalho para quando só importa saber se a transação é parcela.
        /// </summary>
        public static bool IsParcela(this Transacao transacao) => transacao.TryGetParcela(out _, out _);

        /// <summary>
        /// Soma entradas e saídas do ponto de vista de uma conta.
        /// Receita entra, Despesa sai. Transferência sai da conta de origem (ContaId)
        /// e entra na conta de destino (ContaDestinoId).
        /// </summary>
        public static (decimal Entradas, decimal Saidas) CalcularMovimentoDaConta(this IEnumerable<Transacao> transacoes, Guid contaId)
        {
            decimal entradas = 0;
            decimal saidas = 0;

            foreach (var t in transacoes)
            {
                switch (t.TipoTransacao)
                {
                    case TipoTransacao.Receita when t.ContaId == contaId:
                        entradas += t.Valor;
                        break;
                    case TipoTransacao.Despesa when t.ContaId == contaId:
                        saidas += t.Valor;
                        break;
                    case TipoTransacao.Transferencia when t.ContaId == contaId:
                        saidas += t.Valor;
                        break;
                    case TipoTransacao.Transferencia when t.ContaDestinoId == contaId:
                        entradas += t.Valor;
                        break;
                }
            }

            return (entradas, saidas);
        }
    }
}
