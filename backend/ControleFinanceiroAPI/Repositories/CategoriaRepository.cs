using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    public class CategoriaRepository : Repository<Categoria>, ICategoriaRepository
    {
        public CategoriaRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Categoria>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Categoria>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.Categorias
                .AsNoTracking()
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }
}
