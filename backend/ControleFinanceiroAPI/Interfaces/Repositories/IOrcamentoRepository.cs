using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>Consultas de orçamentos, sempre com a categoria carregada.</summary>
    public interface IOrcamentoRepository : IRepository<Orcamento>
    {
        /// <summary>Orçamentos cadastrados pelo usuário, em qualquer ambiente. Sem chamadas no código atual.</summary>
        Task<IEnumerable<Orcamento>> GetAllByUsuarioAsync(string usuarioId);

        /// <summary>Orçamentos do ambiente com o status informado.</summary>
        Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status);

        /// <summary>Orçamentos do ambiente, de qualquer status.</summary>
        Task<IEnumerable<Orcamento>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
