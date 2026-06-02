import { useState, useMemo } from 'react'

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
  useMemo(() => { setPagina(1) }, [items.length])

  return { paginados, pagina: paginaAtual, totalPaginas, irPara, total: items.length }
}
