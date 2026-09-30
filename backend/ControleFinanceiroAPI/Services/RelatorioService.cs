using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Relatorio;
using ControleFinanceiroAPI.Enums;
using ControleFinanceiroAPI.Interfaces.Repositories;
using ControleFinanceiroAPI.Interfaces.Services;
using ControleFinanceiroAPI.Model;
using System.Globalization;

namespace ControleFinanceiroAPI.Services
{
    public class RelatorioService : IRelatorioService
    {
        private const int QuantidadeMaximaMaioresDespesas = 100;
        private const int AnoMinimo = 1900;
        private const int AnoMaximo = 2100;
        private const int QuantidadeMaximaMesesProjecao = 36;
        private const int MesesHistoricoEstimativa = 3;
        private const string GrupoFixo = "Fixo";
        private const string GrupoRecebimento = "Recebimento";

        private readonly IUnityOfWork _repository;

        // Parcela já reconhecida pela observação, com a competência normalizada para o dia 1
        private sealed record ParcelaLancada(Transacao Transacao, DateOnly Competencia, int Numero, int Total);

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
            var despesasMes = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();

            // Despesa em conta do tipo cartão de crédito vai para DespesasCartao; o resto, para DespesasOutras
            var despesasCartao = despesasMes.Where(t => t.Conta?.TipoConta == TipoConta.CartaoCredito).Sum(t => t.Valor);
            var despesasOutras = despesasMes.Where(t => t.Conta?.TipoConta != TipoConta.CartaoCredito).Sum(t => t.Valor);
            var despesas = despesasCartao + despesasOutras;

            return new ResumoFinanceiroResponseDTO
            {
                Mes = mes,
                Ano = ano,
                TotalReceitas = receitas,
                TotalDespesas = despesas,
                DespesasCartao = despesasCartao,
                DespesasOutras = despesasOutras,
                Saldo = receitas - despesas
            };
        }

