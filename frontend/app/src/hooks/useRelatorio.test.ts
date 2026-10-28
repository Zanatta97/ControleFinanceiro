import { describe, expect, it, vi } from 'vitest'
import { renderHook, waitFor } from '@testing-library/react'
import type { AxiosResponse } from 'axios'
import type { ApiResponse } from '../types/api'
import { useRelatorio } from './useRelatorio'
import { adiado, erroHttp, respostaOk } from '../test/helpers'

type Busca<T> = () => Promise<AxiosResponse<ApiResponse<T>>>

describe('useRelatorio', () => {
  it('começa carregando e passa a sucesso com os dados', async () => {
    const buscar: Busca<{ total: number }> = vi.fn().mockResolvedValue(respostaOk({ total: 10 }))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))

    expect(result.current).toEqual({ status: 'carregando' })
    await waitFor(() => expect(result.current).toEqual({ status: 'sucesso', dados: { total: 10 } }))
    expect(buscar).toHaveBeenCalledTimes(1)
  })

  it('lista vazia vira estado vazio', async () => {
    const buscar: Busca<number[]> = vi.fn().mockResolvedValue(respostaOk([]))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() => expect(result.current).toEqual({ status: 'vazio', mensagem: null }))
  })

  it('dados nulos viram estado vazio', async () => {
    const buscar: Busca<number[]> = vi.fn().mockResolvedValue(respostaOk(null))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() => expect(result.current).toEqual({ status: 'vazio', mensagem: null }))
  })

  it('usa a função `vazio` para decidir se um objeto conta como sem dados', async () => {
    const buscar: Busca<{ total: number }> = vi.fn().mockResolvedValue(respostaOk({ total: 0 }))
    const vazio = vi.fn((d: { total: number }) => d.total === 0)
    const { result } = renderHook(() => useRelatorio(buscar, 'k', { vazio }))
    await waitFor(() => expect(result.current.status).toBe('vazio'))
    expect(vazio).toHaveBeenCalledWith({ total: 0 })
  })

  it('404 com errorMessage é "sem dados" e mostra a mensagem do backend', async () => {
    const buscar: Busca<number[]> = vi
      .fn()
      .mockRejectedValue(erroHttp(404, { errorMessage: 'Nenhuma despesa encontrada no período.' }))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() =>
      expect(result.current).toEqual({ status: 'vazio', mensagem: 'Nenhuma despesa encontrada no período.' }),
    )
  })

  it('404 sem ApiResponse é rota inexistente e vira erro', async () => {
    const buscar: Busca<number[]> = vi.fn().mockRejectedValue(erroHttp(404, '<html>Not Found</html>'))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() =>
      expect(result.current).toEqual({
        status: 'erro',
        mensagem: 'Relatório indisponível na API. Verifique se o backend está atualizado.',
      }),
    )
  })

  it('404 com errorMessage em branco também é rota inexistente', async () => {
    const buscar: Busca<number[]> = vi.fn().mockRejectedValue(erroHttp(404, { errorMessage: '   ' }))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() => expect(result.current.status).toBe('erro'))
  })

  it('outros erros mostram a mensagem do backend quando houver', async () => {
    const buscar: Busca<number[]> = vi.fn().mockRejectedValue(erroHttp(400, { errorMessage: 'Período inválido.' }))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() => expect(result.current).toEqual({ status: 'erro', mensagem: 'Período inválido.' }))
  })

  it('erro sem resposta (rede) usa a mensagem genérica', async () => {
    const buscar: Busca<number[]> = vi.fn().mockRejectedValue(new Error('Network Error'))
    const { result } = renderHook(() => useRelatorio(buscar, 'k'))
    await waitFor(() =>
      expect(result.current).toEqual({ status: 'erro', mensagem: 'Não foi possível carregar o relatório.' }),
    )
  })

  it('desabilitado fica ocioso e não chama a API', () => {
    const buscar: Busca<number[]> = vi.fn()
    const { result } = renderHook(() => useRelatorio(buscar, 'k', { habilitado: false }))
    expect(result.current).toEqual({ status: 'ocioso' })
    expect(buscar).not.toHaveBeenCalled()
  })

  it('busca quando passa a ser habilitado e volta a ocioso quando desabilita', async () => {
    const buscar: Busca<number[]> = vi.fn().mockResolvedValue(respostaOk([1]))
    const { result, rerender } = renderHook(({ habilitado }) => useRelatorio(buscar, 'k', { habilitado }), {
      initialProps: { habilitado: false },
    })
    rerender({ habilitado: true })
    await waitFor(() => expect(result.current).toEqual({ status: 'sucesso', dados: [1] }))

    rerender({ habilitado: false })
    expect(result.current).toEqual({ status: 'ocioso' })
  })

  it('refaz a busca quando a chave muda, usando a função mais recente', async () => {
    const primeira: Busca<string[]> = vi.fn().mockResolvedValue(respostaOk(['a']))
    const segunda: Busca<string[]> = vi.fn().mockResolvedValue(respostaOk(['b']))
    const { result, rerender } = renderHook(({ buscar, chave }) => useRelatorio(buscar, chave), {
      initialProps: { buscar: primeira, chave: 'c:1-2026' },
    })
    await waitFor(() => expect(result.current).toEqual({ status: 'sucesso', dados: ['a'] }))

    rerender({ buscar: segunda, chave: 'c:2-2026' })
    await waitFor(() => expect(result.current).toEqual({ status: 'sucesso', dados: ['b'] }))
    expect(primeira).toHaveBeenCalledTimes(1)
    expect(segunda).toHaveBeenCalledTimes(1)
  })

  it('não refaz a busca se só a função mudar e a chave for a mesma', async () => {
    const primeira: Busca<string[]> = vi.fn().mockResolvedValue(respostaOk(['a']))
    const segunda: Busca<string[]> = vi.fn().mockResolvedValue(respostaOk(['b']))
    const { result, rerender } = renderHook(({ buscar }) => useRelatorio(buscar, 'mesma'), {
      initialProps: { buscar: primeira },
    })
    await waitFor(() => expect(result.current.status).toBe('sucesso'))
    rerender({ buscar: segunda })
    expect(segunda).not.toHaveBeenCalled()
  })

  it('descarta a resposta de uma busca antiga que chega depois da atual', async () => {
    const antiga = adiado<AxiosResponse<ApiResponse<string[]>>>()
    const buscarAntiga: Busca<string[]> = vi.fn(() => antiga.promise)
    const buscarNova: Busca<string[]> = vi.fn().mockResolvedValue(respostaOk(['novo']))

    const { result, rerender } = renderHook(({ buscar, chave }) => useRelatorio(buscar, chave), {
      initialProps: { buscar: buscarAntiga, chave: 'antiga' },
    })
    rerender({ buscar: buscarNova, chave: 'nova' })
    await waitFor(() => expect(result.current).toEqual({ status: 'sucesso', dados: ['novo'] }))

    antiga.resolver(respostaOk(['velho']))
    await antiga.promise
    await new Promise((r) => setTimeout(r, 0))
    expect(result.current).toEqual({ status: 'sucesso', dados: ['novo'] })
  })
})
