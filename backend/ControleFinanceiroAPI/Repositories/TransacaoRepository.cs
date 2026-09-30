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
            var query = _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaDestino)
                // Transferência aparece no extrato da origem (saída) e no da conta destino (entrada)
                .Where(t => (t.ContaId == contaId || t.ContaDestinoId == contaId)
                         && t.AmbienteId == ambienteId && t.Data >= inicio);

            return await FiltrarFimDoPeriodo(query, fim)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        // A Data da transação é gravada com hora. Se o fim do período chega sem hora
        // (ex.: 2026-09-30T00:00), "<= fim" deixaria de fora o que foi lançado nesse dia;
        // nesse caso o fim vale como o dia inteiro, usando um limite exclusivo no dia seguinte.
        // Com hora informada, o fim é respeitado como veio.
        private static IQueryable<Transacao> FiltrarFimDoPeriodo(IQueryable<Transacao> query, DateTime fim)
        {
            if (fim.TimeOfDay == TimeSpan.Zero)
            {
                var limiteExclusivo = fim.Date.AddDays(1);
                return query.Where(t => t.Data < limiteExclusivo);
            }

            return query.Where(t => t.Data <= fim);
        }

        public async Task<IEnumerable<Transacao>> GetByMesCompetenciaAsync(Guid ambienteId, int mes, int ano)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaDestino)
                .Where(t => t.AmbienteId == ambienteId
                         && t.MesCompetencia.Month == mes
                         && t.MesCompetencia.Year == ano)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByIntervaloCompetenciaAsync(Guid ambienteId, DateOnly inicio, DateOnly fim)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaDestino)
                .Where(t => t.AmbienteId == ambienteId
                         && t.MesCompetencia >= inicio
                         && t.MesCompetencia <= fim)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByMesCompetenciaEContaAsync(Guid ambienteId, Guid contaId, int mes, int ano)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Where(t => t.AmbienteId == ambienteId
                         && (t.ContaId == contaId || t.ContaDestinoId == contaId)
                         && t.MesCompetencia.Month == mes
                         && t.MesCompetencia.Year == ano)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetAPartirDeMesCompetenciaAsync(Guid ambienteId, Guid contaId, DateOnly mesInicio)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Where(t => t.AmbienteId == ambienteId
                         && (t.ContaId == contaId || t.ContaDestinoId == contaId)
                         && t.MesCompetencia >= mesInicio)
                .ToListAsync();
        }

        // Pré-filtro no banco pelo prefixo "Parcela " (vira LIKE 'Parcela %'); quem confirma o formato
        // "Parcela N/M" é o TransacaoExtension.TryGetParcela, já em memória.
        public async Task<IEnumerable<Transacao>> GetDespesasParceladasAPartirDeMesCompetenciaAsync(Guid ambienteId, DateOnly mesInicio)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Where(t => t.AmbienteId == ambienteId
                         && t.TipoTransacao == TipoTransacao.Despesa
                         && t.MesCompetencia >= mesInicio
                         && t.Observacao != null
                         && t.Observacao.StartsWith("Parcela "))
                .OrderBy(t => t.MesCompetencia)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByPeriodoAsync(Guid ambienteId, DateTime inicio, DateTime fim)
        {
            var query = _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaDestino)
                .Where(t => t.AmbienteId == ambienteId && t.Data >= inicio);

            return await FiltrarFimDoPeriodo(query, fim)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetByTipoAsync(Guid ambienteId, TipoTransacao tipo)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaDestino)
                .Where(t => t.AmbienteId == ambienteId && t.TipoTransacao == tipo)
                .OrderByDescending(t => t.Data)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transacao>> GetAllByAmbienteAsync(Guid ambienteId)
        {
            return await _context.Transacoes
                .AsNoTracking()
                .Include(t => t.Categoria)
                .Include(t => t.Conta)
                .Include(t => t.ContaDestino)
                .Where(c => c.AmbienteId == ambienteId)
                .ToListAsync();
        }
    }
}
