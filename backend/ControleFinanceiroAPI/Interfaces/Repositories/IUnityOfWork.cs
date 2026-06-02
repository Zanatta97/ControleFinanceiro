namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface IUnityOfWork : IDisposable
    {
        ICategoriaRepository CategoriaRepository { get; }
        IContaRepository ContaRepository { get; }
        IOrcamentoRepository OrcamentoRepository { get; }
        ITransacaoRepository TransacaoRepository { get; }
        IAmbienteRepository AmbienteRepository { get; }
        ISaldoMensalRepository SaldoMensalRepository { get; }
        Task<int> SaveChangesAsync();
    }
}
