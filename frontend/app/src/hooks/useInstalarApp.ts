import { useSyncExternalStore } from 'react'

/** Evento não padronizado do Chrome/Edge/Android que permite adiar o convite de instalação. */
interface BeforeInstallPromptEvent extends Event {
  prompt: () => Promise<void>
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed'; platform: string }>
}

interface EstadoInstalacao {
  convite: BeforeInstallPromptEvent | null
  instalado: boolean
}

function detectarInstalado(): boolean {
  // `navigator.standalone` só existe no Safari do iOS
  const navegadorIOS = navigator as Navigator & { standalone?: boolean }
  return window.matchMedia('(display-mode: standalone)').matches || navegadorIOS.standalone === true
}

function detectarIOS(): boolean {
  const ua = navigator.userAgent
  if (/iPhone|iPad|iPod/.test(ua)) return true
  // O iPadOS 13+ se apresenta como Mac; a diferença é a tela de toque
  return /Macintosh/.test(ua) && navigator.maxTouchPoints > 1
}

const ehIOS = detectarIOS()
let estado: EstadoInstalacao = { convite: null, instalado: detectarInstalado() }
const ouvintes = new Set<() => void>()

function atualizar(parcial: Partial<EstadoInstalacao>) {
  estado = { ...estado, ...parcial }
  ouvintes.forEach((ouvinte) => ouvinte())
}

// Os ouvintes ficam no nível do módulo, e não no hook: o `beforeinstallprompt` costuma
// disparar logo no carregamento, antes de o menu do usuário existir (ex.: na tela de login).
window.addEventListener('beforeinstallprompt', (evento) => {
  evento.preventDefault()
  atualizar({ convite: evento as BeforeInstallPromptEvent })
})

window.addEventListener('appinstalled', () => {
  atualizar({ convite: null, instalado: true })
})

window.matchMedia('(display-mode: standalone)').addEventListener('change', () => {
  atualizar({ instalado: detectarInstalado() })
})

function assinar(ouvinte: () => void) {
  ouvintes.add(ouvinte)
  return () => {
    ouvintes.delete(ouvinte)
  }
}

function lerEstado() {
  return estado
}

async function instalar(): Promise<void> {
  const convite = estado.convite
  if (!convite) return
  try {
    await convite.prompt()
    await convite.userChoice
  } catch {
    // O navegador recusa um convite já usado; não há o que mostrar ao usuário
  } finally {
    // O convite só pode ser usado uma vez; o navegador dispara outro se voltar a oferecer
    atualizar({ convite: null })
  }
}

/**
 * Estado da instalação do app (PWA).
 *
 * - `podeInstalar`: o navegador ofereceu o convite nativo (Android/desktop); `instalar()` o exibe.
 * - `ehIOS`: no iOS não há convite; a instalação é manual pelo menu Compartilhar.
 * - `instalado`: o app já está aberto como aplicativo instalado.
 */
export function useInstalarApp() {
  const { convite, instalado } = useSyncExternalStore(assinar, lerEstado)
  return {
    podeInstalar: convite !== null && !instalado,
    instalar,
    ehIOS,
    instalado,
  }
}
