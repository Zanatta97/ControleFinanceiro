import api from './client'
import type { ApiResponse, TokenDTO, LoginTokenDTO, UsuarioLoginRequest, UsuarioRegisterRequest } from '../types/api'

export const login = (data: UsuarioLoginRequest) =>
  api.post<ApiResponse<LoginTokenDTO>>('/Auth/login', data)

export const register = (data: UsuarioRegisterRequest) =>
  api.post<ApiResponse<null>>('/Auth/register', data)

export const refreshToken = (data: TokenDTO) =>
  api.post<ApiResponse<TokenDTO>>('/Auth/refresh-token', data)

export const revokeToken = (username: string) =>
  api.post(`/Auth/revoke/${username}`)
