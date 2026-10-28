using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// CRUD de categorias. Toda operação por id filtra também pelo ambiente: categoria de outro
    /// ambiente se comporta como inexistente.
    /// </summary>
    public interface ICategoriaService
    {
        /// <summary>Categorias de todos os ambientes (uso do Admin).</summary>
        Task<IEnumerable<Categoria>> GetAllAsync();

        /// <summary>Categorias do ambiente, sem rastreamento do EF.</summary>
        Task<IEnumerable<Categoria>> GetAllByAmbienteAsync(Guid ambienteId);

        /// <summary>Categoria do ambiente, rastreada pelo EF; null se não existir nele.</summary>
        Task<Categoria?> GetAsync(Guid id, Guid ambienteId);

        /// <summary>Igual a <see cref="GetAsync"/>, sem rastreamento (só leitura).</summary>
        Task<Categoria?> GetReadOnlyAsync(Guid id, Guid ambienteId);

        /// <summary>Grava a categoria. Ambiente e autor já devem vir preenchidos (DTOMapper.ToEntity).</summary>
        /// <exception cref="ArgumentNullException">Categoria null.</exception>
        Task<Categoria> AddAsync(Categoria categoria);

        /// <summary>Atualiza nome, cor e ícone.</summary>
        /// <exception cref="KeyNotFoundException">Categoria não encontrada no ambiente.</exception>
        Task<Categoria> UpdateAsync(Guid id, Categoria categoria, Guid ambienteId);

        /// <summary>
        /// Exclui a categoria. Pelas FKs em cascata do banco, as transações e os orçamentos dessa
        /// categoria são apagados junto.
        /// </summary>
        /// <returns>false quando a categoria não existe no ambiente.</returns>
        Task<bool> DeleteAsync(Guid id, Guid ambienteId);

    }
}
