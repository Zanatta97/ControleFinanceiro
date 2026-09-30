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

export const resumoMensal = (mes?: number, ano?: number) =>
  api.get<ApiResponse<ResumoFinanceiroResponse>>('/Relatorio/resumo', { params: { mes, ano } })

export const gastoPorCategoria = (mes?: number, ano?: number) =>
  api.get<ApiResponse<GastoPorCategoriaResponse[]>>('/Relatorio/por-categoria', { params: { mes, ano } })

// Mesma rota de gastoPorCategoria, aceitando competência ou período
export const gastoPorCategoriaFiltrado = (filtro: FiltroCompetenciaOuPeriodo) =>
  api.get<ApiResponse<GastoPorCategoriaResponse[]>>('/Relatorio/por-categoria', { params: filtro })

export const gastosPorConta = (filtro: FiltroCompetenciaOuPeriodo) =>
  api.get<ApiResponse<GastosPorContaResponse[]>>('/Relatorio/por-conta', { params: filtro })

export const comparativoCategoria = (mes: number, ano: number) =>
  api.get<ApiResponse<ComparativoCategoriaResponse[]>>('/Relatorio/comparativo-categoria', { params: { mes, ano } })

export const maioresDespesas = (filtro: FiltroCompetenciaOuPeriodo, quantidade: number) =>
  api.get<ApiResponse<MaiorDespesaResponse[]>>('/Relatorio/maiores-despesas', { params: { ...filtro, quantidade } })

export const matrizCategoriaMes = (ano: number) =>
  api.get<ApiResponse<MatrizCategoriaMesResponse>>('/Relatorio/matriz-categoria-mes', { params: { ano } })

export const frequenciaCategoria = (filtro: FiltroCompetenciaOuPeriodo) =>
  api.get<ApiResponse<FrequenciaCategoriaResponse[]>>('/Relatorio/frequencia-categoria', { params: filtro })

export const ritmoMes = () =>
  api.get<ApiResponse<RitmoMesResponse>>('/Relatorio/ritmo-mes')

export const faturaCompetencia = (contaId: string, mes: number, ano: number) =>
  api.get<ApiResponse<FaturaCompetenciaResponse>>(`/Relatorio/fatura/conta/${contaId}`, { params: { mes, ano } })

export const projecaoParcelas = (mes?: number, ano?: number, meses?: number) =>
  api.get<ApiResponse<ProjecaoParcelasResponse>>('/Relatorio/projecao/parcelas', { params: { mes, ano, meses } })

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

export const evolucaoMensal = (ano?: number) =>
  api.get<ApiResponse<EvolucaoMensalResponse>>('/Relatorio/evolucao-mensal', { params: { ano } })

export const statusOrcamentos = () =>
  api.get<ApiResponse<OrcamentoStatusResponse[]>>('/Relatorio/orcamentos')

export const extratoConta = (contaId: string, dataInicio?: string, dataFim?: string) =>
  api.get<ApiResponse<ExtratoContaResponse>>(`/Relatorio/extrato/conta/${contaId}`, {
    params: { dataInicio, dataFim },
  })
