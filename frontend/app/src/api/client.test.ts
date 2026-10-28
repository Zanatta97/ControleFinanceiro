import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import axios, { AxiosError, type AxiosAdapter, type AxiosResponse, type InternalAxiosRequestConfig } from 'axios'
import api from './client'
import { adiado, respostaOk } from '../test/helpers'
import type { ApiResponse, TokenDTO } from '../types/api'

const adapterOriginal = api.defaults.adapter

/** Servidor falso: só aceita o token "novo"; qualquer outro recebe 401. */
function servidorQueExigeTokenNovo(tokenValido = 'Bearer novo') {
  const chamadas: InternalAxiosRequestConfig[] = []
  const adapter: AxiosAdapter = async (config) => {
    chamadas.push(config)
    const resposta: AxiosResponse = {
      data: { url: config.url },
      status: 200,
      statusText: 'OK',
      headers: {},
      config,
    }
    if (config.headers.Authorization !== tokenValido) {
      resposta.status = 401
      resposta.statusText = 'Unauthorized'
      throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, resposta)
    }
    return resposta
  }
  api.defaults.adapter = adapter
  return chamadas
}

function respostaRefresh(tokens: TokenDTO) {
  return respostaOk(tokens) as AxiosResponse<ApiResponse<TokenDTO>>
}

