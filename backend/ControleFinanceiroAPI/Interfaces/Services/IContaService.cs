using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// CRUD de contas. Toda operação por id filtra também pelo ambiente: conta de outro
    /// ambiente se comporta como inexistente.
    /// </summary>
    public interface IContaService
    {
        /// <summary>Contas de todos os ambientes (uso do Admin).</summary>
        Task<IEnumerable<Conta>> GetAllAsync();

        /// <summary>Contas do ambiente, sem rastreamento do EF.</summary>
        Task<IEnumerable<Conta>> GetAllByAmbienteAsync(Guid ambienteId);

        /// <summary>Conta do ambiente, rastreada pelo EF; null se não existir nele.</summary>
        Task<Conta?> GetAsync(Guid id, Guid ambienteId);

        /// <summary>Igual a <see cref="GetAsync"/>, sem rastreamento (só leitura).</summary>
        Task<Conta?> GetReadOnlyAsync(Guid id, Guid ambienteId);

        /// <summary>Grava a conta. Ambiente e autor já devem vir preenchidos (DTOMapper.ToEntity).</summary>
        /// <exception cref="ArgumentNullException">Conta null.</exception>
        Task<Conta> AddAsync(Conta conta);

        /// <summary>Atualiza nome, tipo e saldo informado.</summary>
        /// <exception cref="KeyNotFoundException">Conta não encontrada no ambiente.</exception>
        Task<Conta> UpdateAsync(Guid id, Conta conta, Guid ambienteId);

        /// <summary>
        /// Exclui a conta. As transações com origem nela e os saldos mensais dela são apagados em cascata
        /// pelo banco; se ela for destino de alguma transferência, o banco recusa a exclusão (FK Restrict)
        /// e o erro sobe como 500.
        /// </summary>
        /// <returns>false quando a conta não existe no ambiente.</returns>
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);
    }
}
