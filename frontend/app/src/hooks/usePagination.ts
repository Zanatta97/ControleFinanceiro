import { useState, useMemo } from 'react'

/**
 * Paginação feita no cliente: a lista completa já veio da API e é fatiada aqui.
 * @param items lista completa (já filtrada e ordenada).
 * @param itensPorPagina tamanho da página; padrão 20.
 * @returns a fatia da página atual, a página (limitada ao total), o total de páginas (mínimo 1),
 * `irPara` (aceita qualquer número e ajusta ao intervalo válido) e o total de itens.
 */
export function usePagination<T>(items: T[], itensPorPagina = 20) {
  const [pagina, setPagina] = useState(1)

  const totalPaginas = Math.max(1, Math.ceil(items.length / itensPorPagina))
  const paginaAtual = Math.min(pagina, totalPaginas)

  const paginados = useMemo(
    () => items.slice((paginaAtual - 1) * itensPorPagina, paginaAtual * itensPorPagina),
    [items, paginaAtual, itensPorPagina],
  )

  function irPara(p: number) {
    setPagina(Math.max(1, Math.min(p, totalPaginas)))
  }

  // Resetar para página 1 quando a lista muda de tamanho
  // (comportamento atual: o setState roda dentro do useMemo, durante a renderização, e não num useEffect)
  useMemo(() => { setPagina(1) }, [items.length])

  return { paginados, pagina: paginaAtual, totalPaginas, irPara, total: items.length }
}
