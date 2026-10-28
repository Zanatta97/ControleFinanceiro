import api from './client'
import type { ApiResponse, CategoriaRequest, CategoriaResponse } from '../types/api'

/** Categorias de todos os ambientes (só Admin). */
export const listarTodas = () =>
  api.get<ApiResponse<CategoriaResponse[]>>('/Categoria')

/** Categorias do ambiente ativo (o do token). */
export const listarDoAmbiente = () =>
  api.get<ApiResponse<CategoriaResponse[]>>('/Categoria/ambiente')

/** Uma categoria do ambiente ativo. */
export const buscarPorId = (id: string) =>
  api.get<ApiResponse<CategoriaResponse>>(`/Categoria/${id}`)

/** Cria a categoria no ambiente ativo. */
export const criar = (data: CategoriaRequest) =>
  api.post<ApiResponse<CategoriaResponse>>('/Categoria', data)

/** Atualiza nome, cor e ícone. */
export const atualizar = (id: string, data: CategoriaRequest) =>
  api.put<ApiResponse<CategoriaResponse>>(`/Categoria/${id}`, data)

/** Exclui a categoria; o banco apaga junto as transações e orçamentos dela. */
export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Categoria/${id}`)
