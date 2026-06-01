import api from './client'
import type { ApiResponse, OrcamentoRequest, OrcamentoResponse, StatusOrcamento } from '../types/api'

export const listarTodos = () =>
  api.get<ApiResponse<OrcamentoResponse[]>>('/Orcamento')

export const listarDoAmbiente = () =>
  api.get<ApiResponse<OrcamentoResponse[]>>('/Orcamento/ambiente')

export const listarPorStatus = (status: StatusOrcamento) =>
  api.get<ApiResponse<OrcamentoResponse[]>>(`/Orcamento/status/${status}`)

export const buscarPorId = (id: string) =>
  api.get<ApiResponse<OrcamentoResponse>>(`/Orcamento/${id}`)

export const criar = (data: OrcamentoRequest) =>
  api.post<ApiResponse<OrcamentoResponse>>('/Orcamento', data)

export const atualizar = (id: string, data: OrcamentoRequest) =>
  api.put<ApiResponse<OrcamentoResponse>>(`/Orcamento/${id}`, data)

export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Orcamento/${id}`)
