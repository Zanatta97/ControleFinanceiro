import api from './client'
import type { ApiResponse, AmbienteRequest, AmbienteResponse, TokenDTO, UsuarioResumoDTO } from '../types/api'

export const listarTodos = () =>
  api.get<ApiResponse<AmbienteResponse[]>>('/Ambiente')

export const listarDoUsuario = () =>
  api.get<ApiResponse<AmbienteResponse[]>>('/Ambiente/user-ambientes')

export const buscarPorId = (id: string) =>
  api.get<ApiResponse<AmbienteResponse>>(`/Ambiente/${id}`)

export const criar = (data: AmbienteRequest) =>
  api.post<ApiResponse<AmbienteResponse>>('/Ambiente', data)

export const atualizar = (id: string, data: AmbienteRequest) =>
  api.put<ApiResponse<AmbienteResponse>>(`/Ambiente/${id}`, data)

export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Ambiente/${id}`)

export const selecionarAmbiente = (id: string) =>
  api.post<ApiResponse<TokenDTO>>(`/Ambiente/selecionar/${id}`)

export const listarMembros = (id: string) =>
  api.get<ApiResponse<UsuarioResumoDTO[]>>(`/Ambiente/${id}/membros`)

export const adicionarMembro = (id: string, membroEmail: string) =>
  api.post<ApiResponse<null>>(`/Ambiente/${id}/membros`, null, { params: { membroEmail } })

export const removerMembro = (id: string, membroId: string) =>
  api.delete<ApiResponse<null>>(`/Ambiente/${id}/membros/${membroId}`)

export const transferirDono = (id: string, membroId: string) =>
  api.put<ApiResponse<null>>(`/Ambiente/${id}/dono`, null, { params: { membroId } })
