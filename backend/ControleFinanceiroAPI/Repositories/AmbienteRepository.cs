using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    /// <summary>Implementação de <see cref="IAmbienteRepository"/> sobre o AppDbContext.</summary>
    public class AmbienteRepository : Repository<Ambiente>, IAmbienteRepository
    {

        public AmbienteRepository(AppDbContext context) : base(context) { }

        /// <summary>
        /// Todos os ambientes com membros e usuários carregados. Sobrescreve a versão genérica porque a
        /// tela de Admin mostra a contagem e a lista de membros de cada ambiente.
        /// </summary>
        public override async Task<IEnumerable<Ambiente>> GetAllAsync()
        {
            return await _context.Ambientes
                .Include(a => a.Membros)
                    .ThenInclude(m => m.Usuario)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Usuario>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.AmbienteMembros
                .Where(m => m.AmbienteId == ambienteId)
                .Include(m => m.Usuario)
                .Select(m => m.Usuario!)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Ambiente>> GetAllByUsuarioAsync(string usuarioId)
        {
            return await _context.AmbienteMembros
                .Where(m => m.UsuarioId == usuarioId)
                .Include(m => m.Ambiente)
                    .ThenInclude(a => a!.Membros)
                        .ThenInclude(m => m.Usuario)
                .Select(m => m.Ambiente!)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<AmbienteMembro?> GetMembroAsync(Guid ambienteId, string userId)
        {
            return await _context.AmbienteMembros
                .FirstOrDefaultAsync(m => m.AmbienteId == ambienteId && m.UsuarioId == userId);
        }

        /// <inheritdoc />
        public void AddMembro(AmbienteMembro membro)
        {
            _context.AmbienteMembros.Add(membro);
        }

        /// <inheritdoc />
        public void RemoveMembro(AmbienteMembro membro)
        {
            _context.AmbienteMembros.Remove(membro);
        }
    }
}
