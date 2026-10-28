import api from './client'
import type { ApiResponse, TokenDTO, LoginTokenDTO, UsuarioLoginRequest, UsuarioRegisterRequest } from '../types/api'

/**
 * Login por e-mail e senha. O token devolvido só traz `ambiente_id` se o usuário já selecionou um
 * ambiente antes; sem ele, o App leva à tela de seleção.
 */
export const login = (data: UsuarioLoginRequest) =>
  api.post<ApiResponse<LoginTokenDTO>>('/Auth/login', data)

/** Entra no modo demonstração (sem senha); o token já vem com o ambiente demo selecionado. */
export const demoLogin = () =>
  api.post<ApiResponse<LoginTokenDTO>>('/Auth/demo')

/** Cadastro de usuário. O backend exige a role Admin; a tela de Admin usa `criarUsuario` (api/admin). */
export const register = (data: UsuarioRegisterRequest) =>
  api.post<ApiResponse<null>>('/Auth/register', data)

/**
 * Renovação manual do token. O fluxo automático não usa esta função: o interceptor de api/client.ts
 * chama o endpoint com axios puro, para não entrar no próprio interceptor.
 */
export const refreshToken = (data: TokenDTO) =>
  api.post<ApiResponse<TokenDTO>>('/Auth/refresh-token', data)

/** Revoga o refresh token de um usuário (username = e-mail). Só Admin. */
export const revokeToken = (username: string) =>
  api.post(`/Auth/revoke/${username}`)

/** Troca de senha do usuário logado. */
export interface AlterarSenhaRequest {
  senhaAtual: string
  novaSenha: string
  confirmacaoNovaSenha: string
}

/** Troca a senha do usuário logado (403 no modo demonstração). */
export const alterarSenha = (data: AlterarSenhaRequest) =>
  api.post<ApiResponse<null>>('/Auth/alterar-senha', data)
