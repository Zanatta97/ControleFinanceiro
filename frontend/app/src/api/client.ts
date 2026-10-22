import axios, { type AxiosError, type InternalAxiosRequestConfig } from 'axios'
import type { ApiResponse, TokenDTO } from '../types/api'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL ?? '/api',
  headers: { 'Content-Type': 'application/json' },
})

type RetryableRequestConfig = InternalAxiosRequestConfig & { _retry?: boolean }

// Refresh em andamento, compartilhado pelas requisições que recebem 401 ao mesmo tempo.
// Sem ele, cada uma enviaria o mesmo refresh token; o backend rotaciona no primeiro,
// rejeita os demais e a sessão cai.
let refreshPromise: Promise<string> | null = null

const requestNewAccessToken = async (refreshToken: string): Promise<string> => {
  // axios puro para não passar pelos interceptors do `api` (evita loop),
  // mas com a mesma URL base, para funcionar também fora do proxy do Vite.
  const { data } = await axios.post<ApiResponse<TokenDTO>>(`${api.defaults.baseURL}/Auth/refresh-token`, {
    accessToken: localStorage.getItem('accessToken'),
    refreshToken,
  })
  const tokens = data.dados
  if (!tokens?.accessToken || !tokens.refreshToken) {
    throw new Error('Resposta de renovação de token sem os tokens esperados.')
  }
  localStorage.setItem('accessToken', tokens.accessToken)
  localStorage.setItem('refreshToken', tokens.refreshToken)
  return tokens.accessToken
}

const refreshAccessToken = (refreshToken: string): Promise<string> => {
  if (!refreshPromise) {
    refreshPromise = requestNewAccessToken(refreshToken)
      .catch((refreshError: unknown) => {
        // Falha real do refresh: encerra a sessão uma única vez, não uma por requisição.
        localStorage.clear()
        window.location.href = '/login'
        throw refreshError
      })
      .finally(() => {
        refreshPromise = null
      })
  }
  return refreshPromise
}

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (res) => res,
  async (error: AxiosError) => {
    const original = error.config as RetryableRequestConfig | undefined
    if (error.response?.status === 401 && original && !original._retry) {
      original._retry = true
      const refreshToken = localStorage.getItem('refreshToken')
      if (refreshToken) {
        try {
          const accessToken = await refreshAccessToken(refreshToken)
          original.headers.Authorization = `Bearer ${accessToken}`
          return api(original)
        } catch {
          // A sessão já foi encerrada em refreshAccessToken; rejeita a requisição original.
        }
      }
    }
    return Promise.reject(error)
  },
)

export default api
