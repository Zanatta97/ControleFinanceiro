using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    /// <summary>Implementação de <see cref="ICategoriaRepository"/> sobre o AppDbContext.</summary>
    public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(AppDbContext context) : base(context) { }

        /// <inheritdoc />
        public async Task<IEnumerable<Categoria>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Categoria>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }
}
