import type { ReactNode } from 'react'

interface Props {
  children: ReactNode
  className?: string
}

/** Contêiner com borda e fundo de superfície. */
export default function Card({ children, className = '' }: Props) {
  return (
    <div className={`rounded-xl border border-fin-border bg-fin-surface shadow-sm ${className}`}>
      {children}
    </div>
  )
}
