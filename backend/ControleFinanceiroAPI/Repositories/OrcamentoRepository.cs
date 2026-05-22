using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    public class OrcamentoRepository : Repository<Orcamento>, IOrcamentoRepository
    {
        public OrcamentoRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Orcamento>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.Orcamentos
                .AsNoTracking()
                .Include(o => o.Categoria)
                .Where(o => o.UsuarioId == usuarioId)
                .ToListAsync();
        }

        public async Task<IEnumerable<Orcamento>> GetByStatusAsync(string usuarioId, StatusOrcamento status)
        {
            return await _context.Orcamentos
                .AsNoTracking()
                .Include(o => o.Categoria)
                .Where(o => o.UsuarioId == usuarioId && o.StatusOrcamento == status)
                .ToListAsync();
        }
    }
}