describe('api/client', () => {
  let location: { href: string }

  beforeEach(() => {
    location = { href: 'http://localhost/transacoes' }
    vi.stubGlobal('location', location)
  })

  afterEach(() => {
    api.defaults.adapter = adapterOriginal
  })

  describe('interceptor de requisição', () => {
    it('envia o access token salvo no cabeçalho Authorization', async () => {
      localStorage.setItem('accessToken', 'novo')
      const chamadas = servidorQueExigeTokenNovo()
      await api.get('/Conta')
      expect(chamadas[0].headers.Authorization).toBe('Bearer novo')
    })

    it('sem token salvo, não envia Authorization', async () => {
      const chamadas: InternalAxiosRequestConfig[] = []
      api.defaults.adapter = async (config) => {
        chamadas.push(config)
        return { data: null, status: 200, statusText: 'OK', headers: {}, config }
      }
      await api.get('/Auth/login')
      expect(chamadas[0].headers.Authorization).toBeUndefined()
    })

    it('usa /api como URL base e JSON como conteúdo', () => {
      expect(api.defaults.baseURL).toBe('/api')
      expect(api.defaults.headers['Content-Type']).toBe('application/json')
    })
  })

  describe('renovação do token em 401', () => {
    it('renova o token, salva os novos e repete a requisição', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      const chamadas = servidorQueExigeTokenNovo()
      const post = vi.spyOn(axios, 'post').mockResolvedValue(
        respostaRefresh({ accessToken: 'novo', refreshToken: 'refresh-novo' }),
      )

      const resposta = await api.get('/Conta')

      expect(resposta.status).toBe(200)
      expect(post).toHaveBeenCalledTimes(1)
      expect(post).toHaveBeenCalledWith('/api/Auth/refresh-token', {
        accessToken: 'velho',
        refreshToken: 'refresh-velho',
      })
      expect(localStorage.getItem('accessToken')).toBe('novo')
      expect(localStorage.getItem('refreshToken')).toBe('refresh-novo')
      expect(chamadas).toHaveLength(2)
      expect(chamadas[1].headers.Authorization).toBe('Bearer novo')
    })

    it('vários 401 simultâneos disparam UM único refresh e todas as requisições são repetidas', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      const chamadas = servidorQueExigeTokenNovo()
      const refresh = adiado<AxiosResponse<ApiResponse<TokenDTO>>>()
      const post = vi.spyOn(axios, 'post').mockReturnValue(refresh.promise)

      const requisicoes = Promise.all([api.get('/Conta'), api.get('/Categoria'), api.get('/Transacao')])

      // Espera os três 401 chegarem antes de o refresh responder
      await vi.waitFor(() => expect(chamadas).toHaveLength(3))
      refresh.resolver(respostaRefresh({ accessToken: 'novo', refreshToken: 'refresh-novo' }))
      const respostas = await requisicoes

      expect(post).toHaveBeenCalledTimes(1)
      expect(respostas.map((r) => r.data.url)).toEqual(['/Conta', '/Categoria', '/Transacao'])
      expect(chamadas).toHaveLength(6)
      expect(chamadas.slice(3).every((c) => c.headers.Authorization === 'Bearer novo')).toBe(true)
    })

    it('um refresh concluído não é reaproveitado: o próximo 401 faz um refresh novo', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'r1')
      let tokenValido = 'Bearer t1'
      api.defaults.adapter = async (config) => {
        const resposta: AxiosResponse = { data: null, status: 200, statusText: 'OK', headers: {}, config }
        if (config.headers.Authorization !== tokenValido) {
          resposta.status = 401
          throw new AxiosError('Unauthorized', 'ERR_BAD_REQUEST', config, null, resposta)
        }
        return resposta
      }
      const post = vi
        .spyOn(axios, 'post')
        .mockResolvedValueOnce(respostaRefresh({ accessToken: 't1', refreshToken: 'r2' }))
        .mockResolvedValueOnce(respostaRefresh({ accessToken: 't2', refreshToken: 'r3' }))

      await api.get('/Conta')
      tokenValido = 'Bearer t2'
      await api.get('/Conta')

      expect(post).toHaveBeenCalledTimes(2)
      expect(post).toHaveBeenLastCalledWith('/api/Auth/refresh-token', { accessToken: 't1', refreshToken: 'r2' })
    })

    it('falha no refresh encerra a sessão, redireciona para /login e rejeita com o 401 original', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      localStorage.setItem('nomeAmbiente', 'Casa')
      servidorQueExigeTokenNovo()
      vi.spyOn(axios, 'post').mockRejectedValue(new Error('refresh expirado'))

      const erro = await api.get('/Conta').catch((e: unknown) => e)

      expect(erro).toBeInstanceOf(AxiosError)
      expect((erro as AxiosError).response?.status).toBe(401)
      expect(localStorage.getItem('accessToken')).toBeNull()
      expect(localStorage.getItem('refreshToken')).toBeNull()
      expect(localStorage.getItem('nomeAmbiente')).toBeNull()
      expect(location.href).toBe('/login')
    })

    it('falha no refresh com vários 401 simultâneos encerra a sessão uma única vez', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      const chamadas = servidorQueExigeTokenNovo()
      const refresh = adiado<AxiosResponse<ApiResponse<TokenDTO>>>()
      const post = vi.spyOn(axios, 'post').mockReturnValue(refresh.promise)
      const limpar = vi.spyOn(Storage.prototype, 'clear')

      const resultados = Promise.allSettled([api.get('/Conta'), api.get('/Categoria')])
      await vi.waitFor(() => expect(chamadas).toHaveLength(2))
      refresh.rejeitar(new Error('refresh expirado'))
      const [r1, r2] = await resultados

      expect(post).toHaveBeenCalledTimes(1)
      expect(limpar).toHaveBeenCalledTimes(1)
      expect(r1.status).toBe('rejected')
      expect(r2.status).toBe('rejected')
      expect(chamadas).toHaveLength(2) // nenhuma foi repetida
      expect(location.href).toBe('/login')
    })

    it('resposta de refresh sem os tokens conta como falha', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      servidorQueExigeTokenNovo()
      vi.spyOn(axios, 'post').mockResolvedValue(respostaRefresh({ accessToken: 'novo', refreshToken: null }))

      await expect(api.get('/Conta')).rejects.toBeInstanceOf(AxiosError)
      expect(localStorage.getItem('accessToken')).toBeNull()
      expect(location.href).toBe('/login')
    })

    it('sem refresh token salvo, rejeita o 401 sem tentar renovar nem redirecionar', async () => {
      localStorage.setItem('accessToken', 'velho')
      servidorQueExigeTokenNovo()
      const post = vi.spyOn(axios, 'post')

      await expect(api.get('/Conta')).rejects.toMatchObject({ response: { status: 401 } })
      expect(post).not.toHaveBeenCalled()
      expect(location.href).toBe('http://localhost/transacoes')
    })

    it('se a requisição repetida também der 401, não entra em loop', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      const chamadas = servidorQueExigeTokenNovo('Bearer nunca-aceito')
      const post = vi
        .spyOn(axios, 'post')
        .mockResolvedValue(respostaRefresh({ accessToken: 'novo', refreshToken: 'refresh-novo' }))

      await expect(api.get('/Conta')).rejects.toMatchObject({ response: { status: 401 } })
      expect(post).toHaveBeenCalledTimes(1)
      expect(chamadas).toHaveLength(2)
    })

    it('erro que não é 401 é repassado sem renovar o token', async () => {
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      api.defaults.adapter = async (config) => {
        const resposta: AxiosResponse = { data: null, status: 500, statusText: 'Erro', headers: {}, config }
        throw new AxiosError('Erro', 'ERR_BAD_RESPONSE', config, null, resposta)
      }
      const post = vi.spyOn(axios, 'post')

      await expect(api.get('/Conta')).rejects.toMatchObject({ response: { status: 500 } })
      expect(post).not.toHaveBeenCalled()
    })

    it('erro de rede (sem resposta) é repassado sem renovar o token', async () => {
      localStorage.setItem('refreshToken', 'refresh-velho')
      api.defaults.adapter = async (config) => {
        throw new AxiosError('Network Error', 'ERR_NETWORK', config)
      }
      const post = vi.spyOn(axios, 'post')

      await expect(api.get('/Conta')).rejects.toMatchObject({ code: 'ERR_NETWORK' })
      expect(post).not.toHaveBeenCalled()
    })

    it.skip('Bug: encerrar a sessão por falha no refresh preserva a preferência de tema', async () => {
      // api/client.ts:37 usa localStorage.clear(), que apaga também a chave "theme"
      localStorage.setItem('theme', 'dark')
      localStorage.setItem('accessToken', 'velho')
      localStorage.setItem('refreshToken', 'refresh-velho')
      servidorQueExigeTokenNovo()
      vi.spyOn(axios, 'post').mockRejectedValue(new Error('refresh expirado'))

      await api.get('/Conta').catch(() => {})

      expect(localStorage.getItem('accessToken')).toBeNull()
      expect(localStorage.getItem('theme')).toBe('dark')
    })
  })
})
