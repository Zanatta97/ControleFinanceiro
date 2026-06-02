import type { SelectHTMLAttributes } from 'react'

interface Props extends SelectHTMLAttributes<HTMLSelectElement> {
  label?: string
  error?: string
  options: { value: string | number; label: string }[]
}

export default function Select({ label, error, options, className = '', id, ...rest }: Props) {
  const selectId = id ?? label?.toLowerCase().replace(/\s+/g, '-')
  return (
    <div className="flex flex-col gap-1">
      {label && <label htmlFor={selectId} className="text-sm font-medium text-fin-text-primary">{label}</label>}
      <select
        id={selectId}
        {...rest}
        className={`rounded-lg border px-3 py-2 text-sm bg-fin-surface text-fin-text-primary shadow-sm transition focus:outline-none focus:shadow-fin-focus ${error ? 'border-fin-negative' : 'border-fin-border'} ${className}`}
      >
        {options.map((o) => (
          <option key={o.value} value={o.value}>{o.label}</option>
        ))}
      </select>
      {error && <span className="text-xs text-fin-negative">{error}</span>}
    </div>
  )
}
