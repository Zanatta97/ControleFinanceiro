import api from './client'
import type { ApiResponse } from '../types/api'

export interface SaldoMensalContaResponse {
  contaId: string
  nomeConta: string | null
  tipoConta: number
  saldoInicial: number
  totalEntradas: number
  totalSaidas: number
  saldoFinal: number
  diferenca: number
}

export const getSaldoMensal = (mes: number, ano: number) =>
  api.get<ApiResponse<SaldoMensalContaResponse[]>>('/SaldoMensal', { params: { mes, ano } })

export const salvarSaldoInicial = (contaId: string, mes: number, ano: number, saldoInicial: number) =>
  api.put<ApiResponse<null>>(`/SaldoMensal/${contaId}`, { saldoInicial }, { params: { mes, ano } })

export const calcularSaldoInicial = (mes: number, ano: number) =>
  api.post<ApiResponse<SaldoMensalContaResponse[]>>('/SaldoMensal/calcular', null, { params: { mes, ano } })
