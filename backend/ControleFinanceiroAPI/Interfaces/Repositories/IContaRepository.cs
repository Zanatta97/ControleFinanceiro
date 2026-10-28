using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>Consultas de contas por autor ou por ambiente.</summary>
    public interface IContaRepository : IRepository<Conta>
    {
        /// <summary>Contas cadastradas pelo usuário, em qualquer ambiente. Sem chamadas no código atual.</summary>
        Task<IEnumerable<Conta>> GetAllByUsuarioAsync(string usuarioId);

        /// <summary>Contas do ambiente, sem rastreamento.</summary>
        Task<IEnumerable<Conta>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
