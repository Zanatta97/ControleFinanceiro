using ControleFinanceiroAPI.DTO.Relatorio;

namespace ControleFinanceiroAPI.Interfaces.Services
{
    /// <summary>
    /// Relatórios financeiros do ambiente, calculados em memória a partir das transações.
    /// </summary>
    /// <remarks>
    /// Regras comuns: transferência nunca entra como receita nem despesa (só move dinheiro entre contas e,
    /// se contasse, a compra no cartão apareceria duas vezes — na compra e no pagamento da fatura); entra
    /// apenas no movimento por conta (extrato, saldo, fatura). Parcela é reconhecida pelo prefixo
    /// "Parcela N/M" da observação. Filtros de entrada inválidos lançam <see cref="ArgumentException"/>,
    /// que o RelatorioController devolve como 400.
    /// </remarks>
    public interface IRelatorioService
    {
        /// <summary>
        /// Receitas, despesas e saldo da competência, com as despesas separadas entre contas do tipo cartão
        /// de crédito (DespesasCartao) e as demais (DespesasOutras) para o Dashboard mostrar a divisão.
        /// </summary>
        Task<ResumoFinanceiroResponseDTO> GetResumoAsync(Guid ambienteId, int mes, int ano);

        /// <summary>Despesas agrupadas por categoria, com total e percentual sobre o total do filtro.</summary>
        /// <remarks>Filtro por competência (mes/ano) OU por período de datas — exatamente um dos dois.</remarks>
        /// <exception cref="ArgumentException">Nenhum filtro ou os dois filtros informados.</exception>
        Task<IEnumerable<GastoPorCategoriaResponseDTO>> GetGastoPorCategoriaAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                  DateTime dataInicial = default, DateTime dataFinal = default);

        /// <summary>Receitas, despesas (cartão × outras) e saldo de cada um dos 12 meses de competência do ano.</summary>
        Task<EvolucaoMensalResponseDTO> GetEvolucaoMensalAsync(Guid ambienteId, int ano);

        /// <summary>
        /// Consumo de cada orçamento do ambiente (ativos e inativos): soma das despesas da categoria com
        /// Data até a DataLimite do orçamento, sem data inicial. Ordenado pelo percentual consumido.
        /// </summary>
        Task<IEnumerable<OrcamentoStatusResponseDTO>> GetOrcamentosStatusAsync(Guid ambienteId);

        /// <summary>
        /// Extrato da conta no período (pela Data): transações de origem ou destino nela, com total de
        /// entradas e saídas do ponto de vista da conta.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Conta não encontrada no ambiente (404 pelo middleware).</exception>
        Task<ExtratoContaResponseDTO> GetExtratoContaAsync(Guid ambienteId, Guid contaId, DateTime dataInicio, DateTime dataFim);

