namespace ControleFinanceiroAPI.Enums
{
    /// <summary>
    /// Situação do orçamento. Atenção: o frontend usa Encerrado = 2 (types/api.ts), valor que não existe
    /// aqui; o binding aceita o número e ele é gravado como está, então orçamentos encerrados pela tela
    /// não aparecem em GET /api/Orcamento/status/0 (Inativo).
    /// </summary>
    public enum StatusOrcamento
    {
        Ativo = 1,
        Inativo = 0,
    }
}
