using ControleFinanceiroAPI.Context;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Model;
using Microsoft.EntityFrameworkCore;

namespace ControleFinanceiroAPI.Repositories
{
    public class TransacaoRepository : Repository<Transacao>, ITransacaoRepository
    {
        public TransacaoRepository(AppDbContext context) : base(context) { }

        public async Task<IEnumerable<Transacao>> GetByContaAsync(Guid contaId)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.ContaId == contaId)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByContaAndPeriodoAsync(Guid contaId, Guid ambienteId, DateTime inicio, DateTime fim)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Where(t => t.ContaId == contaId && t.AmbienteId == ambienteId && t.Data >= inicio && t.Data <= fim)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByMesCompetenciaAsync(Guid ambienteId, int mes, int ano)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Where(t => t.AmbienteId == ambienteId
                         && t.MesCompetencia.Month == mes
                         && t.MesCompetencia.Year == ano)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByMesCompetenciaEContaAsync(Guid ambienteId, Guid contaId, int mes, int ano)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Where(t => t.AmbienteId == ambienteId
                         && t.ContaId == contaId
                         && t.MesCompetencia.Month == mes
                         && t.MesCompetencia.Year == ano)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetAPartirDeMesCompetenciaAsync(Guid ambienteId, Guid contaId, DateOnly mesInicio)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Where(t => t.AmbienteId == ambienteId
                         && t.ContaId == contaId
                         && t.MesCompetencia >= mesInicio)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Where(t => t.AmbienteId == ambienteId && t.Data >= inicio && t.Data <= fim)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Where(t => t.AmbienteId == ambienteId && t.TipoTransacao == tipo)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }
}
