import api from './client'
import type { ApiResponse, ContaRequest, ContaResponse } from '../types/api'

export const listarTodas = () =>
  api.get<ApiResponse<ContaResponse[]>>('/Conta')

export const listarDoAmbiente = () =>
  api.get<ApiResponse<ContaResponse[]>>('/Conta/ambiente')

export const buscarPorId = (id: string) =>
  api.get<ApiResponse<ContaResponse>>(`/Conta/${id}`)

export const criar = (data: ContaRequest) =>
  api.post<ApiResponse<ContaResponse>>('/Conta', data)

export const atualizar = (id: string, data: ContaRequest) =>
  api.put<ApiResponse<ContaResponse>>(`/Conta/${id}`, data)

export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Conta/${id}`)
