import api from './client'
import type {
  ApiResponse,
  ResumoFinanceiroResponse,
  GastoPorCategoriaResponse,
  EvolucaoMensalResponse,
  OrcamentoStatusResponse,
  ExtratoContaResponse,
} from '../types/api'

export const resumoMensal = (mes?: number, ano?: number) =>
  api.get<ApiResponse<ResumoFinanceiroResponse>>('/Relatorio/resumo', { params: { mes, ano } })

export const gastoPorCategoria = (mes?: number, ano?: number) =>
  api.get<ApiResponse<GastoPorCategoriaResponse[]>>('/Relatorio/por-categoria', { params: { mes, ano } })

export const evolucaoMensal = (ano?: number) =>
  api.get<ApiResponse<EvolucaoMensalResponse>>('/Relatorio/evolucao-mensal', { params: { ano } })

export const statusOrcamentos = () =>
  api.get<ApiResponse<OrcamentoStatusResponse[]>>('/Relatorio/orcamentos')

export const extratoConta = (contaId: string, dataInicio?: string, dataFim?: string) =>
  api.get<ApiResponse<ExtratoContaResponse>>(`/Relatorio/extrato/conta/${contaId}`, {
    params: { dataInicio, dataFim },
  })
