using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>
    /// Consultas de transações. As declaradas aqui filtram pelo ambiente e não rastreiam. "Período" filtra pela
    /// Data (calendário); "competência" filtra por MesCompetencia.
    /// </summary>
    public interface ITransacaoRepository : IRepository<Transacao>
    {
        /// <summary>Transações com origem na conta (ContaId), da mais recente para a mais antiga, com a categoria.</summary>
        Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId, Guid ambienteId);

        /// <summary>
        /// Transações de origem OU destino na conta, com Data no período (base do extrato).
        /// Fim sem hora vale o dia inteiro.
        /// </summary>
        Task<IEnumerable<Transacao>> GetByContaAndPeriodoAsync(Guid contaId, Guid ambienteId, DateTime inicio, DateTime fim);

        /// <summary>Transações com Data no período, com categoria e contas. Fim sem hora vale o dia inteiro.</summary>
        Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim);

        /// <summary>Transações de uma competência (mês/ano), com categoria e contas.</summary>
        Task<IEnumerable<Transacao>> GetByMesCompetenciaAsync(Guid ambienteId, int mes, int ano);

        /// <summary>Transações com MesCompetencia entre <paramref name="inicio"/> e <paramref name="fim"/>, inclusive.</summary>
        Task<IEnumerable<Transacao>> GetByIntervaloCompetenciaAsync(Guid ambienteId, DateOnly inicio, DateOnly fim);

        /// <summary>
        /// Transações da competência com origem OU destino na conta (base do saldo mensal e da fatura).
        /// Só a categoria é carregada.
        /// </summary>
        Task<IEnumerable<Transacao>> GetByMesCompetenciaEContaAsync(Guid ambienteId, Guid contaId, int mes, int ano);

        /// <summary>
        /// Transações de origem ou destino na conta a partir da competência informada. Sem chamadas no código atual.
        /// </summary>
        Task<IEnumerable<Transacao>> GetAPartirDeMesCompetenciaAsync(Guid ambienteId, Guid contaId, DateOnly mesInicio);

        /// <summary>
        /// Despesas cuja observação começa com "Parcela ", a partir da competência informada, em ordem de
        /// competência. É um pré-filtro: o formato "Parcela N/M" é confirmado depois, em memória.
        /// </summary>
        Task<IEnumerable<Transacao>> GetDespesasParceladasAPartirDeMesCompetenciaAsync(Guid ambienteId, DateOnly mesInicio);

        /// <summary>Transações de um tipo, da mais recente para a mais antiga.</summary>
        Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo);

        /// <summary>Todas as transações do ambiente, com categoria e contas, sem ordenação definida.</summary>
        Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
