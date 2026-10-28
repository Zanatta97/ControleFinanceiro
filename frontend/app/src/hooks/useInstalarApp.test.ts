import { beforeEach, describe, expect, it, vi } from 'vitest'
import { act, renderHook } from '@testing-library/react'

// O hook guarda o estado no módulo: cada teste importa uma cópia nova,
// depois de preparar navegador e media queries.
async function carregarHook() {
  vi.resetModules()
  const { useInstalarApp } = await import('./useInstalarApp')
  return renderHook(() => useInstalarApp())
}

function simularStandalone(ativo: boolean) {
  vi.spyOn(window, 'matchMedia').mockImplementation((query: string) => ({
    matches: ativo && query === '(display-mode: standalone)',
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  }))
}

function simularNavegador(dados: { userAgent: string; maxTouchPoints?: number; standalone?: boolean }) {
  vi.stubGlobal('navigator', { maxTouchPoints: 0, ...dados })
}

function criarConvite(opcoes: { falhar?: boolean } = {}) {
  const evento = new Event('beforeinstallprompt', { cancelable: true })
  const prompt = opcoes.falhar ? vi.fn().mockRejectedValue(new Error('já usado')) : vi.fn().mockResolvedValue(undefined)
  return Object.assign(evento, {
    prompt,
    userChoice: Promise.resolve({ outcome: 'accepted' as const, platform: 'web' }),
  })
}

const UA_CHROME = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/130.0 Safari/537.36'

describe('useInstalarApp', () => {
  beforeEach(() => {
    simularStandalone(false)
    simularNavegador({ userAgent: UA_CHROME })
  })

  it('sem convite do navegador, não pode instalar', async () => {
    const { result } = await carregarHook()
    expect(result.current.podeInstalar).toBe(false)
    expect(result.current.instalado).toBe(false)
    expect(result.current.ehIOS).toBe(false)
  })

  it('guarda o convite do navegador e libera a instalação', async () => {
    const { result } = await carregarHook()
    const convite = criarConvite()
    act(() => { window.dispatchEvent(convite) })

    expect(convite.defaultPrevented).toBe(true)
    expect(result.current.podeInstalar).toBe(true)
  })

  it('instalar() exibe o convite e o descarta depois (só pode ser usado uma vez)', async () => {
    const { result } = await carregarHook()
    const convite = criarConvite()
    act(() => { window.dispatchEvent(convite) })

    await act(() => result.current.instalar())
    expect(convite.prompt).toHaveBeenCalledTimes(1)
    expect(result.current.podeInstalar).toBe(false)
  })

  it('instalar() sem convite não faz nada', async () => {
    const { result } = await carregarHook()
    await expect(result.current.instalar()).resolves.toBeUndefined()
    expect(result.current.podeInstalar).toBe(false)
  })

  it('convite recusado pelo navegador não estoura erro e é descartado', async () => {
    const { result } = await carregarHook()
    const convite = criarConvite({ falhar: true })
    act(() => { window.dispatchEvent(convite) })

    await act(() => result.current.instalar())
    expect(result.current.podeInstalar).toBe(false)
  })

  it('após "appinstalled", marca como instalado e esconde o convite', async () => {
    const { result } = await carregarHook()
    act(() => { window.dispatchEvent(criarConvite()) })
    act(() => { window.dispatchEvent(new Event('appinstalled')) })

    expect(result.current.instalado).toBe(true)
    expect(result.current.podeInstalar).toBe(false)
  })

  it('aberto como app (display-mode standalone) já conta como instalado', async () => {
    simularStandalone(true)
    const { result } = await carregarHook()
    expect(result.current.instalado).toBe(true)
  })

  it('com convite mas já instalado, não oferece instalar', async () => {
    simularStandalone(true)
    const { result } = await carregarHook()
    act(() => { window.dispatchEvent(criarConvite()) })
    expect(result.current.podeInstalar).toBe(false)
  })

  it('Safari do iOS com navigator.standalone conta como instalado', async () => {
    simularNavegador({ userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)', standalone: true })
    const { result } = await carregarHook()
    expect(result.current.instalado).toBe(true)
    expect(result.current.ehIOS).toBe(true)
  })

  it.each([
    ['iPhone', 'Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X)', 0, true],
    ['iPad antigo', 'Mozilla/5.0 (iPad; CPU OS 12_0 like Mac OS X)', 0, true],
    ['iPadOS 13+ (se apresenta como Mac, com toque)', 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)', 5, true],
    ['Mac de verdade (sem toque)', 'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7)', 0, false],
    ['Android', 'Mozilla/5.0 (Linux; Android 14; Pixel 8)', 5, false],
  ])('detecta iOS: %s', async (_nome, userAgent, maxTouchPoints, esperado) => {
    simularNavegador({ userAgent, maxTouchPoints })
    const { result } = await carregarHook()
    expect(result.current.ehIOS).toBe(esperado)
  })
})
