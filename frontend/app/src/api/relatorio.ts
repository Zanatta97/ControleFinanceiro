import api from './client'
import type {
  ApiResponse,
  ResumoFinanceiroResponse,
  GastoPorCategoriaResponse,
  EvolucaoMensalResponse,
  OrcamentoStatusResponse,
  ExtratoContaResponse,
  FiltroCompetenciaOuPeriodo,
  GastosPorContaResponse,
  ComparativoCategoriaResponse,
  MaiorDespesaResponse,
  MatrizCategoriaMesResponse,
  FrequenciaCategoriaResponse,
  RitmoMesResponse,
  FaturaCompetenciaResponse,
  ProjecaoParcelasResponse,
  ProjecaoProximoMesResponse,
  FixosRecebimentosResponse,
} from '../types/api'

// O backend lê listas repetindo o parâmetro (categoriasFixas=a&categoriasFixas=b).
// indexes: null tira os colchetes que o axios põe por padrão; vale só nas chamadas que usam este objeto.
const paramsSemColchetes = { indexes: null }

/*
 * Relatórios do ambiente ativo. Em todos, transferência não conta como receita nem despesa.
 * "Competência" = mês/ano de MesCompetencia; "período" = datas da transação (uma compra parcelada
 * conta inteira na data da compra). Os relatórios com filtro aceitam um OU outro, nunca os dois.
 * Período sem dados costuma voltar 404 com `errorMessage` (ver useRelatorio).
 */

/** Receitas, despesas (cartão × demais contas) e saldo de uma competência. */
export const resumoMensal = (mes?: number, ano?: number) =>
  api.get<ApiResponse<ResumoFinanceiroResponse>>('/Relatorio/resumo', { params: { mes, ano } })

/** Despesas por categoria numa competência (404 quando não há despesa). */
export const gastoPorCategoria = (mes?: number, ano?: number) =>
  api.get<ApiResponse<GastoPorCategoriaResponse[]>>('/Relatorio/por-categoria', { params: { mes, ano } })

// Mesma rota de gastoPorCategoria, aceitando competência ou período
export const gastoPorCategoriaFiltrado = (filtro: FiltroCompetenciaOuPeriodo) =>
  api.get<ApiResponse<GastoPorCategoriaResponse[]>>('/Relatorio/por-categoria', { params: filtro })

/** Despesas por conta, por competência ou período. */
export const gastosPorConta = (filtro: FiltroCompetenciaOuPeriodo) =>
  api.get<ApiResponse<GastosPorContaResponse[]>>('/Relatorio/por-conta', { params: filtro })

/** Despesa por categoria na competência, no mês anterior e na média dos três anteriores. */
export const comparativoCategoria = (mes: number, ano: number) =>
  api.get<ApiResponse<ComparativoCategoriaResponse[]>>('/Relatorio/comparativo-categoria', { params: { mes, ano } })

/** As `quantidade` maiores despesas (1 a 100) do filtro. */
export const maioresDespesas = (filtro: FiltroCompetenciaOuPeriodo, quantidade: number) =>
  api.get<ApiResponse<MaiorDespesaResponse[]>>('/Relatorio/maiores-despesas', { params: { ...filtro, quantidade } })

/** Despesa de cada categoria em cada mês de competência do ano. */
export const matrizCategoriaMes = (ano: number) =>
  api.get<ApiResponse<MatrizCategoriaMesResponse>>('/Relatorio/matriz-categoria-mes', { params: { ano } })

/** Quantidade, total e ticket médio de despesas por categoria. */
export const frequenciaCategoria = (filtro: FiltroCompetenciaOuPeriodo) =>
  api.get<ApiResponse<FrequenciaCategoriaResponse[]>>('/Relatorio/frequencia-categoria', { params: filtro })

/** Gasto do mês corrente até hoje (pela data) contra o mesmo ponto do mês anterior. */
export const ritmoMes = () =>
  api.get<ApiResponse<RitmoMesResponse>>('/Relatorio/ritmo-mes')

/** Fatura de um cartão de crédito na competência (400 se a conta não for cartão). */
export const faturaCompetencia = (contaId: string, mes: number, ano: number) =>
  api.get<ApiResponse<FaturaCompetenciaResponse>>(`/Relatorio/fatura/conta/${contaId}`, { params: { mes, ano } })

/**
 * Parcelas a vencer nos `meses` seguintes à referência (padrão: mês atual) e compras parceladas ativas.
 * Mês e ano vão juntos ou nenhum dos dois.
 */
export const projecaoParcelas = (mes?: number, ano?: number, meses?: number) =>
  api.get<ApiResponse<ProjecaoParcelasResponse>>('/Relatorio/projecao/parcelas', { params: { mes, ano, meses } })

/** Previsão por conta da competência alvo (padrão: mês seguinte), usando as categorias fixas e de recebimento. */
export const projecaoProximoMes = (
  mes: number | undefined,
  ano: number | undefined,
  categoriasFixas: string[],
  categoriasRecebimento: string[],
) =>
  api.get<ApiResponse<ProjecaoProximoMesResponse>>('/Relatorio/projecao/proximo-mes', {
    params: { mes, ano, categoriasFixas, categoriasRecebimento },
    paramsSerializer: paramsSemColchetes,
  })

/** Recebimentos × gastos fixos, parcelas e variáveis da competência (exige ao menos uma categoria de cada lista). */
export const fixosRecebimentos = (
  mes: number,
  ano: number,
  categoriasFixas: string[],
  categoriasRecebimento: string[],
) =>
  api.get<ApiResponse<FixosRecebimentosResponse>>('/Relatorio/fixos-x-recebimentos', {
    params: { mes, ano, categoriasFixas, categoriasRecebimento },
    paramsSerializer: paramsSemColchetes,
  })

/** Receitas, despesas e saldo de cada mês de competência do ano. */
export const evolucaoMensal = (ano?: number) =>
  api.get<ApiResponse<EvolucaoMensalResponse>>('/Relatorio/evolucao-mensal', { params: { ano } })

/** Consumo de cada orçamento do ambiente (404 quando não há orçamento). */
export const statusOrcamentos = () =>
  api.get<ApiResponse<OrcamentoStatusResponse[]>>('/Relatorio/orcamentos')

/** Extrato de uma conta no período (origem e destino), com total de entradas e saídas. */
export const extratoConta = (contaId: string, dataInicio?: string, dataFim?: string) =>
  api.get<ApiResponse<ExtratoContaResponse>>(`/Relatorio/extrato/conta/${contaId}`, {
    params: { dataInicio, dataFim },
  })
