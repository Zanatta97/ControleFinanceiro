interface Props {
  type: 'error' | 'success' | 'info'
  message: string
}

const styles = {
  error: 'bg-fin-negative-soft border-fin-negative text-fin-negative',
  success: 'bg-fin-positive-soft border-fin-positive text-fin-positive',
  info: 'bg-fin-brand-soft border-fin-brand text-fin-brand',
}

export default function Alert({ type, message }: Props) {
  return (
    <div className={`rounded-lg border px-4 py-3 text-sm ${styles[type]}`}>
      {message}
    </div>
  )
}
