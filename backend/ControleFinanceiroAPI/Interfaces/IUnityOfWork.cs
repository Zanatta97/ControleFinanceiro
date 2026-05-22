namespace ControleFinanceiroAPI.Interfaces
{
    public interface IUnityOfWork : IDisposable
    {
        Task<int> SaveChangesAsync();
    }
}
