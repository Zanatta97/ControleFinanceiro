using ControleFinanceiroAPI.DTO.Relatorio;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    public interface IRelatorioService
    {
        Task<ResumoFinanceiroResponseDTO> GetResumoAsync(Guid ambienteId, int mes, int ano);
        Task<IEnumerable<GastoPorCategoriaResponseDTO>> GetGastoPorCategoriaAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                  DateTime dataInicial = default, DateTime dataFinal = default);
        Task<EvolucaoMensalResponseDTO> GetEvolucaoMensalAsync(Guid ambienteId, int ano);
        Task<IEnumerable<OrcamentoStatusResponseDTO>> GetOrcamentosStatusAsync(Guid ambienteId);
        Task<ExtratoContaResponseDTO> GetExtratoContaAsync(Guid ambienteId, Guid contaId, DateTime dataInicio, DateTime dataFim);
        Task<IEnumerable<GastosPorContaResponseDTO>> GetGastosPorContaAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                            DateTime dataInicial = default, DateTime dataFinal = default);
        Task<IEnumerable<ComparativoCategoriaResponseDTO>> GetComparativoCategoriaAsync(Guid ambienteId, int mes, int ano);
        Task<IEnumerable<MaiorDespesaResponseDTO>> GetMaioresDespesasAsync(Guid ambienteId, int quantidade = 10, int mes = 0, int ano = 0,
                                                                          DateTime dataInicial = default, DateTime dataFinal = default);
        Task<MatrizCategoriaMesResponseDTO> GetMatrizCategoriaMesAsync(Guid ambienteId, int ano);
        Task<IEnumerable<FrequenciaCategoriaResponseDTO>> GetFrequenciaTicketMedioAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                        DateTime dataInicial = default, DateTime dataFinal = default);
        Task<RitmoMesResponseDTO> GetRitmoMesAsync(Guid ambienteId, DateTime? dataReferencia = null);
        Task<FaturaCompetenciaResponseDTO> GetFaturaCompetenciaAsync(Guid ambienteId, Guid contaId, int mes, int ano);
        Task<ProjecaoParcelasResponseDTO> GetProjecaoParcelasAsync(Guid ambienteId, int? mes = null, int? ano = null, int meses = 12,
                                                                   DateTime? dataReferencia = null);
        Task<ProjecaoProximoMesResponseDTO> GetProjecaoProximoMesAsync(Guid ambienteId, int? mes = null, int? ano = null,
                                                                       IEnumerable<Guid>? categoriasFixas = null,
                                                                       IEnumerable<Guid>? categoriasRecebimento = null,
                                                                       DateTime? dataReferencia = null);
        Task<FixosRecebimentosResponseDTO> GetFixosRecebimentosAsync(Guid ambienteId, int mes, int ano,
                                                                     IEnumerable<Guid>? categoriasFixas,
                                                                     IEnumerable<Guid>? categoriasRecebimento);
    }
}
