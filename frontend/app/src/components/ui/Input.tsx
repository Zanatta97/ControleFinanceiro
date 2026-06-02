import type { InputHTMLAttributes } from 'react'

interface Props extends InputHTMLAttributes<HTMLInputElement> {
  label?: string
  error?: string
}

export default function Input({ label, error, className = '', id, ...rest }: Props) {
  const inputId = id ?? label?.toLowerCase().replace(/\s+/g, '-')
  return (
    <div className="flex flex-col gap-1">
      {label && <label htmlFor={inputId} className="text-sm font-medium text-fin-text-primary">{label}</label>}
      <input
        id={inputId}
        {...rest}
        className={`rounded-lg border px-3 py-2 text-sm bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted shadow-sm transition focus:outline-none focus:shadow-fin-focus ${error ? 'border-fin-negative' : 'border-fin-border'} ${className}`}
      />
      {error && <span className="text-xs text-fin-negative">{error}</span>}
    </div>
  )
}
