import { useEffect } from 'react'

interface Props {
  message: string
  type?: 'error' | 'success' | 'info'
  onClose: () => void
  duration?: number
}

export default function Toast({ message, type = 'error', onClose, duration = 5000 }: Props) {
  useEffect(() => {
    const t = setTimeout(onClose, duration)
    return () => clearTimeout(t)
  }, [message, duration, onClose])

  const bgColor =
    type === 'error'
      ? 'var(--fin-negative)'
      : type === 'success'
      ? 'var(--fin-positive)'
      : 'var(--fin-brand)'

  return (
    <div
      className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 flex items-center gap-3 rounded-xl px-5 py-3 shadow-lg text-sm font-medium max-w-md w-max"
      style={{ backgroundColor: bgColor, color: '#fff' }}
    >
      <span>{message}</span>
      <button onClick={onClose} className="ml-2 opacity-70 hover:opacity-100 text-lg leading-none">×</button>
    </div>
  )
}
