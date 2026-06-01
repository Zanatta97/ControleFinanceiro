using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Services
{
    public class ContaService : IContaService
    {
        private readonly IUnityOfWork _repository;
        private readonly ILogger<ContaService> _logger;

        public ContaService(IUnityOfWork repository, ILogger<ContaService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Conta>> GetAllAsync()
        {
            return await _repository.ContaRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Conta>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _repository.ContaRepository.GetAllByAmbienteAsync(ambienteId);
        }

        public async Task<Conta?> GetAsync(Guid id, Guid ambienteId)
        {
            return await _repository.ContaRepository.GetByIdAsync(c => c.Id == id && c.AmbienteId == ambienteId);
        }

        public async Task<Conta?> GetReadOnlyAsync(Guid id, Guid ambienteId)
        {
            return await _repository.ContaRepository.GetByIdReadOnlyAsync(c => c.Id == id && c.AmbienteId == ambienteId);
        }

        public async Task<Conta> AddAsync(Conta conta)
        {
            ArgumentNullException.ThrowIfNull(conta, nameof(conta));
            try
            {
                _repository.ContaRepository.Add(conta);
                await _repository.SaveChangesAsync();
                return conta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao adicionar a conta");
                throw;
            }
        }

        public async Task<Conta> UpdateAsync(Guid id, Conta conta, Guid ambienteId)
        {
            ArgumentNullException.ThrowIfNull(conta, nameof(conta));
            try
            {
                var contaExistente = await _repository.ContaRepository.GetByIdAsync(c => c.Id == id && c.AmbienteId == ambienteId);
                if (contaExistente == null)
                    throw new KeyNotFoundException("Conta não encontrada");

                contaExistente.Nome = conta.Nome;
                contaExistente.TipoConta = conta.TipoConta;
                contaExistente.Saldo = conta.Saldo;

                _repository.ContaRepository.Update(contaExistente);
                await _repository.SaveChangesAsync();
                return contaExistente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao atualizar a conta");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid ambienteId)
        {
            try
            {
                var contaExistente = await _repository.ContaRepository.GetByIdAsync(c => c.Id == id && c.AmbienteId == ambienteId);
                if (contaExistente == null)
                    return false;

                _repository.ContaRepository.Delete(contaExistente);
                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao deletar a conta");
                throw;
            }
        }
    }
}
