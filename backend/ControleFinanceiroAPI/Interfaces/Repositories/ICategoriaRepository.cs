using ControleFinanceiroAPI.Model;

namespace ControleFinanceiroAPI.Interfaces.Repositories
{
    /// <summary>Consultas de categorias por autor ou por ambiente.</summary>
    public interface ICategoriaRepository : IRepository<Categoria>
    {
        /// <summary>Categorias cadastradas pelo usuário, em qualquer ambiente. Sem chamadas no código atual.</summary>
        Task<IEnumerable<Categoria>> GetAllByUsuarioAsync(string usuarioId);

        /// <summary>Categorias do ambiente, sem rastreamento.</summary>
        Task<IEnumerable<Categoria>> GetAllByAmbienteAsync(Guid ambienteId);
    }
}
