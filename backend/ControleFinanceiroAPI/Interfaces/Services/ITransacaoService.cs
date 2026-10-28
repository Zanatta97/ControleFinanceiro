using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// CRUD de transações, com a regra de coerência entre tipo e conta de destino (transferência).
    /// Toda operação por id filtra também pelo ambiente.
    /// </summary>
    /// <remarks>
    /// Só a conta de destino é conferida contra o ambiente; ContaId e CategoriaId são gravados como vieram.
    /// </remarks>
    public interface ITransacaoService
    {
        /// <summary>Transações de todos os ambientes (uso do Admin).</summary>
        Task<IEnumerable<Transacao>> GetAllAsync();

        /// <summary>Transações do ambiente, com categoria, conta e conta de destino carregadas.</summary>
        Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId);

        /// <summary>
        /// Transações com origem na conta (ContaId), da mais recente para a mais antiga. Transferências
        /// em que a conta é só o destino não entram; conta de outro ambiente devolve lista vazia.
        /// </summary>
        Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId, Guid ambienteId);

        /// <summary>
        /// Transações com Data no período. Se <paramref name="fim"/> vier sem hora, o dia final conta inteiro.
        /// </summary>
        Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim);

        /// <summary>Transações do ambiente de um tipo, da mais recente para a mais antiga.</summary>
        Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo);

        /// <summary>Transação do ambiente, rastreada pelo EF; null se não existir nele.</summary>
        Task<Transacao?> GetAsync(Guid id, Guid ambienteId);

        /// <summary>Igual a <see cref="GetAsync"/>, sem rastreamento (só leitura).</summary>
        Task<Transacao?> GetReadOnlyAsync(Guid id, Guid ambienteId);

        /// <summary>Valida a conta de destino e grava uma transação.</summary>
        /// <exception cref="ArgumentNullException">Transação null.</exception>
        /// <exception cref="InvalidOperationException">Tipo e conta de destino incoerentes (409).</exception>
        /// <exception cref="KeyNotFoundException">Conta de destino fora do ambiente (404).</exception>
        Task<Transacao> AddAsync(Transacao transacao);

        /// <summary>
        /// Grava as parcelas de uma compra em um único SaveChanges: ou todas entram, ou nenhuma.
        /// Todas são validadas antes de qualquer gravação.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tipo e conta de destino incoerentes (409).</exception>
        /// <exception cref="KeyNotFoundException">Conta de destino fora do ambiente (404).</exception>
        Task<IEnumerable<Transacao>> AddManyAsync(IEnumerable<Transacao> transacoes);

        /// <summary>
        /// Substitui os campos editáveis da transação (descrição, valor, data, observação, tipo, categoria,
        /// contas e competência). Edita só a transação indicada, não as demais parcelas da mesma compra.
        /// </summary>
        /// <exception cref="InvalidOperationException">Tipo e conta de destino incoerentes (409).</exception>
        /// <exception cref="KeyNotFoundException">Transação ou conta de destino não encontrada no ambiente (404).</exception>
        Task<Transacao> UpdateAsync(Guid id, Transacao transacao, Guid ambienteId);

        /// <summary>Exclui só a transação indicada (não as demais parcelas da compra).</summary>
        /// <returns>false quando a transação não existe no ambiente.</returns>
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);
    }
}
