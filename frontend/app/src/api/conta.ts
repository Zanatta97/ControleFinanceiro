import api from './client'
import type { ApiResponse, ContaRequest, ContaResponse } from '../types/api'

/** Contas de todos os ambientes (só Admin). */
export const listarTodas = () =>
  api.get<ApiResponse<ContaResponse[]>>('/Conta')

/** Contas do ambiente ativo (o do token). */
export const listarDoAmbiente = () =>
  api.get<ApiResponse<ContaResponse[]>>('/Conta/ambiente')

/** Uma conta do ambiente ativo. */
export const buscarPorId = (id: string) =>
  api.get<ApiResponse<ContaResponse>>(`/Conta/${id}`)

/** Cria a conta no ambiente ativo. */
export const criar = (data: ContaRequest) =>
  api.post<ApiResponse<ContaResponse>>('/Conta', data)

/** Atualiza nome, tipo e saldo informado. */
export const atualizar = (id: string, data: ContaRequest) =>
  api.put<ApiResponse<ContaResponse>>(`/Conta/${id}`, data)

/** Exclui a conta e as transações de origem nela; falha se ela for destino de alguma transferência. */
export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Conta/${id}`)
