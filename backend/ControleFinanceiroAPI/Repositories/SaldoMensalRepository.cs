using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    public class SaldoMensalRepository : ISaldoMensalRepository
    {
        private readonly AppDbContext _context;

        public SaldoMensalRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<SaldoMensalConta?> GetAsync(Guid contaId, Guid ambienteId, DateOnly mes)
        {
            return await _context.SaldoMensalContas
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.ContaId == contaId && s.AmbienteId == ambienteId && s.Mes == mes);
        }

        public async Task<IEnumerable<SaldoMensalConta>> GetAllByAmbienteAsync(Guid ambienteId, DateOnly mes)
        {
            return await _context.SaldoMensalContas
                .AsNoTracking()
                .Where(s => s.AmbienteId == ambienteId && s.Mes == mes)
                .ToListAsync();
        }

        public void AddOrUpdate(SaldoMensalConta saldo)
        {
            var existing = _context.SaldoMensalContas
                .FirstOrDefault(s => s.ContaId == saldo.ContaId && s.AmbienteId == saldo.AmbienteId && s.Mes == saldo.Mes);

            if (existing is null)
            {
                saldo.Id = Guid.NewGuid();
                _context.SaldoMensalContas.Add(saldo);
            }
            else
            {
                existing.SaldoInicial = saldo.SaldoInicial;
                _context.SaldoMensalContas.Update(existing);
            }
        }
    }
}
