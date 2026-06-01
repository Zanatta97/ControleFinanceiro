using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly IUnityOfWork _repository;
        private readonly ILogger<CategoriaService> _logger;

        public CategoriaService(IUnityOfWork repository, ILogger<CategoriaService> logger)
        {
            _repository = repository;
            _logger = logger;
        }

        public async Task<IEnumerable<Categoria>> GetAllAsync()
        {
            return await _repository.CategoriaRepository.GetAllAsync();
        }

        public async Task<IEnumerable<Categoria>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _repository.CategoriaRepository.GetAllByAmbienteAsync(ambienteId);
        }

        public async Task<Categoria?> GetAsync(Guid id, Guid ambienteId)
        {
            return await _repository.CategoriaRepository.GetByIdAsync(c => c.Id == id && c.AmbienteId == ambienteId);
        }

        public async Task<Categoria?> GetReadOnlyAsync(Guid id, Guid ambienteId)
        {
            return await _repository.CategoriaRepository.GetByIdReadOnlyAsync(c => c.Id == id && c.AmbienteId == ambienteId);
        }
        public async Task<Categoria> AddAsync(Categoria categoria)
        {
            ArgumentNullException.ThrowIfNull(categoria, nameof(categoria));
            try
            {
                _repository.CategoriaRepository.Add(categoria);
                await _repository.SaveChangesAsync();
                return categoria;

            } catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao adicionar a categoria");
                throw;
            }
        }
        public async Task<Categoria> UpdateAsync(Guid id, Categoria categoria, Guid ambienteId)
        {
            ArgumentNullException.ThrowIfNull(categoria, nameof(categoria));

            try
            {
                var categoriaExistente = await _repository.CategoriaRepository.GetByIdAsync(c => c.Id == id && c.AmbienteId == ambienteId);
                if (categoriaExistente == null)
                {
                    throw new KeyNotFoundException("Categoria não encontrada");
                }
                categoriaExistente.Nome = categoria.Nome;
                categoriaExistente.Cor = categoria.Cor;
                categoriaExistente.UrlIcone = categoria.UrlIcone;
                _repository.CategoriaRepository.Update(categoriaExistente);
                await _repository.SaveChangesAsync();
                return categoriaExistente;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao atualizar a categoria");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(Guid id, Guid ambienteId)
        {
            try
            {
                var categoriaExistente = await _repository.CategoriaRepository.GetByIdAsync(c => c.Id == id && c.AmbienteId == ambienteId);
                if (categoriaExistente == null)
                {
                    return false;
                }
                _repository.CategoriaRepository.Delete(categoriaExistente);
                await _repository.SaveChangesAsync();
                return true;

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ocorreu um erro ao deletar a categoria");
                throw;
            }
        }
    }
}
