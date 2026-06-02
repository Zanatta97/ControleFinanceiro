import api from './client'
import type { ApiResponse, AmbienteResponse } from '../types/api'

export interface UsuarioAdmin {
  id: string
  nome: string | null
  email: string | null
  dtaCriacao: string | null
  bloqueado: boolean
  roles: string[]
}

export const listarUsuarios = () =>
  api.get<ApiResponse<UsuarioAdmin[]>>('/Admin/usuarios')

export const listarRoles = () =>
  api.get<ApiResponse<string[]>>('/Admin/roles')

export const atribuirRoles = (id: string, roles: string[]) =>
  api.put<ApiResponse<null>>(`/Admin/usuarios/${id}/roles`, { roles })

export const bloquearUsuario = (id: string) =>
  api.put<ApiResponse<null>>(`/Admin/usuarios/${id}/bloquear`)

export const desbloquearUsuario = (id: string) =>
  api.put<ApiResponse<null>>(`/Admin/usuarios/${id}/desbloquear`)

export const excluirUsuario = (id: string) =>
  api.delete<ApiResponse<null>>(`/Admin/usuarios/${id}`)

export const listarAmbientes = () =>
  api.get<ApiResponse<AmbienteResponse[]>>('/Admin/ambientes')

export const excluirAmbiente = (id: string) =>
  api.delete<ApiResponse<null>>(`/Admin/ambientes/${id}`)