        public async Task<IEnumerable<GastoPorCategoriaResponseDTO>> GetGastoPorCategoriaAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                               DateTime dataInicial = default, DateTime dataFinal = default)
        {
            var despesas = await GetDespesasPorCompetenciaOuPeriodoAsync(ambienteId, mes, ano, dataInicial, dataFinal);
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
            // Busca pela competência, não pela Data: parcelas de compras feitas no ano anterior
            // guardam a Data original, mas a competência cai no ano consultado.
            // O repositório inclui o limite final (MesCompetencia <= fim).
            var inicio = new DateOnly(ano, 1, 1);
            var fim = new DateOnly(ano, 12, 31);

            var transacoes = await _repository.TransacaoRepository.GetByIntervaloCompetenciaAsync(ambienteId, inicio, fim);

            var meses = Enumerable.Range(1, 12).Select(mes =>
            {
                var doMes = transacoes.Where(t => t.MesCompetencia.Month == mes && t.MesCompetencia.Year == ano).ToList();
                var receitas = doMes.Where(t => t.TipoTransacao == TipoTransacao.Receita).Sum(t => t.Valor);
                var despesasMes = doMes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();
                var despesasCartao = despesasMes.Where(t => t.Conta?.TipoConta == TipoConta.CartaoCredito).Sum(t => t.Valor);
                var despesasOutras = despesasMes.Where(t => t.Conta?.TipoConta != TipoConta.CartaoCredito).Sum(t => t.Valor);
                var despesas = despesasCartao + despesasOutras;

                return new EvolucaoMensalItemDTO
                {
                    Mes = mes,
                    NomeMes = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat.GetMonthName(mes),
                    TotalReceitas = receitas,
                    TotalDespesas = despesas,
                    DespesasCartao = despesasCartao,
                    DespesasOutras = despesasOutras,
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

        public async Task<IEnumerable<GastosPorContaResponseDTO>> GetGastosPorContaAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                         DateTime dataInicial = default, DateTime dataFinal = default)
        {
            var despesas = await GetDespesasPorCompetenciaOuPeriodoAsync(ambienteId, mes, ano, dataInicial, dataFinal);
            var totalGeral = despesas.Sum(t => t.Valor);

            return despesas
                    .GroupBy(t => new { t.ContaId, t.Conta?.Nome, t.Conta?.TipoConta })
                    .Select(g => new GastosPorContaResponseDTO
                    {
                        ContaId = g.Key.ContaId,
                        NomeConta = g.Key.Nome ?? "Sem conta",
                        TipoConta = g.Key.TipoConta,
                        TotalGasto = g.Sum(t => t.Valor),
                        Percentual = totalGeral > 0
                            ? Math.Round(g.Sum(t => t.Valor) / totalGeral * 100, 2)
                            : 0
                    })
                    .OrderByDescending(g => g.TotalGasto)
                    .ToList();
        }

        public async Task<IEnumerable<ComparativoCategoriaResponseDTO>> GetComparativoCategoriaAsync(Guid ambienteId, int mes, int ano)
        {
            ValidarCompetencia(mes, ano);

            var competencia = new DateOnly(ano, mes, 1);
            var competencias = new[]
            {
                competencia,                // M
                competencia.AddMonths(-1),  // M-1
                competencia.AddMonths(-2),  // M-2
                competencia.AddMonths(-3)   // M-3
            };

            // Uma consulta só: do dia 1 de M-3 até o último dia de M
            var inicio = competencias[3];
            var fim = competencia.AddMonths(1).AddDays(-1);
            var transacoes = await _repository.TransacaoRepository.GetByIntervaloCompetenciaAsync(ambienteId, inicio, fim);
            var despesas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();

            return despesas
                    .GroupBy(t => new { t.CategoriaId, t.Categoria?.Nome, t.Categoria?.Cor })
                    .Select(g =>
                    {
                        var porMes = competencias
                            .Select(c => g.Where(t => t.MesCompetencia.Year == c.Year && t.MesCompetencia.Month == c.Month)
                                          .Sum(t => t.Valor))
                            .ToArray();

                        var gastoMes = porMes[0];
                        var gastoMesAnterior = porMes[1];

                        // Média simples dos três meses anteriores; mês sem despesa entra como zero
                        var media = Math.Round((porMes[1] + porMes[2] + porMes[3]) / 3, 2);

                        return new ComparativoCategoriaResponseDTO
                        {
                            CategoriaId = g.Key.CategoriaId,
                            NomeCategoria = g.Key.Nome ?? "Sem categoria",
                            Cor = g.Key.Cor,
                            GastoMes = gastoMes,
                            GastoMesAnterior = gastoMesAnterior,
                            MediaTresMesesAnteriores = media,
                            VariacaoMesAnterior = CalcularVariacao(gastoMes, gastoMesAnterior),
                            VariacaoMedia = CalcularVariacao(gastoMes, media)
                        };
                    })
                    .OrderByDescending(c => c.GastoMes)
                    .ThenByDescending(c => c.MediaTresMesesAnteriores)
                    .ToList();
        }

        public async Task<IEnumerable<MaiorDespesaResponseDTO>> GetMaioresDespesasAsync(Guid ambienteId, int quantidade = 10, int mes = 0, int ano = 0,
                                                                                       DateTime dataInicial = default, DateTime dataFinal = default)
        {
            if (quantidade < 1 || quantidade > QuantidadeMaximaMaioresDespesas)
                throw new ArgumentException($"A quantidade deve estar entre 1 e {QuantidadeMaximaMaioresDespesas}.");

            var despesas = await GetDespesasPorCompetenciaOuPeriodoAsync(ambienteId, mes, ano, dataInicial, dataFinal);

            return despesas
                    .OrderByDescending(t => t.Valor)
                    .ThenByDescending(t => t.Data)
                    .Take(quantidade)
                    .Select(t => new MaiorDespesaResponseDTO
                    {
                        TransacaoId = t.Id,
                        Descricao = t.Descricao,
                        Valor = t.Valor,
                        Data = t.Data,
                        MesCompetencia = t.MesCompetencia,
                        CategoriaId = t.CategoriaId,
                        NomeCategoria = t.Categoria?.Nome ?? "Sem categoria",
                        Cor = t.Categoria?.Cor,
                        ContaId = t.ContaId,
                        NomeConta = t.Conta?.Nome ?? "Sem conta"
                    })
                    .ToList();
        }

        public async Task<MatrizCategoriaMesResponseDTO> GetMatrizCategoriaMesAsync(Guid ambienteId, int ano)
        {
            ValidarAno(ano);

            var inicio = new DateOnly(ano, 1, 1);
            var fim = new DateOnly(ano, 12, 31);
            var transacoes = await _repository.TransacaoRepository.GetByIntervaloCompetenciaAsync(ambienteId, inicio, fim);
            var despesas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();

            var formato = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat;

            var categorias = despesas
                    .GroupBy(t => new { t.CategoriaId, t.Categoria?.Nome, t.Categoria?.Cor })
                    .Select(g => new MatrizCategoriaLinhaDTO
                    {
                        CategoriaId = g.Key.CategoriaId,
                        NomeCategoria = g.Key.Nome ?? "Sem categoria",
                        Cor = g.Key.Cor,
                        Meses = Enumerable.Range(1, 12).Select(m => new MatrizCategoriaValorMesDTO
                        {
                            Mes = m,
                            NomeMes = formato.GetMonthName(m),
                            Valor = g.Where(t => t.MesCompetencia.Month == m).Sum(t => t.Valor)
                        }).ToList(),
                        TotalAno = g.Sum(t => t.Valor)
                    })
                    .OrderByDescending(c => c.TotalAno)
                    .ToList();

            var totaisPorMes = Enumerable.Range(1, 12).Select(m => new MatrizCategoriaValorMesDTO
            {
                Mes = m,
                NomeMes = formato.GetMonthName(m),
                Valor = despesas.Where(t => t.MesCompetencia.Month == m).Sum(t => t.Valor)
            }).ToList();

            return new MatrizCategoriaMesResponseDTO
            {
                Ano = ano,
                Categorias = categorias,
                TotaisPorMes = totaisPorMes,
                TotalAno = despesas.Sum(t => t.Valor)
            };
        }

        public async Task<IEnumerable<FrequenciaCategoriaResponseDTO>> GetFrequenciaTicketMedioAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                                     DateTime dataInicial = default, DateTime dataFinal = default)
        {
            var despesas = await GetDespesasPorCompetenciaOuPeriodoAsync(ambienteId, mes, ano, dataInicial, dataFinal);

            return despesas
                    .GroupBy(t => new { t.CategoriaId, t.Categoria?.Nome, t.Categoria?.Cor })
                    .Select(g =>
                    {
                        var quantidade = g.Count();
                        var total = g.Sum(t => t.Valor);

                        return new FrequenciaCategoriaResponseDTO
                        {
                            CategoriaId = g.Key.CategoriaId,
                            NomeCategoria = g.Key.Nome ?? "Sem categoria",
                            Cor = g.Key.Cor,
                            Quantidade = quantidade,
                            TotalGasto = total,
                            TicketMedio = Math.Round(total / quantidade, 2)
                        };
                    })
                    .OrderByDescending(f => f.Quantidade)
                    .ThenByDescending(f => f.TotalGasto)
                    .ToList();
        }

        public async Task<RitmoMesResponseDTO> GetRitmoMesAsync(Guid ambienteId, DateTime? dataReferencia = null)
        {
            // Este relatório usa a DATA da transação (calendário), não a competência
            var hoje = (dataReferencia ?? DateTime.Today).Date;
            var fimHoje = hoje.AddDays(1).AddTicks(-1);

            var inicioMes = new DateTime(hoje.Year, hoje.Month, 1);
            var diasNoMes = DateTime.DaysInMonth(hoje.Year, hoje.Month);
            var diasDecorridos = hoje.Day;

            // Mês anterior mais curto (ex.: hoje é 31/03 e fevereiro tem 28 dias): compara até o último dia dele
            var inicioMesAnterior = inicioMes.AddMonths(-1);
            var diasNoMesAnterior = DateTime.DaysInMonth(inicioMesAnterior.Year, inicioMesAnterior.Month);
            var diaComparado = Math.Min(hoje.Day, diasNoMesAnterior);
            var fimComparadoMesAnterior = inicioMesAnterior.AddDays(diaComparado).AddTicks(-1);

            // Uma consulta só: do dia 1 do mês anterior até o fim do dia de hoje
            var transacoes = await _repository.TransacaoRepository.GetByPeriodoAsync(ambienteId, inicioMesAnterior, fimHoje);
            var despesas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();

            var gastoAteHoje = despesas
                .Where(t => t.Data >= inicioMes && t.Data <= fimHoje)
                .Sum(t => t.Valor);
            var gastoMesAnteriorAteMesmoDia = despesas
                .Where(t => t.Data >= inicioMesAnterior && t.Data <= fimComparadoMesAnterior)
                .Sum(t => t.Valor);
            var gastoTotalMesAnterior = despesas
                .Where(t => t.Data >= inicioMesAnterior && t.Data < inicioMes)
                .Sum(t => t.Valor);

            return new RitmoMesResponseDTO
            {
                DataReferencia = hoje,
                DiasDecorridos = diasDecorridos,
                DiasNoMes = diasNoMes,
                GastoAteHoje = gastoAteHoje,
                DiaComparadoMesAnterior = diaComparado,
                GastoMesAnteriorAteMesmoDia = gastoMesAnteriorAteMesmoDia,
                VariacaoPercentual = CalcularVariacao(gastoAteHoje, gastoMesAnteriorAteMesmoDia),
                GastoTotalMesAnterior = gastoTotalMesAnterior,
                ProjecaoFimMes = Math.Round(gastoAteHoje / diasDecorridos * diasNoMes, 2)
            };
        }

        public async Task<FaturaCompetenciaResponseDTO> GetFaturaCompetenciaAsync(Guid ambienteId, Guid contaId, int mes, int ano)
        {
            ValidarCompetencia(mes, ano);

            var conta = await _repository.ContaRepository.GetByIdReadOnlyAsync(c => c.Id == contaId && c.AmbienteId == ambienteId);
            if (conta is null)
                throw new KeyNotFoundException("Conta não encontrada.");

            if (conta.TipoConta != TipoConta.CartaoCredito)
                throw new ArgumentException("A conta informada não é um cartão de crédito.");

            // Traz o que sai do cartão (despesas) e o que entra nele (transferências de pagamento)
            var transacoes = await _repository.TransacaoRepository.GetByMesCompetenciaEContaAsync(ambienteId, contaId, mes, ano);

            var totalDespesas = transacoes
                .Where(t => t.TipoTransacao == TipoTransacao.Despesa && t.ContaId == contaId)
                .Sum(t => t.Valor);

            var totalPago = transacoes
                .Where(t => t.TipoTransacao == TipoTransacao.Transferencia && t.ContaDestinoId == contaId)
                .Sum(t => t.Valor);

            return new FaturaCompetenciaResponseDTO
            {
                ContaId = conta.Id,
                NomeConta = conta.Nome ?? string.Empty,
                Mes = mes,
                Ano = ano,
                TotalDespesas = totalDespesas,
                TotalPago = totalPago,
                SaldoEmAberto = totalDespesas - totalPago
            };
        }

        public async Task<ProjecaoParcelasResponseDTO> GetProjecaoParcelasAsync(Guid ambienteId, int? mes = null, int? ano = null, int meses = 12,
                                                                                DateTime? dataReferencia = null)
        {
            if (meses < 1 || meses > QuantidadeMaximaMesesProjecao)
                throw new ArgumentException($"A quantidade de meses deve estar entre 1 e {QuantidadeMaximaMesesProjecao}.");

            var hoje = (dataReferencia ?? DateTime.Today).Date;
            var referencia = ResolverCompetencia(mes, ano, new DateOnly(hoje.Year, hoje.Month, 1));

            // As parcelas futuras já existem no banco (o cadastro grava uma transação por competência).
            // Busca da referência em diante, sem limite final: a lista de compras precisa da última parcela,
            // mesmo que ela caia depois do horizonte pedido.
            var transacoes = await _repository.TransacaoRepository.GetDespesasParceladasAPartirDeMesCompetenciaAsync(ambienteId, referencia);

            var parcelas = transacoes
                .Select(t => t.TryGetParcela(out var numero, out var total)
                    ? new ParcelaLancada(t, NormalizarCompetencia(t.MesCompetencia), numero, total)
                    : null)
                .OfType<ParcelaLancada>()
                .ToList();

            var formato = CultureInfo.GetCultureInfo("pt-BR").DateTimeFormat;
            var totalReferencia = parcelas.Where(p => p.Competencia == referencia).Sum(p => p.Transacao.Valor);

            var serie = new List<ProjecaoParcelasMesDTO>();
            var totalAnterior = totalReferencia;

            for (var i = 1; i <= meses; i++)
            {
                var competencia = referencia.AddMonths(i);
                var doMes = parcelas.Where(p => p.Competencia == competencia).ToList();
                var terminando = doMes.Where(p => p.Numero == p.Total).ToList();
                var total = doMes.Sum(p => p.Transacao.Valor);

                serie.Add(new ProjecaoParcelasMesDTO
                {
                    Mes = competencia.Month,
                    Ano = competencia.Year,
                    NomeMes = formato.GetMonthName(competencia.Month),
                    TotalParcelas = total,
                    QuantidadeParcelas = doMes.Count,
                    TotalParcelasTerminando = terminando.Sum(p => p.Transacao.Valor),
                    QuantidadeParcelasTerminando = terminando.Count,
                    ReducaoEmRelacaoAoMesAnterior = totalAnterior - total
                });

                totalAnterior = total;
            }

            // Compra ativa = tem parcela depois da referência. Sem id de compra no modelo, as parcelas
            // são agrupadas por (Descricao, ContaId, Data, total M): todas guardam a data original da compra.
            var futuras = parcelas.Where(p => p.Competencia > referencia).ToList();

            var compras = futuras
                .GroupBy(p => new { p.Transacao.Descricao, p.Transacao.ContaId, p.Transacao.Data, p.Total })
                .Select(g =>
                {
                    var ordenadas = g.OrderBy(p => p.Numero).ToList();
                    var proxima = ordenadas[0];

                    return new CompraParceladaAtivaDTO
                    {
                        Descricao = g.Key.Descricao,
                        ContaId = g.Key.ContaId,
                        NomeConta = proxima.Transacao.Conta?.Nome ?? "Sem conta",
                        CategoriaId = proxima.Transacao.CategoriaId,
                        NomeCategoria = proxima.Transacao.Categoria?.Nome ?? "Sem categoria",
                        Cor = proxima.Transacao.Categoria?.Cor,
                        DataCompra = g.Key.Data,
                        ValorParcela = proxima.Transacao.Valor,
                        // Parcela atual = a anterior à primeira futura; 0 quando a compra ainda não começou
                        ParcelaAtual = proxima.Numero - 1,
                        TotalParcelas = g.Key.Total,
                        ParcelasRestantes = ordenadas.Count,
                        ValorRestante = ordenadas.Sum(p => p.Transacao.Valor),
                        UltimaCompetencia = ordenadas.Max(p => p.Competencia)
                    };
                })
                .OrderBy(c => c.UltimaCompetencia)
                .ThenBy(c => c.Descricao)
                .ToList();

            return new ProjecaoParcelasResponseDTO
            {
                MesReferencia = referencia.Month,
                AnoReferencia = referencia.Year,
                QuantidadeMeses = meses,
                TotalParcelasMesReferencia = totalReferencia,
                TotalRestante = futuras.Sum(p => p.Transacao.Valor),
                Meses = serie,
                ComprasAtivas = compras
            };
        }

        public async Task<ProjecaoProximoMesResponseDTO> GetProjecaoProximoMesAsync(Guid ambienteId, int? mes = null, int? ano = null,
                                                                                    IEnumerable<Guid>? categoriasFixas = null,
                                                                                    IEnumerable<Guid>? categoriasRecebimento = null,
                                                                                    DateTime? dataReferencia = null)
        {
            var hoje = (dataReferencia ?? DateTime.Today).Date;
            var alvo = ResolverCompetencia(mes, ano, new DateOnly(hoje.Year, hoje.Month, 1).AddMonths(1));
            var mesBase = alvo.AddMonths(-1);
            var inicioHistorico = alvo.AddMonths(-MesesHistoricoEstimativa);

            var fixas = (categoriasFixas ?? []).ToHashSet();
            var recebimento = (categoriasRecebimento ?? []).ToHashSet();
            await ValidarCategoriasSelecionadasAsync(ambienteId, fixas, recebimento);

            var contas = await _repository.ContaRepository.GetAllByAmbienteAsync(ambienteId);
            var saldosSalvos = await _repository.SaldoMensalRepository.GetAllByAmbienteAsync(ambienteId, mesBase);

            // Uma consulta só: do dia 1 de alvo−3 até o último dia do alvo
            var fimAlvo = alvo.AddMonths(1).AddDays(-1);
            var transacoes = (await _repository.TransacaoRepository.GetByIntervaloCompetenciaAsync(ambienteId, inicioHistorico, fimAlvo)).ToList();

            var doMesBase = transacoes.Where(t => NormalizarCompetencia(t.MesCompetencia) == mesBase).ToList();
            var doAlvo = transacoes.Where(t => NormalizarCompetencia(t.MesCompetencia) == alvo).ToList();

            // Base da média: alvo−3 a alvo−1, sem parcelas (as do alvo já estão lançadas como parcelas)
            var historico = transacoes
                .Where(t => NormalizarCompetencia(t.MesCompetencia) < alvo && !t.IsParcela())
                .ToList();

            var projecaoContas = contas.Select(conta =>
            {
                // Mesma lógica do SaldoMensalService: saldo inicial salvo do mês base (0 se não houver) + movimento do mês base
                var saldoSalvo = saldosSalvos.FirstOrDefault(s => s.ContaId == conta.Id);
                var (entradasBase, saidasBase) = doMesBase.CalcularMovimentoDaConta(conta.Id);
                var saldoInicial = (saldoSalvo?.SaldoInicial ?? 0) + entradasBase - saidasBase;

                var (entradas, saidas) = doAlvo.CalcularMovimentoDaConta(conta.Id);
                var fixos = EstimarRecorrente(historico, doAlvo, conta.Id, TipoTransacao.Despesa, fixas);
                var recebimentos = EstimarRecorrente(historico, doAlvo, conta.Id, TipoTransacao.Receita, recebimento);

                decimal? fatura = null;
                if (conta.TipoConta == TipoConta.CartaoCredito)
                {
                    fatura = doAlvo
                        .Where(t => t.TipoTransacao == TipoTransacao.Despesa && t.ContaId == conta.Id)
                        .Sum(t => t.Valor) + fixos;
                }

                return new ProjecaoContaDTO
                {
                    ContaId = conta.Id,
                    NomeConta = conta.Nome ?? string.Empty,
                    TipoConta = conta.TipoConta,
                    SaldoInicialPrevisto = saldoInicial,
                    EntradasLancadas = entradas,
                    SaidasLancadas = saidas,
                    FixosEstimados = fixos,
                    RecebimentosEstimados = recebimentos,
                    SaldoFinalPrevisto = saldoInicial + entradas + recebimentos - saidas - fixos,
                    FaturaPrevista = fatura
                };
            }).ToList();

            // Transferência não é receita nem despesa: os totais gerais olham só Receita e Despesa
            var receitasPrevistas = doAlvo.Where(t => t.TipoTransacao == TipoTransacao.Receita).Sum(t => t.Valor)
                                    + projecaoContas.Sum(c => c.RecebimentosEstimados);
            var despesasPrevistas = doAlvo.Where(t => t.TipoTransacao == TipoTransacao.Despesa).Sum(t => t.Valor)
                                    + projecaoContas.Sum(c => c.FixosEstimados);

            return new ProjecaoProximoMesResponseDTO
            {
                Mes = alvo.Month,
                Ano = alvo.Year,
                ReceitasPrevistas = receitasPrevistas,
                DespesasPrevistas = despesasPrevistas,
                TotalParcelas = doAlvo
                    .Where(t => t.TipoTransacao == TipoTransacao.Despesa && t.IsParcela())
                    .Sum(t => t.Valor),
                ResultadoPrevisto = receitasPrevistas - despesasPrevistas,
                SaldoPrevisto = projecaoContas.Sum(c => c.SaldoFinalPrevisto),
                Contas = projecaoContas
            };
        }

        public async Task<FixosRecebimentosResponseDTO> GetFixosRecebimentosAsync(Guid ambienteId, int mes, int ano,
                                                                                  IEnumerable<Guid>? categoriasFixas,
                                                                                  IEnumerable<Guid>? categoriasRecebimento)
        {
            ValidarCompetencia(mes, ano);

            var fixas = (categoriasFixas ?? []).ToHashSet();
            var recebimento = (categoriasRecebimento ?? []).ToHashSet();

            if (fixas.Count == 0)
                throw new ArgumentException("Informe ao menos uma categoria de gasto fixo.");

            if (recebimento.Count == 0)
                throw new ArgumentException("Informe ao menos uma categoria de recebimento.");

            var categorias = await ValidarCategoriasSelecionadasAsync(ambienteId, fixas, recebimento);

            var transacoes = await _repository.TransacaoRepository.GetByMesCompetenciaAsync(ambienteId, mes, ano);
            var receitas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Receita).ToList();
            var despesas = transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();

            var recebimentos = receitas.Where(t => recebimento.Contains(t.CategoriaId)).Sum(t => t.Valor);
            var outrasReceitas = receitas.Where(t => !recebimento.Contains(t.CategoriaId)).Sum(t => t.Valor);

            // Categoria fixa tem prioridade: a parcela numa categoria fixa conta só em GastosFixos
            var gastosFixos = despesas.Where(t => fixas.Contains(t.CategoriaId)).Sum(t => t.Valor);
            var naoFixas = despesas.Where(t => !fixas.Contains(t.CategoriaId)).ToList();
            var parcelas = naoFixas.Where(t => t.IsParcela()).Sum(t => t.Valor);
            var gastosVariaveis = naoFixas.Where(t => !t.IsParcela()).Sum(t => t.Valor);
            var totalDespesas = gastosFixos + parcelas + gastosVariaveis;

            var detalhe = fixas
                .Select(id => CriarDetalheCategoria(categorias[id], despesas, GrupoFixo))
                .Concat(recebimento.Select(id => CriarDetalheCategoria(categorias[id], receitas, GrupoRecebimento)))
                .OrderBy(c => c.Grupo)
                .ThenByDescending(c => c.Total)
                .ToList();

            return new FixosRecebimentosResponseDTO
            {
                Mes = mes,
                Ano = ano,
                Recebimentos = recebimentos,
                OutrasReceitas = outrasReceitas,
                GastosFixos = gastosFixos,
                Parcelas = parcelas,
                GastosVariaveis = gastosVariaveis,
                TotalDespesas = totalDespesas,
                Saldo = recebimentos + outrasReceitas - totalDespesas,
                PercentualComprometido = CalcularPercentual(gastosFixos + parcelas, recebimentos),
                PercentualFixosSobreRecebimentos = CalcularPercentual(gastosFixos, recebimentos),
                Categorias = detalhe
            };
        }

        private static FixosRecebimentosCategoriaDTO CriarDetalheCategoria(Categoria categoria, IEnumerable<Transacao> transacoes, string grupo)
        {
            return new FixosRecebimentosCategoriaDTO
            {
                CategoriaId = categoria.Id,
                NomeCategoria = categoria.Nome ?? "Sem categoria",
                Cor = categoria.Cor,
                Total = transacoes.Where(t => t.CategoriaId == categoria.Id).Sum(t => t.Valor),
                Grupo = grupo
            };
        }

        // Para cada categoria escolhida: média do histórico (mês sem lançamento entra como zero)
        // menos o que já foi lançado no alvo, com mínimo zero. Parcelas ficam fora dos dois lados:
        // já estão lançadas no alvo e não representam gasto que se repete todo mês.
        private static decimal EstimarRecorrente(IEnumerable<Transacao> historico, IEnumerable<Transacao> doAlvo, Guid contaId,
                                                 TipoTransacao tipo, IReadOnlySet<Guid> categorias)
        {
            decimal total = 0;

            foreach (var categoriaId in categorias)
            {
                var media = Math.Round(historico
                    .Where(t => t.ContaId == contaId && t.TipoTransacao == tipo && t.CategoriaId == categoriaId)
                    .Sum(t => t.Valor) / MesesHistoricoEstimativa, 2);

                var lancado = doAlvo
                    .Where(t => t.ContaId == contaId && t.TipoTransacao == tipo && t.CategoriaId == categoriaId && !t.IsParcela())
                    .Sum(t => t.Valor);

                total += Math.Max(0, media - lancado);
            }

            return total;
        }

        // Categoria nas duas listas ou fora do ambiente ativo → 400. Devolve as categorias do ambiente por id.
        private async Task<Dictionary<Guid, Categoria>> ValidarCategoriasSelecionadasAsync(Guid ambienteId, IReadOnlySet<Guid> fixas,
                                                                                         IReadOnlySet<Guid> recebimento)
        {
            if (fixas.Overlaps(recebimento))
                throw new ArgumentException("Uma mesma categoria não pode estar entre as fixas e as de recebimento.");

            var categorias = (await _repository.CategoriaRepository.GetAllByAmbienteAsync(ambienteId)).ToDictionary(c => c.Id);

            var desconhecidas = fixas.Concat(recebimento).Where(id => !categorias.ContainsKey(id)).ToList();
            if (desconhecidas.Count > 0)
                throw new ArgumentException($"Categoria não encontrada no ambiente ativo: {string.Join(", ", desconhecidas)}.");

            return categorias;
        }

        // Mês e ano são opcionais juntos: sem nenhum dos dois vale o padrão; só um deles é erro
        private static DateOnly ResolverCompetencia(int? mes, int? ano, DateOnly padrao)
        {
            if (mes is null && ano is null)
                return padrao;

            if (mes is null || ano is null)
                throw new ArgumentException("Informe mês e ano juntos, ou nenhum dos dois.");

            ValidarCompetencia(mes.Value, ano.Value);
            return new DateOnly(ano.Value, mes.Value, 1);
        }

        private static DateOnly NormalizarCompetencia(DateOnly competencia) => new(competencia.Year, competencia.Month, 1);

        // Percentual de "parte" sobre "valorBase". Nulo quando a base é zero, como em CalcularVariacao
        private static decimal? CalcularPercentual(decimal parte, decimal valorBase)
        {
            return valorBase > 0
                ? Math.Round(parte / valorBase * 100, 2)
                : null;
        }

        // Valida o filtro "mês/ano de competência OU período de datas" e devolve só as despesas
        // (receita e transferência ficam de fora). Usado pelos relatórios que aceitam os dois filtros.
        private async Task<List<Transacao>> GetDespesasPorCompetenciaOuPeriodoAsync(Guid ambienteId, int mes, int ano,
                                                                                    DateTime dataInicial, DateTime dataFinal)
        {
            if ((mes > 0 || ano > 0) && (dataInicial != DateTime.MinValue || dataFinal != DateTime.MinValue))
                throw new ArgumentException("Informe apenas mês/ano ou período de datas, não ambos.");

            IEnumerable<Transacao> transacoes;

            if (mes > 0 && ano > 0)
                transacoes = await _repository.TransacaoRepository.GetByMesCompetenciaAsync(ambienteId, mes, ano);
            else if (dataInicial != DateTime.MinValue && dataFinal != DateTime.MinValue)
                transacoes = await _repository.TransacaoRepository.GetByPeriodoAsync(ambienteId, dataInicial, dataFinal);
            else
                throw new ArgumentException("Informe mês/ano ou período de datas.");

            return transacoes.Where(t => t.TipoTransacao == TipoTransacao.Despesa).ToList();
        }

        private static void ValidarCompetencia(int mes, int ano)
        {
            if (mes < 1 || mes > 12)
                throw new ArgumentException("Informe um mês entre 1 e 12.");

            ValidarAno(ano);
        }

        private static void ValidarAno(int ano)
        {
            if (ano < AnoMinimo || ano > AnoMaximo)
                throw new ArgumentException($"Informe um ano entre {AnoMinimo} e {AnoMaximo}.");
        }

        // Variação percentual de "atual" sobre "valorBase". Nulo quando a base é zero:
        // não existe variação percentual sobre zero, e devolver 0% esconderia um gasto que surgiu do nada.
        private static decimal? CalcularVariacao(decimal atual, decimal valorBase)
        {
            return valorBase > 0
                ? Math.Round((atual - valorBase) / valorBase * 100, 2)
                : null;
        }
    }
}
