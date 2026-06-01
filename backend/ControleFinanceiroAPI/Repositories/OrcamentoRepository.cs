using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
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

        public async Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status)
        {
            return await _context.Orcamentos
                .AsNoTracking()
                .Include(o => o.Categoria)
                .Where(o => o.AmbienteId == ambienteId && o.StatusOrcamento == status)
                .ToListAsync();
        }

        public async Task<IEnumerable<Orcamento>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.Orcamentos
                .AsNoTracking()
                .Include(o => o.Categoria)
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }
}
