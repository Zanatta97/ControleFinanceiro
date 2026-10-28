import type { AxiosResponse } from 'axios'
import type { ApiResponse } from '../types/api'

/** Monta um JWT (sem assinatura válida) com o payload em UTF-8 + base64url, como o backend emite. */
export function criarToken(payload: Record<string, unknown>): string {
  const base64url = (texto: string) => {
    const bytes = new TextEncoder().encode(texto)
    let binario = ''
    bytes.forEach((b) => { binario += String.fromCharCode(b) })
    return btoa(binario).replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '')
  }
  return `${base64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }))}.${base64url(JSON.stringify(payload))}.assinatura`
}

/** Resposta de sucesso da API no formato ApiResponse, já embrulhada como resposta do axios. */
export function respostaOk<T>(dados: T, extra: Partial<ApiResponse<T>> = {}): AxiosResponse<ApiResponse<T>> {
  return {
    data: {
      statusCode: 200,
      errorMessage: '',
      success: true,
      statusMessage: null,
      timestamp: '2026-01-01T00:00:00Z',
      dados,
      ...extra,
    },
    status: 200,
    statusText: 'OK',
    headers: {},
    config: {} as AxiosResponse['config'],
  }
}

/** Erro no formato que o axios rejeita: `response.status` e `response.data`. */
export function erroHttp(status: number, data?: unknown) {
  return Object.assign(new Error(`HTTP ${status}`), { response: { status, data } })
}

/** Promise controlada pelo teste, para segurar uma requisição em andamento. */
export function adiado<T>() {
  let resolver!: (valor: T) => void
  let rejeitar!: (erro: unknown) => void
  const promise = new Promise<T>((res, rej) => {
    resolver = res
    rejeitar = rej
  })
  return { promise, resolver, rejeitar }
}
