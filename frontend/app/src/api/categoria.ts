import api from './client'
import type { ApiResponse, CategoriaRequest, CategoriaResponse } from '../types/api'

export const listarTodas = () =>
  api.get<ApiResponse<CategoriaResponse[]>>('/Categoria')

export const listarDoAmbiente = () =>
  api.get<ApiResponse<CategoriaResponse[]>>('/Categoria/ambiente')

export const buscarPorId = (id: string) =>
  api.get<ApiResponse<CategoriaResponse>>(`/Categoria/${id}`)

export const criar = (data: CategoriaRequest) =>
  api.post<ApiResponse<CategoriaResponse>>('/Categoria', data)

export const atualizar = (id: string, data: CategoriaRequest) =>
  api.put<ApiResponse<CategoriaResponse>>(`/Categoria/${id}`, data)

export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Categoria/${id}`)
