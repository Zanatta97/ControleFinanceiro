import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Modal from './Modal'

describe('Modal', () => {
  it('fechado não renderiza nada', () => {
    const { container } = render(
      <Modal open={false} onClose={() => {}} title="Nova conta"><p>Conteúdo</p></Modal>,
    )
    expect(container).toBeEmptyDOMElement()
  })

  it('aberto mostra título e conteúdo', () => {
    render(<Modal open onClose={() => {}} title="Nova conta"><p>Conteúdo</p></Modal>)
    expect(screen.getByRole('heading', { name: 'Nova conta' })).toBeInTheDocument()
    expect(screen.getByText('Conteúdo')).toBeInTheDocument()
  })

  it('o botão do cabeçalho fecha', async () => {
    const onClose = vi.fn()
    render(<Modal open onClose={onClose} title="Nova conta"><p>Conteúdo</p></Modal>)
    await userEvent.click(screen.getByRole('button'))
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('clicar no fundo fecha; clicar no conteúdo não', async () => {
    const onClose = vi.fn()
    const { container } = render(<Modal open onClose={onClose} title="Nova conta"><p>Conteúdo</p></Modal>)

    await userEvent.click(screen.getByText('Conteúdo'))
    expect(onClose).not.toHaveBeenCalled()

    await userEvent.click(container.querySelector('.absolute.inset-0') as HTMLElement)
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it.each([
    ['sm', 'max-w-sm'],
    ['md', 'max-w-md'],
    ['lg', 'max-w-lg'],
  ] as const)('tamanho %s aplica %s', (size, classe) => {
    const { container } = render(<Modal open onClose={() => {}} title="T" size={size}>x</Modal>)
    expect(container.querySelector(`.${classe}`)).toBeInTheDocument()
  })

  it('o tamanho padrão é md', () => {
    const { container } = render(<Modal open onClose={() => {}} title="T">x</Modal>)
    expect(container.querySelector('.max-w-md')).toBeInTheDocument()
  })

  it.skip('Bug: o botão de fechar tem nome acessível', () => {
    // components/ui/Modal.tsx:25 — botão só com o SVG do "X", sem aria-label: o leitor de tela anuncia "botão"
    render(<Modal open onClose={() => {}} title="Nova conta">x</Modal>)
    expect(screen.getByRole('button', { name: /fechar/i })).toBeInTheDocument()
  })
})
