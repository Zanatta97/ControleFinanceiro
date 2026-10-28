namespace ControleFinanceiroAPI.Enums
{
    /// <summary>
    /// Tipo da conta. Os valores numéricos são espelhados no frontend (types/api.ts) e precisam continuar iguais.
    /// CartaoCredito tem tratamento próprio nos relatórios (DespesasCartao, fatura por competência).
    /// </summary>
    public enum TipoConta
    {
        Corrente = 1,
        Poupanca = 2,
        Investimento = 3,
        CartaoCredito = 4
    }
}
