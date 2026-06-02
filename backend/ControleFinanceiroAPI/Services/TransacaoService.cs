using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Services
{
    public class TransacaoService : ITransacaoService
    {
        private readonly IUnityOfWork _repository;
        private readonly ILogger<TransacaoService> _logger;

        public TransacaoService(IUnityOfWork repository, ILogger<TransacaoService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Transacao>> GetAllAsync()
        {
            return await _repository.TransacaoRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _repository.TransacaoRepository.GetAllByAmbienteAsync(ambienteId);
        }

        public async Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId, Guid ambienteId)
        {
            return await _repository.TransacaoRepository.GetByContaAsync(contaId);
        }

        public async Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim)
        {
            return await _repository.TransacaoRepository.GetByPeriodoAsync(ambienteId, inicio, fim);
        }

        public async Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo)
        {
            return await _repository.TransacaoRepository.GetByTipoAsync(ambienteId, tipo);
        }

        public async Task<Transacao?> GetAsync(Guid id, Guid ambienteId)
        {
            return await _repository.TransacaoRepository.GetByIdAsync(t => t.Id == id && t.AmbienteId == ambienteId);
        }

        public async Task<Transacao?> GetReadOnlyAsync(Guid id, Guid ambienteId)
        {
            return await _repository.TransacaoRepository.GetByIdReadOnlyAsync(t => t.Id == id && t.AmbienteId == ambienteId);
        }

        public async Task<Transacao> AddAsync(Transacao transacao)
        {
            ArgumentNullException.ThrowIfNull(transacao, nameof(transacao));
            try
            {
                _repository.TransacaoRepository.Add(transacao);
                await _repository.SaveChangesAsync();
                return transacao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao adicionar a transação");
                throw;
            }
        }

        public async Task<IEnumerable<Transacao>> AddManyAsync(IEnumerable<Transacao> transacoes)
        {
            var lista = transacoes.ToList();
            ArgumentNullException.ThrowIfNull(lista, nameof(transacoes));
            try
            {
                foreach (var t in lista)
                    _repository.TransacaoRepository.Add(t);
                await _repository.SaveChangesAsync();
                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao adicionar as transações parceladas");
                throw;
            }
        }

        public async Task<Transacao> UpdateAsync(Guid id, Transacao transacao, Guid ambienteId)
        {
            ArgumentNullException.ThrowIfNull(transacao, nameof(transacao));
            try
            {
                var transacaoExistente = await _repository.TransacaoRepository.GetByIdAsync(t => t.Id == id && t.AmbienteId == ambienteId);
                if (transacaoExistente == null)
                    throw new KeyNotFoundException("Transação não encontrada");

                transacaoExistente.Descricao = transacao.Descricao;
                transacaoExistente.Valor = transacao.Valor;
                transacaoExistente.Data = transacao.Data;
                transacaoExistente.Observacao = transacao.Observacao;
                transacaoExistente.TipoTransacao = transacao.TipoTransacao;
                transacaoExistente.CategoriaId = transacao.CategoriaId;
                transacaoExistente.ContaId = transacao.ContaId;
                transacaoExistente.MesCompetencia = transacao.MesCompetencia;

                _repository.TransacaoRepository.Update(transacaoExistente);
                await _repository.SaveChangesAsync();
                return transacaoExistente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao atualizar a transação");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid ambienteId)
        {
            try
            {
                var transacaoExistente = await _repository.TransacaoRepository.GetByIdAsync(t => t.Id == id && t.AmbienteId == ambienteId);
                if (transacaoExistente == null)
                    return false;

                _repository.TransacaoRepository.Delete(transacaoExistente);
                await _repository.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao deletar a transação");
                throw;
            }
        }
    }
}