        /// <summary>Despesas agrupadas por conta, com total e percentual sobre o total do filtro.</summary>
        /// <remarks>Filtro por competência (mes/ano) OU por período de datas — exatamente um dos dois.</remarks>
        /// <exception cref="ArgumentException">Nenhum filtro ou os dois filtros informados.</exception>
        Task<IEnumerable<GastosPorContaResponseDTO>> GetGastosPorContaAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                            DateTime dataInicial = default, DateTime dataFinal = default);

        /// <summary>
        /// Despesa por categoria na competência M, em M−1 e na média de M−1..M−3, com as variações percentuais.
        /// </summary>
        /// <exception cref="ArgumentException">Mês fora de 1..12 ou ano fora de 1900..2100.</exception>
        Task<IEnumerable<ComparativoCategoriaResponseDTO>> GetComparativoCategoriaAsync(Guid ambienteId, int mes, int ano);

        /// <summary>As maiores despesas do filtro, da maior para a menor (empate: a mais recente primeiro).</summary>
        /// <param name="quantidade">Quantas devolver, de 1 a 100.</param>
        /// <exception cref="ArgumentException">Quantidade fora de 1..100, ou filtro ausente/duplicado.</exception>
        Task<IEnumerable<MaiorDespesaResponseDTO>> GetMaioresDespesasAsync(Guid ambienteId, int quantidade = 10, int mes = 0, int ano = 0,
                                                                          DateTime dataInicial = default, DateTime dataFinal = default);

        /// <summary>Despesa de cada categoria em cada mês de competência do ano, com totais por mês e do ano.</summary>
        /// <exception cref="ArgumentException">Ano fora de 1900..2100.</exception>
        Task<MatrizCategoriaMesResponseDTO> GetMatrizCategoriaMesAsync(Guid ambienteId, int ano);

        /// <summary>Quantidade de despesas, total e ticket médio por categoria.</summary>
        /// <remarks>Filtro por competência (mes/ano) OU por período de datas — exatamente um dos dois.</remarks>
        /// <exception cref="ArgumentException">Nenhum filtro ou os dois filtros informados.</exception>
        Task<IEnumerable<FrequenciaCategoriaResponseDTO>> GetFrequenciaTicketMedioAsync(Guid ambienteId, int mes = 0, int ano = 0,
                                                                                        DateTime dataInicial = default, DateTime dataFinal = default);

        /// <summary>
        /// Gasto do mês corrente até hoje (pela Data, não pela competência) comparado ao mesmo ponto do mês
        /// anterior, com projeção linear para o fim do mês.
        /// </summary>
        /// <param name="dataReferencia">Data usada como "hoje"; padrão DateTime.Today (o controller não informa).</param>
        Task<RitmoMesResponseDTO> GetRitmoMesAsync(Guid ambienteId, DateTime? dataReferencia = null);

        /// <summary>
        /// Fatura do cartão na competência: despesas no cartão, pagamentos recebidos (transferências com
        /// destino nele) e saldo em aberto.
        /// </summary>
        /// <exception cref="KeyNotFoundException">Conta não encontrada no ambiente.</exception>
        /// <exception cref="ArgumentException">Conta não é cartão de crédito, ou mês/ano inválido.</exception>
        Task<FaturaCompetenciaResponseDTO> GetFaturaCompetenciaAsync(Guid ambienteId, Guid contaId, int mes, int ano);

        /// <summary>
        /// Total de parcelas por mês nos <paramref name="meses"/> seguintes à referência e as compras parceladas
        /// ainda ativas. Parcelas que faltam no banco são projetadas a partir da última lançada de cada compra.
        /// </summary>
        /// <param name="mes">Mês de referência; informe junto com <paramref name="ano"/> ou nenhum dos dois (padrão: mês atual).</param>
        /// <param name="meses">Horizonte da série, de 1 a 36.</param>
        /// <param name="dataReferencia">Data usada como "hoje"; padrão DateTime.Today.</param>
        /// <exception cref="ArgumentException">Horizonte fora de 1..36, ou mês/ano incompleto ou inválido.</exception>
        Task<ProjecaoParcelasResponseDTO> GetProjecaoParcelasAsync(Guid ambienteId, int? mes = null, int? ano = null, int meses = 12,
                                                                   DateTime? dataReferencia = null);

        /// <summary>
        /// Previsão da competência alvo por conta: saldo inicial previsto, lançamentos já feitos, fixos e
        /// recebimentos estimados pela média dos 3 meses anteriores e parcelas projetadas.
        /// </summary>
        /// <param name="mes">Mês alvo; informe junto com <paramref name="ano"/> ou nenhum dos dois (padrão: mês seguinte ao atual).</param>
        /// <param name="categoriasFixas">Categorias tratadas como gasto fixo (opcional).</param>
        /// <param name="categoriasRecebimento">Categorias tratadas como recebimento (opcional).</param>
        /// <param name="dataReferencia">Data usada como "hoje"; padrão DateTime.Today.</param>
        /// <exception cref="ArgumentException">
        /// Mês/ano incompleto ou inválido, categoria fora do ambiente ou presente nas duas listas.
        /// </exception>
        Task<ProjecaoProximoMesResponseDTO> GetProjecaoProximoMesAsync(Guid ambienteId, int? mes = null, int? ano = null,
                                                                       IEnumerable<Guid>? categoriasFixas = null,
                                                                       IEnumerable<Guid>? categoriasRecebimento = null,
                                                                       DateTime? dataReferencia = null);

        /// <summary>
        /// Divide receitas e despesas da competência entre recebimentos, gastos fixos, parcelas e variáveis,
        /// e calcula quanto dos recebimentos já está comprometido.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Mês/ano inválido, lista de categorias vazia, categoria fora do ambiente ou presente nas duas listas.
        /// </exception>
        Task<FixosRecebimentosResponseDTO> GetFixosRecebimentosAsync(Guid ambienteId, int mes, int ano,
                                                                     IEnumerable<Guid>? categoriasFixas,
                                                                     IEnumerable<Guid>? categoriasRecebimento);
    }
}
