using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    /// <summary>Implementação de <see cref="IContaRepository"/> sobre o AppDbContext.</summary>
    public class ContaRepository : Repository<Conta>, IContaRepository
    {
        public ContaRepository(AppDbContext context) : base(context) { }

        /// <inheritdoc />
        public async Task<IEnumerable<Conta>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.Contas
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Conta>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.Contas
                .AsNoTracking()
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }
}
