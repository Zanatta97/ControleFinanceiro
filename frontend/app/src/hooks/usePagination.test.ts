import { describe, expect, it } from 'vitest'
import { act, renderHook } from '@testing-library/react'
import { usePagination } from './usePagination'

const lista = (n: number) => Array.from({ length: n }, (_, i) => i + 1)

describe('usePagination', () => {
  it('começa na página 1 com a primeira fatia', () => {
    const { result } = renderHook(() => usePagination(lista(45), 20))
    expect(result.current.pagina).toBe(1)
    expect(result.current.totalPaginas).toBe(3)
    expect(result.current.total).toBe(45)
    expect(result.current.paginados).toEqual(lista(20))
  })

  it('usa 20 itens por página por padrão', () => {
    const { result } = renderHook(() => usePagination(lista(21)))
    expect(result.current.paginados).toHaveLength(20)
    expect(result.current.totalPaginas).toBe(2)
  })

  it('lista vazia tem 1 página e nenhum item', () => {
    const { result } = renderHook(() => usePagination([], 10))
    expect(result.current.totalPaginas).toBe(1)
    expect(result.current.paginados).toEqual([])
    expect(result.current.total).toBe(0)
  })

  it('irPara muda a página e a fatia', () => {
    const { result } = renderHook(() => usePagination(lista(45), 20))
    act(() => result.current.irPara(3))
    expect(result.current.pagina).toBe(3)
    expect(result.current.paginados).toEqual([41, 42, 43, 44, 45])
  })

  it('irPara limita ao intervalo de páginas válido', () => {
    const { result } = renderHook(() => usePagination(lista(45), 20))
    act(() => result.current.irPara(99))
    expect(result.current.pagina).toBe(3)
    act(() => result.current.irPara(-5))
    expect(result.current.pagina).toBe(1)
  })

  it('volta para a página 1 quando a lista muda de tamanho', () => {
    const { result, rerender } = renderHook(({ itens }) => usePagination(itens, 10), {
      initialProps: { itens: lista(50) },
    })
    act(() => result.current.irPara(4))
    expect(result.current.pagina).toBe(4)

    rerender({ itens: lista(45) })
    expect(result.current.pagina).toBe(1)
    expect(result.current.paginados).toEqual(lista(10))
  })

  it('mantém a página quando a lista muda sem mudar de tamanho', () => {
    const { result, rerender } = renderHook(({ itens }) => usePagination(itens, 10), {
      initialProps: { itens: lista(30) },
    })
    act(() => result.current.irPara(2))
    rerender({ itens: lista(30).reverse() })
    expect(result.current.pagina).toBe(2)
  })

  it('nunca expõe página além do total quando a lista encolhe', () => {
    const { result, rerender } = renderHook(({ itens }) => usePagination(itens, 10), {
      initialProps: { itens: lista(30) },
    })
    act(() => result.current.irPara(3))
    rerender({ itens: lista(5) })
    expect(result.current.pagina).toBeLessThanOrEqual(result.current.totalPaginas)
    expect(result.current.paginados).toEqual(lista(5))
  })

  it.skip('Bug: não atualiza estado durante a renderização (setState dentro de useMemo)', () => {
    // hooks/usePagination.ts:19 — o setPagina(1) no useMemo roda no render e força
    // uma renderização extra a cada montagem e a cada mudança de tamanho da lista.
    let renders = 0
    const { rerender } = renderHook(({ itens }) => {
      renders++
      return usePagination(itens, 10)
    }, { initialProps: { itens: lista(30) } })
    expect(renders).toBe(1)

    renders = 0
    rerender({ itens: lista(25) })
    expect(renders).toBe(1)
  })
})
