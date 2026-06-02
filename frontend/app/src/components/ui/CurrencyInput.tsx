interface Props {
  label?: string
  value: number
  onChange: (value: number) => void
  error?: string
}

export default function CurrencyInput({ label, value, onChange, error }: Props) {
  const inputId = label?.toLowerCase().replace(/\s+/g, '-')

  // Exibe "1.234,56" a partir do número 1234.56
  function toDisplay(n: number): string {
    const cents = Math.round(Math.abs(n) * 100)
    const str = String(cents).padStart(3, '0')
    const reais = str.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
    const centavos = str.slice(-2)
    return `${reais},${centavos}`
  }

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (e.key === 'Backspace') {
      const cents = Math.round(value * 100)
      onChange(Math.floor(cents / 10) / 100)
      e.preventDefault()
      return
    }
    if (!/^\d$/.test(e.key)) return
    e.preventDefault()
    const cents = Math.round(value * 100)
    const next = cents * 10 + parseInt(e.key)
    if (next > 9_999_999_99) return // limite 99.999.999,99
    onChange(next / 100)
  }

  return (
    <div className="flex flex-col gap-1">
      {label && (
        <label htmlFor={inputId} className="text-sm font-medium text-fin-text-primary">
          {label}
        </label>
      )}
      <div
        className={`flex items-center rounded-lg border px-3 py-2 text-sm bg-fin-surface shadow-sm transition focus-within:shadow-fin-focus focus-within:border-fin-brand ${error ? 'border-fin-negative' : 'border-fin-border'}`}
      >
        <span className="mr-1 text-fin-text-muted select-none">R$</span>
        <input
          id={inputId}
          type="text"
          inputMode="numeric"
          value={toDisplay(value)}
          onKeyDown={handleKeyDown}
          onChange={() => {}} // controlado via keyDown
          className="flex-1 bg-transparent outline-none text-fin-text-primary"
        />
      </div>
      {error && <span className="text-xs text-fin-negative">{error}</span>}
    </div>
  )
}
