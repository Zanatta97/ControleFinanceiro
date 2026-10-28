using ControleFinanceiroAPI.DTO.SaldoMensal;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// Saldo das contas por mês de competência: saldo inicial (gravado) + entradas − saídas (calculadas).
    /// </summary>
    public interface ISaldoMensalService
    {
        /// <summary>
        /// Saldo inicial, entradas, saídas e saldo final de cada conta do ambiente na competência.
        /// Conta sem saldo inicial gravado no mês começa em 0.
        /// </summary>
        Task<IEnumerable<SaldoMensalContaResponseDTO>> GetByMesAsync(Guid ambienteId, int mes, int ano);

        /// <summary>
        /// Grava (ou substitui) o saldo inicial digitado pelo usuário para a conta no mês.
        /// Não confere se a conta pertence ao ambiente.
        /// </summary>
        Task SalvarSaldoInicialAsync(Guid ambienteId, Guid contaId, int mes, int ano, decimal saldoInicial);

        /// <summary>
        /// Recalcula o saldo inicial de todas as contas no mês como o saldo final do mês anterior
        /// (saldo inicial gravado do mês anterior, ou 0, + movimento da competência anterior).
        /// Sobrescreve valores digitados manualmente.
        /// </summary>
        Task CalcularSaldoInicialAsync(Guid ambienteId, int mes, int ano);
    }
}
