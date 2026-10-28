import api from './client'
import type { ApiResponse, TipoConta } from '../types/api'

/** Saldo de uma conta numa competência: inicial + entradas − saídas = final; `diferenca` = final − inicial. */
export interface SaldoMensalContaResponse {
  contaId: string
  nomeConta: string | null
  tipoConta: TipoConta
  saldoInicial: number
  totalEntradas: number
  totalSaidas: number
  saldoFinal: number
  diferenca: number
}

/** Saldo de cada conta do ambiente na competência `mes`/`ano`. */
export const getSaldoMensal = (mes: number, ano: number) =>
  api.get<ApiResponse<SaldoMensalContaResponse[]>>('/SaldoMensal', { params: { mes, ano } })

/** Grava o saldo inicial digitado para a conta no mês (substitui o anterior). */
export const salvarSaldoInicial = (contaId: string, mes: number, ano: number, saldoInicial: number) =>
  api.put<ApiResponse<null>>(`/SaldoMensal/${contaId}`, { saldoInicial }, { params: { mes, ano } })

/**
 * Recalcula o saldo inicial de todas as contas a partir do saldo final do mês anterior, sobrescrevendo
 * valores digitados, e devolve os saldos do mês já atualizados.
 */
export const calcularSaldoInicial = (mes: number, ano: number) =>
  api.post<ApiResponse<SaldoMensalContaResponse[]>>('/SaldoMensal/calcular', null, { params: { mes, ano } })
