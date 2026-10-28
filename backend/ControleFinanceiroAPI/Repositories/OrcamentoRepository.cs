using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    /// <summary>
    /// Implementação de <see cref="IOrcamentoRepository"/>. As consultas incluem a Categoria, cujo nome
    /// o relatório de status de orçamentos exibe.
    /// </summary>
    public class OrcamentoRepository : Repository<Orcamento>, IOrcamentoRepository
    {
        public OrcamentoRepository(AppDbContext context) : base(context) { }

        /// <inheritdoc />
        public async Task<IEnumerable<Orcamento>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.Orcamentos
                .AsNoTracking()
                .Include(o => o.Categoria)
                .Where(o => o.UsuarioId == usuarioId)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Orcamento>> GetByStatusAsync(Guid ambienteId, StatusOrcamento status)
        {
            return await _context.Orcamentos
                .AsNoTracking()
                .Include(o => o.Categoria)
                .Where(o => o.AmbienteId == ambienteId && o.StatusOrcamento == status)
                .ToListAsync();
        }

        /// <inheritdoc />
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
