using ControleFinanceiroAPI.DTO.Relatorio;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IRelatorioService
    {
        Task<ResumoFinanceiroResponseDTO> GetResumoAsync(Guid ambienteId, int mes, int ano);
        Task<IEnumerable<GastoPorCategoriaResponseDTO>> GetGastoPorCategoriaAsync(Guid ambienteId, int mes, int ano);
        Task<EvolucaoMensalResponseDTO> GetEvolucaoMensalAsync(Guid ambienteId, int ano);
        Task<IEnumerable<OrcamentoStatusResponseDTO>> GetOrcamentosStatusAsync(Guid ambienteId);
        Task<ExtratoContaResponseDTO> GetExtratoContaAsync(Guid ambienteId, Guid contaId, DateTime dataInicio, DateTime dataFim);
    }
}
