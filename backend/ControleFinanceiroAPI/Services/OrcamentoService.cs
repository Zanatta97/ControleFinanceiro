using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Services
{
    public class OrcamentoService : IOrcamentoService
    {
        private readonly IUnityOfWork _repository;
        private readonly ILogger<OrcamentoService> _logger;

        public OrcamentoService(IUnityOfWork repository, ILogger<OrcamentoService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Orcamento>> GetAllAsync()
        {
            return await _repository.OrcamentoRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Orcamento>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _repository.OrcamentoRepository.GetAllByAmbienteAsync(ambienteId);
        }

        public async Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status)
        {
            return await _repository.OrcamentoRepository.GetByStatusAsync(ambienteId, status);
        }

        public async Task<Orcamento?> GetAsync(Guid id, Guid ambienteId)
        {
            return await _repository.OrcamentoRepository.GetByIdAsync(o => o.Id == id && o.AmbienteId == ambienteId);
        }

        public async Task<Orcamento?> GetReadOnlyAsync(Guid id, Guid ambienteId)
        {
            return await _repository.OrcamentoRepository.GetByIdReadOnlyAsync(o => o.Id == id && o.AmbienteId == ambienteId);
        }

        public async Task<Orcamento> AddAsync(Orcamento orcamento)
        {
            ArgumentNullException.ThrowIfNull(orcamento, nameof(orcamento));
            try
            {
                _repository.OrcamentoRepository.Add(orcamento);
                await _repository.SaveChangesAsync();
                return orcamento;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao adicionar o orçamento");
                throw;
            }
        }

        public async Task<Orcamento> UpdateAsync(Guid id, Orcamento orcamento, Guid ambienteId)
        {
            ArgumentNullException.ThrowIfNull(orcamento, nameof(orcamento));
            try
            {
                var orcamentoExistente = await _repository.OrcamentoRepository.GetByIdAsync(o => o.Id == id && o.AmbienteId == ambienteId);
                if (orcamentoExistente == null)
                    throw new KeyNotFoundException("Orçamento não encontrado");

                orcamentoExistente.Nome = orcamento.Nome;
                orcamentoExistente.Descricao = orcamento.Descricao;
                orcamentoExistente.ValorLimite = orcamento.ValorLimite;
                orcamentoExistente.DataLimite = orcamento.DataLimite;
                orcamentoExistente.StatusOrcamento = orcamento.StatusOrcamento;
                orcamentoExistente.CategoriaId = orcamento.CategoriaId;

                _repository.OrcamentoRepository.Update(orcamentoExistente);
                await _repository.SaveChangesAsync();
                return orcamentoExistente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao atualizar o orçamento");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid ambienteId)
        {
            try
            {
                var orcamentoExistente = await _repository.OrcamentoRepository.GetByIdAsync(o => o.Id == id && o.AmbienteId == ambienteId);
                if (orcamentoExistente == null)
                    return false;

                _repository.OrcamentoRepository.Delete(orcamentoExistente);
                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao deletar o orçamento");
                throw;
            }
        }
    }
}
