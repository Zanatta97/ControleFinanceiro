namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>
    /// Agrupa os repositórios sobre um único DbContext (Scoped, um por requisição): as alterações feitas
    /// por qualquer repositório vão ao banco juntas, numa única transação, em <see cref="SaveChangesAsync"/>.
    /// </summary>
    public interface IUnityOfWork : IDisposable
    {
        ICategoriaRepository CategoriaRepository { get; }
        IContaRepository ContaRepository { get; }
        IOrcamentoRepository OrcamentoRepository { get; }
        ITransacaoRepository TransacaoRepository { get; }
        IAmbienteRepository AmbienteRepository { get; }
        ISaldoMensalRepository SaldoMensalRepository { get; }

        /// <summary>Grava no banco tudo o que foi marcado nos repositórios desde o último save.</summary>
        /// <returns>Quantidade de linhas afetadas.</returns>
        Task<int> SaveChangesAsync();
    }
}
