using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Common.Extensions
{
    public static class TransacaoExtension
    {
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
