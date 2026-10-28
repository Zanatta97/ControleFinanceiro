import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import AvisoAtualizacao from './AvisoAtualizacao'

const pwa = vi.hoisted(() => ({
  precisaAtualizar: false,
  setPrecisaAtualizar: vi.fn(),
  updateServiceWorker: vi.fn(),
}))

// Módulo virtual do vite-plugin-pwa: não existe fora do build, então é simulado por inteiro
vi.mock('virtual:pwa-register/react', () => ({
  useRegisterSW: () => ({
    needRefresh: [pwa.precisaAtualizar, pwa.setPrecisaAtualizar],
    offlineReady: [false, vi.fn()],
    updateServiceWorker: pwa.updateServiceWorker,
  }),
}))

describe('AvisoAtualizacao', () => {
  beforeEach(() => {
    pwa.precisaAtualizar = false
    pwa.setPrecisaAtualizar.mockReset()
    pwa.updateServiceWorker.mockReset().mockResolvedValue(undefined)
  })

  it('sem versão nova, não mostra nada', () => {
    const { container } = render(<AvisoAtualizacao />)
    expect(container).toBeEmptyDOMElement()
  })

  it('com versão nova, avisa de forma acessível', () => {
    pwa.precisaAtualizar = true
    render(<AvisoAtualizacao />)
    const aviso = screen.getByRole('status')
    expect(aviso).toHaveAttribute('aria-live', 'polite')
    expect(screen.getByText('Nova versão disponível')).toBeInTheDocument()
    expect(screen.getByText('Atualize para carregar as melhorias.')).toBeInTheDocument()
  })

  it('"Atualizar" ativa o service worker novo e mostra carregando', async () => {
    pwa.precisaAtualizar = true
    pwa.updateServiceWorker.mockReturnValue(new Promise(() => {})) // a página recarrega antes de resolver
    render(<AvisoAtualizacao />)

    await userEvent.click(screen.getByRole('button', { name: 'Atualizar' }))

    expect(pwa.updateServiceWorker).toHaveBeenCalledWith(true)
    expect(screen.getByRole('button', { name: 'Atualizar' })).toBeDisabled()
  })

  it('fechar dispensa o aviso sem atualizar', async () => {
    pwa.precisaAtualizar = true
    render(<AvisoAtualizacao />)

    await userEvent.click(screen.getByRole('button', { name: 'Fechar aviso' }))

    expect(pwa.setPrecisaAtualizar).toHaveBeenCalledWith(false)
    expect(pwa.updateServiceWorker).not.toHaveBeenCalled()
  })
})
