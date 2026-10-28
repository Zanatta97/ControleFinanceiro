using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// CRUD de orçamentos. Toda operação por id filtra também pelo ambiente. O consumo do orçamento
    /// não é calculado aqui, e sim no relatório de status (IRelatorioService.GetOrcamentosStatusAsync).
    /// </summary>
    public interface IOrcamentoService
    {
        /// <summary>Orçamentos de todos os ambientes (uso do Admin).</summary>
        Task<IEnumerable<Orcamento>> GetAllAsync();

        /// <summary>Orçamentos do ambiente, com a categoria carregada.</summary>
        Task<IEnumerable<Orcamento>> GetAllByAmbienteAsync(Guid ambienteId);

        /// <summary>Orçamentos do ambiente com o status informado, com a categoria carregada.</summary>
        Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status);

        /// <summary>Orçamento do ambiente, rastreado pelo EF; null se não existir nele.</summary>
        Task<Orcamento?> GetAsync(Guid id, Guid ambienteId);

        /// <summary>Igual a <see cref="GetAsync"/>, sem rastreamento (só leitura).</summary>
        Task<Orcamento?> GetReadOnlyAsync(Guid id, Guid ambienteId);

        /// <summary>
        /// Grava o orçamento. Ambiente e autor já devem vir preenchidos (DTOMapper.ToEntity).
        /// A categoria não é conferida contra o ambiente.
        /// </summary>
        /// <exception cref="ArgumentNullException">Orçamento null.</exception>
        Task<Orcamento> AddAsync(Orcamento orcamento);

        /// <summary>Atualiza todos os campos editáveis, inclusive status e categoria.</summary>
        /// <exception cref="KeyNotFoundException">Orçamento não encontrado no ambiente.</exception>
        Task<Orcamento> UpdateAsync(Guid id, Orcamento orcamento, Guid ambienteId);

        /// <summary>Exclui o orçamento.</summary>
        /// <returns>false quando o orçamento não existe no ambiente.</returns>
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);
    }
}
