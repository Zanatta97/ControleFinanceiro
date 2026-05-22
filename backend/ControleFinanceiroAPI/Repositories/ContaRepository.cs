using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    public class ContaRepository : Repository<Conta>, IContaRepository
    {
        public ContaRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Conta>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.Contas
                .AsNoTracking()
                .Where(c => c.UsuarioId == usuarioId)
                .ToListAsync();
        }
    }
}
