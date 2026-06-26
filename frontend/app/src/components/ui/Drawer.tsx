import { useEffect, type ReactNode } from 'react'
import { Icon } from '@iconify/react'

interface Props {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  /** Rodapé fixo (ex.: botões Cancelar/Salvar). No mobile fica colado embaixo. */
  footer?: ReactNode
}

export default function Drawer({ open, onClose, title, children, footer }: Props) {
  useEffect(() => {
    if (!open) return
    const prev = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    return () => { document.body.style.overflow = prev }
  }, [open])

  useEffect(() => {
    if (!open) return
    function onKey(e: KeyboardEvent) { if (e.key === 'Escape') onClose() }
    document.addEventListener('keydown', onKey)
    return () => document.removeEventListener('keydown', onKey)
  }, [open, onClose])

  return (
    <>
      {/* Backdrop (apenas no desktop; no mobile o painel ocupa a tela toda) */}
      <div
        className={`fixed inset-0 z-40 hidden transition-opacity duration-300 md:block ${open ? 'opacity-100' : 'pointer-events-none opacity-0'}`}
        style={{ backgroundColor: 'var(--fin-overlay)' }}
        onClick={onClose}
      />

      {/* Painel — tela cheia no mobile, painel lateral 420px no desktop */}
      <div
        className={`fixed inset-0 z-50 flex flex-col bg-fin-surface transition-transform duration-300 md:inset-y-0 md:left-auto md:right-0 md:w-full md:max-w-[420px] md:shadow-2xl ${open ? 'translate-x-0' : 'translate-x-full'}`}
      >
        {/* Cabeçalho */}
        <div className="flex flex-none items-center justify-between border-b border-fin-border px-4 py-4 sm:px-6">
          <div className="flex items-center gap-2">
            <button
              onClick={onClose}
              aria-label="Voltar"
              className="flex h-9 w-9 items-center justify-center rounded-lg text-fin-text-muted transition hover:bg-fin-surface-2 md:hidden"
            >
              <Icon icon="lucide:chevron-left" width={22} height={22} />
            </button>
            <h2 className="text-lg font-semibold text-fin-text-primary">{title}</h2>
          </div>
          <button
            onClick={onClose}
            aria-label="Fechar"
            className="hidden h-9 w-9 items-center justify-center rounded-lg text-fin-text-muted transition hover:bg-fin-surface-2 md:flex"
          >
            <Icon icon="lucide:x" width={20} height={20} />
          </button>
        </div>

        {/* Conteúdo com scroll */}
        <div className="flex-1 overflow-y-auto px-4 py-5 sm:px-6">
          {children}
        </div>

        {/* Rodapé fixo */}
        {footer && (
          <div className="flex flex-none items-center gap-2 border-t border-fin-border px-4 py-3.5 sm:justify-end sm:px-6">
            {footer}
          </div>
        )}
      </div>
    </>
  )
}
