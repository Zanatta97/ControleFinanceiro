import api from './client'
import type { ApiResponse, AmbienteRequest, AmbienteResponse, TokenDTO, UsuarioResumoDTO } from '../types/api'

/** Todos os ambientes do sistema (só Admin). */
export const listarTodos = () =>
  api.get<ApiResponse<AmbienteResponse[]>>('/Ambiente')

/** Ambientes dos quais o usuário logado é membro, com a lista de membros e papéis. */
export const listarDoUsuario = () =>
  api.get<ApiResponse<AmbienteResponse[]>>('/Ambiente/user-ambientes')

/** Um ambiente do qual o usuário é membro (404 se não for). */
export const buscarPorId = (id: string) =>
  api.get<ApiResponse<AmbienteResponse>>(`/Ambiente/${id}`)

/** Cria um ambiente com o usuário como Dono. Não troca o ambiente ativo: para entrar nele, chame `selecionarAmbiente`. */
export const criar = (data: AmbienteRequest) =>
  api.post<ApiResponse<AmbienteResponse>>('/Ambiente', data)

/** Renomeia o ambiente (só Dono ou Admin do sistema). */
export const atualizar = (id: string, data: AmbienteRequest) =>
  api.put<ApiResponse<AmbienteResponse>>(`/Ambiente/${id}`, data)

/** Exclui o ambiente (só Dono ou Admin do sistema). */
export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Ambiente/${id}`)

/**
 * Seleciona o ambiente ativo. Devolve um NOVO par de tokens, com o claim `ambiente_id`; quem chama precisa
 * guardá-los (AuthContext.setAmbienteToken), senão as próximas requisições continuam no ambiente anterior.
 */
export const selecionarAmbiente = (id: string) =>
  api.post<ApiResponse<TokenDTO>>(`/Ambiente/selecionar/${id}`)

/** Membros do ambiente (id, nome, e-mail). */
export const listarMembros = (id: string) =>
  api.get<ApiResponse<UsuarioResumoDTO[]>>(`/Ambiente/${id}/membros`)

/** Adiciona como "Membro" o usuário do e-mail (só Dono ou Admin do ambiente). */
export const adicionarMembro = (id: string, membroEmail: string) =>
  api.post<ApiResponse<null>>(`/Ambiente/${id}/membros`, null, { params: { membroEmail } })

/** Remove um membro pelo id do usuário; o Dono não pode ser removido. */
export const removerMembro = (id: string, membroId: string) =>
  api.delete<ApiResponse<null>>(`/Ambiente/${id}/membros/${membroId}`)

/** Transfere a posse do ambiente para outro membro. */
export const transferirDono = (id: string, membroId: string) =>
  api.put<ApiResponse<null>>(`/Ambiente/${id}/dono`, null, { params: { membroId } })
