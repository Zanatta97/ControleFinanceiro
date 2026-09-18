using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Relatorio;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using System.Globalization;

namespace ControleFinanceiroAPI.Services
{
    public class RelatorioService : IRelatorioService
    {
        private readonly IUnityOfWork _repository;

        public RelatorioService(IUnityOfWork repository)
        {
            _repository = repository;
        }

        public async Task<ResumoFinanceiroResponseDTO> GetResumoAsync(Guid ambienteId, int mes, int ano)
        {
            var transacoes = await _repository.TransacaoRepository.GetByMesCompetenciaAsync(ambienteId, mes, ano);

            // Transferência (ex.: pagamento de fatura) só move dinheiro entre contas:
            // não é receita nem despesa, por isso os filtros abaixo a deixam de fora.
            var receitas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Receita).Sum(t => t.Valor);
            var despesas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).Sum(t => t.Valor);

            return new ResumoFinanceiroResponseDTO
            {
                Mes = mes,
                Ano = ano,
                TotalReceitas = receitas,
                TotalDespesas = despesas,
                Saldo = receitas - despesas
            };
        }

        public async Task<IEnumerable<GastoPorCategoriaResponseDTO>> GetGastoPorCategoriaAsync(Guid ambienteId, int mes, int ano)
        {
            var transacoes = await _repository.TransacaoRepository.GetByMesCompetenciaAsync(ambienteId, mes, ano);

            var despesas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();
            var totalGeral = despesas.Sum(t => t.Valor);

            return despesas
                .GroupBy(t => new { t.CategoriaId, t.Categoria?.Nome, t.Categoria?.Cor })
                .Select(g => new GastoPorCategoriaResponseDTO
                {
                    CategoriaId = g.Key.CategoriaId,
                    NomeCategoria = g.Key.Nome ?? "Sem categoria",
                    Cor = g.Key.Cor,
                    TotalGasto = g.Sum(t => t.Valor),
                    Percentual = totalGeral > 0
                        ? Math.Round(g.Sum(t => t.Valor) / totalGeral * 100, 2)
                        : 0
                })
                .OrderByDescending(g => g.TotalGasto)
                .ToList();
        }

        public async Task<EvolucaoMensalResponseDTO> GetEvolucaoMensalAsync(Guid ambienteId, int ano)
        {
            var inicio = new DateTime(ano, 1, 1);
            var fim = new DateTime(ano, 12, 31, 23, 59, 59);

            var transacoes = await _repository.TransacaoRepository.GetByPeriodoAsync(ambienteId, inicio, fim);

            var meses = Enumerable.Range(1, 12).Select(mes =>
            {
                var doMes = transacoes.Where(t => t.MesCompetencia.Month == mes && t.MesCompetencia.Year == ano).ToList();
                var receitas = doMes.Where(t => t.TipoTransacao == TipoTransacao.Receita).Sum(t => t.Valor);
                var despesas = doMes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).Sum(t => t.Valor);

                return new EvolucaoMensalItemDTO
                {
                    Mes = mes,
                    NomeMes = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(mes),
                    TotalReceitas = receitas,
                    TotalDespesas = despesas,
                    Saldo = receitas - despesas
                };
            }).ToList();

            return new EvolucaoMensalResponseDTO
            {
                Ano = ano,
                Meses = meses
            };
        }

        public async Task<IEnumerable<OrcamentoStatusResponseDTO>> GetOrcamentosStatusAsync(Guid ambienteId)
        {
            var orcamentos = await _repository.OrcamentoRepository.GetAllByAmbienteAsync(ambienteId);
            var todasTransacoes = await _repository.TransacaoRepository.GetAllByAmbienteAsync(ambienteId);

            var despesas = todasTransacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();

            return orcamentos.Select(o =>
            {
                var gasto = despesas
                    .Where(t => t.CategoriaId == o.CategoriaId && t.Data <= o.DataLimite)
                    .Sum(t => t.Valor);

                return new OrcamentoStatusResponseDTO
                {
                    Id = o.Id,
                    NomeOrcamento = o.Nome ?? string.Empty,
                    Descricao = o.Descricao,
                    CategoriaId = o.CategoriaId,
                    NomeCategoria = o.Categoria?.Nome ?? "Sem categoria",
                    ValorLimite = o.ValorLimite,
                    ValorGasto = gasto,
                    Percentual = o.ValorLimite > 0
                        ? Math.Round(gasto / o.ValorLimite * 100, 2)
                        : 0,
                    DataLimite = o.DataLimite,
                    Status = o.StatusOrcamento
                };
            }).OrderBy(o => o.Percentual).ToList();
        }

        public async Task<ExtratoContaResponseDTO> GetExtratoContaAsync(Guid ambienteId, Guid contaId, DateTime dataInicio, DateTime dataFim)
        {
            var conta = await _repository.ContaRepository.GetByIdReadOnlyAsync(c => c.Id == contaId && c.AmbienteId == ambienteId);
            if (conta is null)
                throw new KeyNotFoundException("Conta não encontrada.");

            var transacoes = await _repository.TransacaoRepository.GetByContaAndPeriodoAsync(contaId, ambienteId, dataInicio, dataFim);
            var lista = transacoes.ToList();

            // No extrato a transferência conta: saída na origem, entrada no destino
            var (entradas, saidas) = lista.CalcularMovimentoDaConta(contaId);

            return new ExtratoContaResponseDTO
            {
                ContaId = conta.Id,
                NomeConta = conta.Nome ?? string.Empty,
                TipoConta = conta.TipoConta,
                SaldoAtual = conta.Saldo,
                DataInicio = dataInicio,
                DataFim = dataFim,
                TotalEntradas = entradas,
                TotalSaidas = saidas,
                Transacoes = lista.Select(t => t.ToResponseDTO()!).ToList()
            };
        }
    }
}
