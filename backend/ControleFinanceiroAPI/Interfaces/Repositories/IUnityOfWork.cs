namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    public interface IUnityOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
    }
}
