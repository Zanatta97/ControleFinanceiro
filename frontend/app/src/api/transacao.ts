import api from './client'
import type { ApiResponse, TransacaoRequest, TransacaoResponse, TipoTransacao } from '../types/api'

export const listarTodas = () =>
  api.get<ApiResponse<TransacaoResponse[]>>('/Transacao')

export const listarDoAmbiente = () =>
  api.get<ApiResponse<TransacaoResponse[]>>('/Transacao/ambiente')

export const listarPorConta = (contaId: string) =>
  api.get<ApiResponse<TransacaoResponse[]>>(`/Transacao/conta/${contaId}`)

export const listarPorPeriodo = (inicio: string, fim: string) =>
  api.get<ApiResponse<TransacaoResponse[]>>('/Transacao/periodo', { params: { inicio, fim } })

export const listarPorTipo = (tipo: TipoTransacao) =>
  api.get<ApiResponse<TransacaoResponse[]>>(`/Transacao/tipo/${tipo}`)

export const buscarPorId = (id: string) =>
  api.get<ApiResponse<TransacaoResponse>>(`/Transacao/${id}`)

export const criar = (data: TransacaoRequest) =>
  api.post<ApiResponse<TransacaoResponse>>('/Transacao', data)

export const atualizar = (id: string, data: TransacaoRequest) =>
  api.put<ApiResponse<TransacaoResponse>>(`/Transacao/${id}`, data)

export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Transacao/${id}`)
