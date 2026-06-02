interface Props {
  pagina: number
  totalPaginas: number
  total: number
  itensPorPagina: number
  onPagina: (p: number) => void
}

export default function Pagination({ pagina, totalPaginas, total, itensPorPagina, onPagina }: Props) {
  if (totalPaginas <= 1) return null

  const inicio = (pagina - 1) * itensPorPagina + 1
  const fim = Math.min(pagina * itensPorPagina, total)

  return (
    <div className="flex items-center justify-between border-t border-fin-border px-4 py-3 text-sm text-fin-text-secondary">
      <span>{inicio}–{fim} de {total}</span>
      <div className="flex items-center gap-1">
        <button
          onClick={() => onPagina(1)}
          disabled={pagina === 1}
          className="rounded px-2 py-1 hover:bg-fin-surface-2 disabled:opacity-40 disabled:cursor-not-allowed"
        >«</button>
        <button
          onClick={() => onPagina(pagina - 1)}
          disabled={pagina === 1}
          className="rounded px-2 py-1 hover:bg-fin-surface-2 disabled:opacity-40 disabled:cursor-not-allowed"
        >‹</button>
        <span className="px-3 font-medium text-fin-text-primary">{pagina} / {totalPaginas}</span>
        <button
          onClick={() => onPagina(pagina + 1)}
          disabled={pagina === totalPaginas}
          className="rounded px-2 py-1 hover:bg-fin-surface-2 disabled:opacity-40 disabled:cursor-not-allowed"
        >›</button>
        <button
          onClick={() => onPagina(totalPaginas)}
          disabled={pagina === totalPaginas}
          className="rounded px-2 py-1 hover:bg-fin-surface-2 disabled:opacity-40 disabled:cursor-not-allowed"
        >»</button>
      </div>
    </div>
  )
}
