using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    public class AmbienteRepository : Repository<Ambiente>, IAmbienteRepository
    {

        public AmbienteRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.AmbienteMembros
                .Where(m => m.AmbienteId == ambienteId)
                .Include(m => m.Usuario)
                .Select(m => m.Usuario!)
                .ToListAsync();
        }

        public async Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.AmbienteMembros
                .Where(m => m.UsuarioId == usuarioId)
                .Include(m => m.Ambiente)
                .Select(m => m.Ambiente!)
                .ToListAsync();
        }

        public async Task<AmbienteMembro?> GetMembroAsync(Guid ambienteId, string userId)
        {
            return await _context.AmbienteMembros
                .FirstOrDefaultAsync(m => m.AmbienteId == ambienteId && m.UsuarioId == userId);
        }

        public void AddMembro(AmbienteMembro membro)
        {
            _context.AmbienteMembros.Add(membro);
        }

        public void RemoveMembro(AmbienteMembro membro)
        {
            _context.AmbienteMembros.Remove(membro);
        }
    }
}
