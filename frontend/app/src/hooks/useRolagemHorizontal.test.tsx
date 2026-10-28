import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { act, fireEvent, render, screen } from '@testing-library/react'
import { useRolagemHorizontal } from './useRolagemHorizontal'

// O jsdom não calcula layout: as medidas da rolagem vêm destas variáveis
const medidas = { scrollLeft: 0, clientWidth: 100, scrollWidth: 100 }

let callbackResize: (() => void) | null = null
const observe = vi.fn()
const disconnect = vi.fn()

class ResizeObserverFalso {
  constructor(cb: () => void) {
    callbackResize = cb
  }
  observe = observe
  unobserve = vi.fn()
  disconnect = disconnect
}

function Rolagem({ chave }: { chave?: unknown }) {
  const { ref, classeFade } = useRolagemHorizontal<HTMLDivElement>(chave)
  return <div ref={ref} data-testid="rolagem" className={classeFade} />
}

describe('useRolagemHorizontal', () => {
  beforeEach(() => {
    Object.assign(medidas, { scrollLeft: 0, clientWidth: 100, scrollWidth: 100 })
    callbackResize = null
    observe.mockClear()
    disconnect.mockClear()
    vi.stubGlobal('ResizeObserver', ResizeObserverFalso)
    vi.spyOn(HTMLElement.prototype, 'scrollLeft', 'get').mockImplementation(() => medidas.scrollLeft)
    vi.spyOn(HTMLElement.prototype, 'clientWidth', 'get').mockImplementation(() => medidas.clientWidth)
    vi.spyOn(HTMLElement.prototype, 'scrollWidth', 'get').mockImplementation(() => medidas.scrollWidth)
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  it('sem conteúdo escondido, só a classe base', () => {
    render(<Rolagem />)
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem')
  })

  it('com conteúdo à direita, liga o degradê da direita', () => {
    medidas.scrollWidth = 300
    render(<Rolagem />)
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem fade-rolagem-direita')
  })

  it('no meio da rolagem, liga os dois lados', () => {
    medidas.scrollWidth = 300
    render(<Rolagem />)
    medidas.scrollLeft = 100
    fireEvent.scroll(screen.getByTestId('rolagem'))
    expect(screen.getByTestId('rolagem')).toHaveAttribute(
      'class',
      'fade-rolagem fade-rolagem-esquerda fade-rolagem-direita',
    )
  })

  it('no fim da rolagem, só o da esquerda', () => {
    medidas.scrollWidth = 300
    render(<Rolagem />)
    medidas.scrollLeft = 200
    fireEvent.scroll(screen.getByTestId('rolagem'))
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem fade-rolagem-esquerda')
  })

  it('tolera 1px de arredondamento de subpixel nas bordas', () => {
    // 1px escondido de cada lado não conta como conteúdo escondido
    medidas.scrollWidth = 102
    medidas.scrollLeft = 1
    render(<Rolagem />)
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem')
  })

  it('recalcula quando o contêiner muda de tamanho', () => {
    medidas.scrollWidth = 300
    render(<Rolagem />)
    expect(observe).toHaveBeenCalledWith(screen.getByTestId('rolagem'))

    medidas.clientWidth = 300
    act(() => callbackResize?.())
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem')
  })

  it('recalcula quando a chave muda', () => {
    const { rerender } = render(<Rolagem chave="a" />)
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem')

    medidas.scrollWidth = 500
    rerender(<Rolagem chave="b" />)
    expect(screen.getByTestId('rolagem')).toHaveAttribute('class', 'fade-rolagem fade-rolagem-direita')
  })

  it('para de ouvir rolagem e redimensionamento ao desmontar', () => {
    const { unmount } = render(<Rolagem />)
    const el = screen.getByTestId('rolagem')
    const remover = vi.spyOn(el, 'removeEventListener')
    unmount()
    expect(disconnect).toHaveBeenCalled()
    expect(remover).toHaveBeenCalledWith('scroll', expect.any(Function))
  })
})
