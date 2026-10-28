/** Chamadas do painel de Administração (/Admin e /Logs). Todas exigem a role Admin no backend. */
import api from './client'
import type { ApiResponse, AmbienteResponse, ApiLogResponse } from '../types/api'

/** Usuário como aparece no painel de Admin. */
export interface UsuarioAdmin {
  id: string
  nome: string | null
  email: string | null
  dtaCriacao: string | null
  bloqueado: boolean
  roles: string[]
}

export interface CriarUsuarioRequest {
  nome: string
  email: string
  senha: string
  confirmacaoSenha: string
}

/** Cria um usuário (único caminho de cadastro; não há auto-cadastro). A confirmação de senha não é conferida pelo backend. */
export const criarUsuario = (data: CriarUsuarioRequest) =>
  api.post<ApiResponse<null>>('/Admin/usuarios', data)

/** Todos os usuários, com roles e situação de bloqueio. */
export const listarUsuarios = () =>
  api.get<ApiResponse<UsuarioAdmin[]>>('/Admin/usuarios')

/** Nomes das roles do Identity cadastradas. */
export const listarRoles = () =>
  api.get<ApiResponse<string[]>>('/Admin/roles')

/** Substitui todas as roles do usuário pela lista enviada. */
export const atribuirRoles = (id: string, roles: string[]) =>
  api.put<ApiResponse<null>>(`/Admin/usuarios/${id}/roles`, { roles })

/** Bloqueia o usuário por tempo indeterminado (lockout do Identity). */
export const bloquearUsuario = (id: string) =>
  api.put<ApiResponse<null>>(`/Admin/usuarios/${id}/bloquear`)

/** Remove o bloqueio do usuário. */
export const desbloquearUsuario = (id: string) =>
  api.put<ApiResponse<null>>(`/Admin/usuarios/${id}/desbloquear`)

/** Exclui o usuário. */
export const excluirUsuario = (id: string) =>
  api.delete<ApiResponse<null>>(`/Admin/usuarios/${id}`)

/** Todos os ambientes do sistema, com membros. */
export const listarAmbientes = () =>
  api.get<ApiResponse<AmbienteResponse[]>>('/Admin/ambientes')

/** Exclui qualquer ambiente, mesmo sem o Admin ser membro. */
export const excluirAmbiente = (id: string) =>
  api.delete<ApiResponse<null>>(`/Admin/ambientes/${id}`)

/** Filtros opcionais da consulta de logs; campos vazios devem ser removidos antes do envio. */
export interface LogFiltros {
  dataInicio?: string
  dataFim?: string
  userId?: string
  path?: string
  statusCode?: number | ''
  apenasErros?: boolean
  limite?: number
}

/** Logs gravados, do mais recente para o mais antigo (o backend limita a 500). */
export const listarLogs = (filtros: LogFiltros = {}) =>
  api.get<ApiResponse<ApiLogResponse[]>>('/Logs', { params: filtros })
