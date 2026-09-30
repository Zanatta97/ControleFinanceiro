using ControleFinanceiroAPI.Common.Extensions;
using ControleFinanceiroAPI.DTO.Common;
using ControleFinanceiroAPI.DTO.Relatorio;
using ControleFinanceiroAPI.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ControleFinanceiroAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RelatorioController : ControllerBase
    {
        private readonly IRelatorioService _service;

        public RelatorioController(IRelatorioService service)
        {
            _service = service;
        }

        /// <summary>
        /// Resumo financeiro (receitas, despesas e saldo) de um mês de competência do ambiente ativo.
        /// </summary>
        /// <param name="mes">Mês de competência (1 a 12).</param>
        /// <param name="ano">Ano de competência.</param>
        [HttpGet("resumo")]
        [ProducesResponseType(typeof(ApiResponseDTO<ResumoFinanceiroResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetResumo([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var resumo = await _service.GetResumoAsync(ambienteId, mes, ano);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<ResumoFinanceiroResponseDTO>.SuccessResponse(resumo));
        }

        /// <summary>
        /// Despesas agrupadas por categoria, com total e percentual. Transferências não entram.
        /// </summary>
        /// <remarks>
        /// Informe mês/ano de competência OU o período (dataInicio/dataFim, pela data da transação) — um dos dois, nunca ambos.
        /// No filtro por período, uma compra parcelada conta inteira no mês da compra, pois todas as parcelas guardam a data original.
        /// </remarks>
        /// <param name="mes">Mês de competência (1 a 12). Use junto com ano.</param>
        /// <param name="ano">Ano de competência. Use junto com mes.</param>
        /// <param name="dataInicio">Início do período (data da transação). Use junto com dataFim.</param>
        /// <param name="dataFim">Fim do período (data da transação). Use junto com dataInicio.</param>
        [HttpGet("por-categoria")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGastoPorCategoria([FromQuery] int mes, [FromQuery] int ano,
                                                              [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var gastos = await _service.GetGastoPorCategoriaAsync(ambienteId, mes, ano, dataInicio ?? default, dataFim ?? default);

                if (!gastos.Any())
                    return StatusCode(StatusCodes.Status404NotFound,
                        ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>.ErrorResponse("Nenhuma despesa encontrada no período.", StatusCodes.Status404NotFound));

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>.SuccessResponse(gastos));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<IEnumerable<GastoPorCategoriaResponseDTO>>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Despesas agrupadas por conta, com total e percentual. Transferências não entram.
        /// </summary>
        /// <remarks>
        /// Informe mês/ano de competência OU o período (dataInicio/dataFim, pela data da transação) — um dos dois, nunca ambos.
        /// No filtro por período, uma compra parcelada conta inteira no mês da compra, pois todas as parcelas guardam a data original.
        /// </remarks>
        /// <param name="mes">Mês de competência (1 a 12). Use junto com ano.</param>
        /// <param name="ano">Ano de competência. Use junto com mes.</param>
        /// <param name="dataInicio">Início do período (data da transação). Use junto com dataFim.</param>
        /// <param name="dataFim">Fim do período (data da transação). Use junto com dataInicio.</param>
        [HttpGet("por-conta")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<GastosPorContaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetGastosPorConta([FromQuery] int mes, [FromQuery] int ano,
                                                           [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var gastos = await _service.GetGastosPorContaAsync(ambienteId, mes, ano, dataInicio ?? default, dataFim ?? default);

                if (!gastos.Any())
                    return StatusCode(StatusCodes.Status404NotFound,
                        ApiResponseDTO<IEnumerable<GastosPorContaResponseDTO>>.ErrorResponse("Nenhuma despesa encontrada no período.", StatusCodes.Status404NotFound));

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<IEnumerable<GastosPorContaResponseDTO>>.SuccessResponse(gastos));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<IEnumerable<GastosPorContaResponseDTO>>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Comparativo por categoria: gasto do mês, do mês anterior e média dos 3 meses anteriores, com variação percentual.
        /// </summary>
        /// <remarks>
        /// Usa o mês de competência. A média considera M-1, M-2 e M-3; mês sem despesa entra como zero.
        /// As variações (contra o mês anterior e contra a média) vêm nulas quando a base de comparação é zero.
        /// </remarks>
        /// <param name="mes">Mês de competência (1 a 12).</param>
        /// <param name="ano">Ano de competência.</param>
        [HttpGet("comparativo-categoria")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<ComparativoCategoriaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetComparativoCategoria([FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var comparativo = await _service.GetComparativoCategoriaAsync(ambienteId, mes, ano);

                if (!comparativo.Any())
                    return StatusCode(StatusCodes.Status404NotFound,
                        ApiResponseDTO<IEnumerable<ComparativoCategoriaResponseDTO>>.ErrorResponse("Nenhuma despesa encontrada no período.", StatusCodes.Status404NotFound));

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<IEnumerable<ComparativoCategoriaResponseDTO>>.SuccessResponse(comparativo));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<IEnumerable<ComparativoCategoriaResponseDTO>>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// As N maiores despesas do período ou da competência, da maior para a menor.
        /// </summary>
        /// <remarks>
        /// Informe mês/ano de competência OU o período (dataInicio/dataFim, pela data da transação) — um dos dois, nunca ambos.
        /// No filtro por período, uma compra parcelada conta inteira no mês da compra, pois todas as parcelas guardam a data original.
        /// </remarks>
        /// <param name="quantidade">Quantidade de despesas a retornar (padrão 10, máximo 100).</param>
        /// <param name="mes">Mês de competência (1 a 12). Use junto com ano.</param>
        /// <param name="ano">Ano de competência. Use junto com mes.</param>
        /// <param name="dataInicio">Início do período (data da transação). Use junto com dataFim.</param>
        /// <param name="dataFim">Fim do período (data da transação). Use junto com dataInicio.</param>
        [HttpGet("maiores-despesas")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<MaiorDespesaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMaioresDespesas([FromQuery] int mes, [FromQuery] int ano,
                                                            [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim,
                                                            [FromQuery] int quantidade = 10)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var despesas = await _service.GetMaioresDespesasAsync(ambienteId, quantidade, mes, ano, dataInicio ?? default, dataFim ?? default);

                if (!despesas.Any())
                    return StatusCode(StatusCodes.Status404NotFound,
                        ApiResponseDTO<IEnumerable<MaiorDespesaResponseDTO>>.ErrorResponse("Nenhuma despesa encontrada no período.", StatusCodes.Status404NotFound));

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<IEnumerable<MaiorDespesaResponseDTO>>.SuccessResponse(despesas));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<IEnumerable<MaiorDespesaResponseDTO>>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Matriz categoria × mês: gasto de cada categoria em cada um dos 12 meses de competência do ano.
        /// </summary>
        /// <remarks>
        /// Cada categoria traz sempre os 12 meses (zero onde não houve despesa) e o total do ano.
        /// A resposta também traz o total de cada mês somando todas as categorias.
        /// </remarks>
        /// <param name="ano">Ano de competência.</param>
        [HttpGet("matriz-categoria-mes")]
        [ProducesResponseType(typeof(ApiResponseDTO<MatrizCategoriaMesResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetMatrizCategoriaMes([FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var matriz = await _service.GetMatrizCategoriaMesAsync(ambienteId, ano);

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<MatrizCategoriaMesResponseDTO>.SuccessResponse(matriz));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<MatrizCategoriaMesResponseDTO>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Frequência e ticket médio por categoria: quantidade de despesas, total e valor médio.
        /// </summary>
        /// <remarks>
        /// Informe mês/ano de competência OU o período (dataInicio/dataFim, pela data da transação) — um dos dois, nunca ambos.
        /// No filtro por período, uma compra parcelada conta inteira no mês da compra, pois todas as parcelas guardam a data original.
        /// Ordenado pela quantidade de despesas, da maior para a menor.
        /// </remarks>
        /// <param name="mes">Mês de competência (1 a 12). Use junto com ano.</param>
        /// <param name="ano">Ano de competência. Use junto com mes.</param>
        /// <param name="dataInicio">Início do período (data da transação). Use junto com dataFim.</param>
        /// <param name="dataFim">Fim do período (data da transação). Use junto com dataInicio.</param>
        [HttpGet("frequencia-categoria")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<FrequenciaCategoriaResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFrequenciaTicketMedio([FromQuery] int mes, [FromQuery] int ano,
                                                                  [FromQuery] DateTime? dataInicio, [FromQuery] DateTime? dataFim)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var frequencia = await _service.GetFrequenciaTicketMedioAsync(ambienteId, mes, ano, dataInicio ?? default, dataFim ?? default);

                if (!frequencia.Any())
                    return StatusCode(StatusCodes.Status404NotFound,
                        ApiResponseDTO<IEnumerable<FrequenciaCategoriaResponseDTO>>.ErrorResponse("Nenhuma despesa encontrada no período.", StatusCodes.Status404NotFound));

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<IEnumerable<FrequenciaCategoriaResponseDTO>>.SuccessResponse(frequencia));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<IEnumerable<FrequenciaCategoriaResponseDTO>>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Ritmo do mês: gasto de hoje no mês corrente comparado ao mesmo ponto do mês anterior, com projeção para o fim do mês.
        /// </summary>
        /// <remarks>
        /// Usa a DATA da transação (calendário), não a competência — uma compra parcelada conta inteira no dia da compra.
        /// Compara do dia 1 até hoje com o dia 1 até o mesmo dia do mês anterior; se o mês anterior for mais curto,
        /// compara até o último dia dele. A projeção é linear: gasto até hoje ÷ dias decorridos × dias do mês.
        /// A variação percentual vem nula quando o mês anterior não teve gasto até o dia comparado.
        /// </remarks>
        [HttpGet("ritmo-mes")]
        [ProducesResponseType(typeof(ApiResponseDTO<RitmoMesResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetRitmoMes()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var ritmo = await _service.GetRitmoMesAsync(ambienteId);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<RitmoMesResponseDTO>.SuccessResponse(ritmo));
        }

        /// <summary>
        /// Fatura do cartão por competência: total de despesas, total pago e saldo em aberto.
        /// </summary>
        /// <remarks>
        /// Total pago = transferências com destino no cartão na mesma competência (pagamentos da fatura).
        /// Saldo em aberto = total de despesas − total pago; negativo indica pagamento acima do valor da fatura.
        /// Retorna 404 se a conta não existir no ambiente ativo e 400 se ela não for cartão de crédito.
        /// </remarks>
        /// <param name="contaId">Id da conta do tipo cartão de crédito.</param>
        /// <param name="mes">Mês de competência (1 a 12).</param>
        /// <param name="ano">Ano de competência.</param>
        [HttpGet("fatura/conta/{contaId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<FaturaCompetenciaResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFaturaCompetencia(Guid contaId, [FromQuery] int mes, [FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                // KeyNotFoundException (conta fora do ambiente) sobe para o ErrorHandlingMiddleware, que devolve 404
                var fatura = await _service.GetFaturaCompetenciaAsync(ambienteId, contaId, mes, ano);

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<FaturaCompetenciaResponseDTO>.SuccessResponse(fatura));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<FaturaCompetenciaResponseDTO>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Projeção das parcelas já lançadas: total por mês futuro e compras parceladas ainda ativas.
        /// </summary>
        /// <remarks>
        /// Parcela é reconhecida pela observação "Parcela N/M" gravada no cadastro; se a observação for editada
        /// e perder esse prefixo, a transação deixa de contar como parcela. Só despesas entram.
        /// A série vai do mês seguinte à referência até referência + meses. ReducaoEmRelacaoAoMesAnterior é
        /// o total do mês anterior da série menos o do mês (no primeiro, o do mês de referência); negativo indica aumento.
        /// Compras ativas: parcelas agrupadas por descrição, conta, data da compra e total de parcelas, com ao menos
        /// uma parcela depois da referência. ParcelaAtual é 0 quando a compra ainda não começou.
        /// </remarks>
        /// <param name="mes">Mês de referência (1 a 12). Opcional, junto com ano; padrão = mês corrente.</param>
        /// <param name="ano">Ano de referência. Opcional, junto com mes.</param>
        /// <param name="meses">Quantidade de meses projetados (padrão 12, de 1 a 36).</param>
        [HttpGet("projecao/parcelas")]
        [ProducesResponseType(typeof(ApiResponseDTO<ProjecaoParcelasResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProjecaoParcelas([FromQuery] int? mes, [FromQuery] int? ano, [FromQuery] int meses = 12)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var projecao = await _service.GetProjecaoParcelasAsync(ambienteId, mes, ano, meses);

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<ProjecaoParcelasResponseDTO>.SuccessResponse(projecao));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<ProjecaoParcelasResponseDTO>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Projeção do próximo mês por conta: saldo inicial previsto, lançamentos já feitos e estimativa de fixos e recebimentos.
        /// </summary>
        /// <remarks>
        /// Saldo inicial previsto = saldo final do mês anterior ao alvo (saldo inicial salvo, ou 0, + movimento da competência).
        /// Fixos/recebimentos estimados: para cada categoria informada, média das despesas/receitas da conta nos 3 meses
        /// anteriores ao alvo (mês sem lançamento conta 0, parcelas fora), menos o já lançado no alvo, mínimo 0.
        /// Saldo final previsto = inicial + entradas lançadas + recebimentos estimados − saídas lançadas − fixos estimados.
        /// Cartão de crédito traz também a fatura prevista (despesas lançadas no cartão + fixos estimados no cartão).
        /// Retorna 400 se uma categoria não for do ambiente ativo ou estiver nas duas listas.
        /// </remarks>
        /// <param name="mes">Mês da competência alvo (1 a 12). Opcional, junto com ano; padrão = mês seguinte ao atual.</param>
        /// <param name="ano">Ano da competência alvo. Opcional, junto com mes.</param>
        /// <param name="categoriasFixas">Ids das categorias tratadas como gasto fixo (repita o parâmetro para várias).</param>
        /// <param name="categoriasRecebimento">Ids das categorias tratadas como recebimento (repita o parâmetro para várias).</param>
        [HttpGet("projecao/proximo-mes")]
        [ProducesResponseType(typeof(ApiResponseDTO<ProjecaoProximoMesResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetProjecaoProximoMes([FromQuery] int? mes, [FromQuery] int? ano,
                                                               [FromQuery] List<Guid>? categoriasFixas,
                                                               [FromQuery] List<Guid>? categoriasRecebimento)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var projecao = await _service.GetProjecaoProximoMesAsync(ambienteId, mes, ano, categoriasFixas, categoriasRecebimento);

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<ProjecaoProximoMesResponseDTO>.SuccessResponse(projecao));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<ProjecaoProximoMesResponseDTO>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Gastos fixos × recebimentos na competência: quanto dos recebimentos já está comprometido com fixos e parcelas.
        /// </summary>
        /// <remarks>
        /// As categorias de gasto fixo e de recebimento são escolhidas na chamada (ao menos uma de cada).
        /// Despesa em categoria fixa conta como fixo mesmo sendo parcela; Parcelas soma só as parceladas fora das fixas;
        /// o resto é gasto variável. Receita fora das categorias de recebimento vai para OutrasReceitas.
        /// Os percentuais vêm nulos quando não houve recebimento no mês.
        /// Retorna 400 se faltar categoria, se uma categoria não for do ambiente ativo ou estiver nas duas listas.
        /// </remarks>
        /// <param name="mes">Mês de competência (1 a 12).</param>
        /// <param name="ano">Ano de competência.</param>
        /// <param name="categoriasFixas">Ids das categorias de gasto fixo (repita o parâmetro para várias).</param>
        /// <param name="categoriasRecebimento">Ids das categorias de recebimento (repita o parâmetro para várias).</param>
        [HttpGet("fixos-x-recebimentos")]
        [ProducesResponseType(typeof(ApiResponseDTO<FixosRecebimentosResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GetFixosRecebimentos([FromQuery] int mes, [FromQuery] int ano,
                                                              [FromQuery] List<Guid>? categoriasFixas,
                                                              [FromQuery] List<Guid>? categoriasRecebimento)
        {
            var ambienteId = User.GetAmbienteAtivo();
            try
            {
                var relatorio = await _service.GetFixosRecebimentosAsync(ambienteId, mes, ano, categoriasFixas, categoriasRecebimento);

                return StatusCode(StatusCodes.Status200OK,
                    ApiResponseDTO<FixosRecebimentosResponseDTO>.SuccessResponse(relatorio));
            }
            catch (ArgumentException ex)
            {
                return StatusCode(StatusCodes.Status400BadRequest,
                    ApiResponseDTO<FixosRecebimentosResponseDTO>.ErrorResponse(ex.Message, StatusCodes.Status400BadRequest));
            }
        }

        /// <summary>
        /// Evolução mensal de receitas, despesas e saldo ao longo de um ano.
        /// </summary>
        /// <param name="ano">Ano de referência.</param>
        [HttpGet("evolucao-mensal")]
        [ProducesResponseType(typeof(ApiResponseDTO<EvolucaoMensalResponseDTO>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetEvolucaoMensal([FromQuery] int ano)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var evolucao = await _service.GetEvolucaoMensalAsync(ambienteId, ano);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<EvolucaoMensalResponseDTO>.SuccessResponse(evolucao));
        }

        /// <summary>
        /// Situação de cada orçamento do ambiente ativo: limite, valor gasto e percentual consumido.
        /// </summary>
        [HttpGet("orcamentos")]
        [ProducesResponseType(typeof(ApiResponseDTO<IEnumerable<OrcamentoStatusResponseDTO>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrcamentosStatus()
        {
            var ambienteId = User.GetAmbienteAtivo();
            var orcamentos = await _service.GetOrcamentosStatusAsync(ambienteId);

            if (!orcamentos.Any())
                return StatusCode(StatusCodes.Status404NotFound,
                    ApiResponseDTO<IEnumerable<OrcamentoStatusResponseDTO>>.ErrorResponse("Nenhum orçamento encontrado.", StatusCodes.Status404NotFound));

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<IEnumerable<OrcamentoStatusResponseDTO>>.SuccessResponse(orcamentos));
        }

        /// <summary>
        /// Extrato de uma conta no período: entradas, saídas e transações (inclui transferências de/para a conta).
        /// </summary>
        /// <param name="contaId">Id da conta.</param>
        /// <param name="dataInicio">Início do período (data da transação).</param>
        /// <param name="dataFim">Fim do período (data da transação).</param>
        [HttpGet("extrato/conta/{contaId}")]
        [ProducesResponseType(typeof(ApiResponseDTO<ExtratoContaResponseDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponseDTO<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetExtratoConta(Guid contaId, [FromQuery] DateTime dataInicio, [FromQuery] DateTime dataFim)
        {
            var ambienteId = User.GetAmbienteAtivo();
            var extrato = await _service.GetExtratoContaAsync(ambienteId, contaId, dataInicio, dataFim);

            return StatusCode(StatusCodes.Status200OK,
                ApiResponseDTO<ExtratoContaResponseDTO>.SuccessResponse(extrato));
        }
    }
}
