import { useState } from 'react'
import { Icon } from '@iconify/react'
import { useRegisterSW } from 'virtual:pwa-register/react'
import Button from './ui/Button'

/**
 * Registra o service worker e avisa quando há versão nova do app.
 *
 * A atualização só acontece quando o usuário clica em "Atualizar" (registerType 'prompt'),
 * para não recarregar a página no meio de um lançamento. Montado uma única vez, no App.
 */
export default function AvisoAtualizacao() {
  const {
    needRefresh: [precisaAtualizar, setPrecisaAtualizar],
    updateServiceWorker,
  } = useRegisterSW()
  const [atualizando, setAtualizando] = useState(false)

  if (!precisaAtualizar) return null

  async function handleAtualizar() {
    setAtualizando(true)
    // Ativa o service worker novo; a página recarrega quando ele assume o controle
    await updateServiceWorker(true)
  }

  // No celular fica acima da MobileTabBar (62px + barrinha do iPhone); no desktop, no canto inferior
  return (
    <div
      role="status"
      aria-live="polite"
      className="fixed inset-x-3 bottom-[calc(62px+env(safe-area-inset-bottom)+12px)] z-40 flex items-center gap-3 rounded-xl border border-fin-border bg-fin-surface px-3.5 py-3 shadow-[0_14px_34px_rgba(0,0,0,0.16)] animate-fin-pop lg:inset-x-auto lg:bottom-6 lg:right-6 lg:w-[360px]"
    >
      <span className="flex h-9 w-9 flex-none items-center justify-center rounded-full bg-fin-brand-soft text-fin-brand">
        <Icon icon="lucide:refresh-cw" width={17} height={17} />
      </span>
      <div className="min-w-0 flex-1">
        <p className="text-[13.5px] font-semibold text-fin-text-primary">Nova versão disponível</p>
        <p className="mt-0.5 text-[11.5px] text-fin-text-muted">Atualize para carregar as melhorias.</p>
      </div>
      <Button size="sm" onClick={handleAtualizar} loading={atualizando}>
        Atualizar
      </Button>
      <button
        onClick={() => setPrecisaAtualizar(false)}
        aria-label="Fechar aviso"
        title="Fechar"
        className="flex h-[30px] w-[30px] flex-none items-center justify-center rounded-lg text-fin-text-muted hover:bg-fin-surface-2"
      >
        <Icon icon="lucide:x" width={16} height={16} />
      </button>
    </div>
  )
}
