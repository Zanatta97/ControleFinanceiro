import type { ReactNode } from 'react'

interface Props {
  open: boolean
  onClose: () => void
  title: string
  children: ReactNode
  size?: 'sm' | 'md' | 'lg'
}

const sizes = { sm: 'max-w-sm', md: 'max-w-md', lg: 'max-w-lg' }

export default function Modal({ open, onClose, title, children, size = 'md' }: Props) {
  if (!open) return null
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div
        className="absolute inset-0"
        style={{ backgroundColor: 'var(--fin-overlay)' }}
        onClick={onClose}
      />
      <div className={`relative w-full ${sizes[size]} rounded-xl bg-fin-surface border border-fin-border shadow-xl`}>
        <div className="flex items-center justify-between border-b border-fin-border px-6 py-4">
          <h2 className="text-lg font-semibold text-fin-text-primary">{title}</h2>
          <button onClick={onClose} className="rounded-lg p-1 hover:bg-fin-surface-2 text-fin-text-muted">
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
          </button>
        </div>
        <div className="px-6 py-5">{children}</div>
      </div>
    </div>
  )
}
