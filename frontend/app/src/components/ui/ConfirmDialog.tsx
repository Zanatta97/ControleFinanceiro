import type { ReactNode } from 'react'
import { Icon } from '@iconify/react'

export type ConfirmVariant = 'delete' | 'warning' | 'info'

interface Props {
  open: boolean
  variant?: ConfirmVariant
  title: string
  message: ReactNode
  confirmLabel?: string
  cancelLabel?: string
  loading?: boolean
  onConfirm: () => void
  onClose: () => void
}

const config: Record<ConfirmVariant, { icon: string; iconSoft: string; iconColor: string; confirmBg: string }> = {
  delete: {
    icon: 'lucide:trash-2',
    iconSoft: 'bg-fin-negative-soft',
    iconColor: 'text-fin-negative',
    confirmBg: 'bg-fin-negative hover:bg-fin-negative-hover',
  },
  warning: {
    icon: 'lucide:alert-triangle',
    iconSoft: 'bg-fin-warning-soft',
    iconColor: 'text-fin-warning',
    confirmBg: 'bg-fin-warning hover:bg-fin-warning-hover',
  },
  info: {
    icon: 'lucide:check-circle',
    iconSoft: 'bg-fin-brand-soft',
    iconColor: 'text-fin-brand',
    confirmBg: 'bg-fin-brand hover:bg-fin-brand-hover',
  },
}

export default function ConfirmDialog({
  open,
  variant = 'delete',
  title,
  message,
  confirmLabel = 'Confirmar',
  cancelLabel = 'Cancelar',
  loading = false,
  onConfirm,
  onClose,
}: Props) {
  if (!open) return null
  const cfg = config[variant]

  return (
    <div
      className="fixed inset-0 z-[60] flex items-center justify-center p-6 animate-fin-fade"
      style={{ backgroundColor: 'var(--fin-overlay)' }}
      onClick={onClose}
    >
      <div
        onClick={(e) => e.stopPropagation()}
        className="w-full max-w-[400px] overflow-hidden rounded-[15px] bg-fin-surface shadow-[0_20px_50px_rgba(0,0,0,0.3)] animate-fin-pop"
      >
        <div className="px-[22px] pt-[22px]">
          <div className="mb-3.5 flex items-center gap-3">
            <div className={`flex h-[42px] w-[42px] flex-none items-center justify-center rounded-[11px] ${cfg.iconSoft} ${cfg.iconColor}`}>
              <Icon icon={cfg.icon} width={21} height={21} />
            </div>
            <h2 className="text-[16.5px] font-bold text-fin-text-primary">{title}</h2>
          </div>
          <div className="text-[13.5px] leading-[1.55] text-fin-text-secondary">{message}</div>
        </div>
        <div className="flex justify-end gap-2 px-[22px] py-[18px]">
          <button
            onClick={onClose}
            disabled={loading}
            className="h-[38px] rounded-[9px] border border-fin-border bg-fin-surface px-4 text-[13.5px] font-semibold text-fin-text-primary transition hover:bg-fin-surface-2 disabled:opacity-50"
          >
            {cancelLabel}
          </button>
          <button
            onClick={onConfirm}
            disabled={loading}
            className={`h-[38px] rounded-[9px] px-[18px] text-[13.5px] font-semibold text-white transition disabled:opacity-50 ${cfg.confirmBg}`}
          >
            {loading ? '...' : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}
