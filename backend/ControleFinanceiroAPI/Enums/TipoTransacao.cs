namespace ControleFinanceiroAPI.Enums
{
    /// <summary>
    /// Natureza da transação. Os valores numéricos são espelhados no frontend (types/api.ts).
    /// </summary>
    public enum TipoTransacao
    {
        Receita = 1,
        Despesa = 2,

        /// <summary>
        /// Move dinheiro entre duas contas do ambiente (ex.: pagamento da fatura do cartão). Não é receita
        /// nem despesa: fica fora dos totais dos relatórios para a compra no cartão não ser contada duas vezes
        /// (uma na compra, outra no pagamento). Exige ContaDestinoId.
        /// </summary>
        Transferencia = 3
    }
}
