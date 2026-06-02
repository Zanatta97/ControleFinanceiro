using ControleFinanceiroAPI.DTO.SaldoMensal;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface ISaldoMensalService
    {
        Task<IEnumerable<SaldoMensalContaResponseDTO>> GetByMesAsync(Guid ambienteId, int mes, int ano);
        Task SalvarSaldoInicialAsync(Guid ambienteId, Guid contaId, int mes, int ano, decimal saldoInicial);
        Task CalcularSaldoInicialAsync(Guid ambienteId, int mes, int ano);
    }
}
