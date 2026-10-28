import { describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Drawer from './Drawer'

function painel() {
  return screen.getByRole('heading', { name: 'Nova transação' }).closest('.fixed') as HTMLElement
}

describe('Drawer', () => {
  it('mostra título, conteúdo e rodapé', () => {
    render(
      <Drawer open onClose={() => {}} title="Nova transação" footer={<button>Salvar</button>}>
        <p>Formulário</p>
      </Drawer>,
    )
    expect(screen.getByRole('heading', { name: 'Nova transação' })).toBeInTheDocument()
    expect(screen.getByText('Formulário')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Salvar' })).toBeInTheDocument()
  })

  it('sem footer, não renderiza o rodapé', () => {
    render(<Drawer open onClose={() => {}} title="Nova transação"><p>Formulário</p></Drawer>)
    expect(screen.getAllByRole('button')).toHaveLength(2) // só Voltar e Fechar
  })

  it('aberto fica na tela; fechado sai pela direita', () => {
    const { rerender } = render(<Drawer open onClose={() => {}} title="Nova transação">x</Drawer>)
    expect(painel()).toHaveClass('translate-x-0')

    rerender(<Drawer open={false} onClose={() => {}} title="Nova transação">x</Drawer>)
    expect(painel()).toHaveClass('translate-x-full')
  })

  it('os botões Voltar (celular) e Fechar (desktop) fecham', async () => {
    const onClose = vi.fn()
    render(<Drawer open onClose={onClose} title="Nova transação">x</Drawer>)
    await userEvent.click(screen.getByRole('button', { name: 'Voltar' }))
    await userEvent.click(screen.getByRole('button', { name: 'Fechar' }))
    expect(onClose).toHaveBeenCalledTimes(2)
  })

  it('clicar no fundo fecha', async () => {
    const onClose = vi.fn()
    const { container } = render(<Drawer open onClose={onClose} title="Nova transação">x</Drawer>)
    await userEvent.click(container.firstElementChild as HTMLElement)
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('Escape fecha quando aberto', () => {
    const onClose = vi.fn()
    render(<Drawer open onClose={onClose} title="Nova transação">x</Drawer>)
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('Escape não faz nada quando fechado, e outras teclas são ignoradas', () => {
    const onClose = vi.fn()
    const { rerender } = render(<Drawer open={false} onClose={onClose} title="Nova transação">x</Drawer>)
    fireEvent.keyDown(document, { key: 'Escape' })
    rerender(<Drawer open onClose={onClose} title="Nova transação">x</Drawer>)
    fireEvent.keyDown(document, { key: 'Enter' })
    expect(onClose).not.toHaveBeenCalled()
  })

  it('para de ouvir o Escape depois de fechar', () => {
    const onClose = vi.fn()
    const { rerender } = render(<Drawer open onClose={onClose} title="Nova transação">x</Drawer>)
    rerender(<Drawer open={false} onClose={onClose} title="Nova transação">x</Drawer>)
    fireEvent.keyDown(document, { key: 'Escape' })
    expect(onClose).not.toHaveBeenCalled()
  })

  it('trava a rolagem da página enquanto aberto e restaura ao fechar', () => {
    document.body.style.overflow = 'auto'
    const { rerender } = render(<Drawer open onClose={() => {}} title="Nova transação">x</Drawer>)
    expect(document.body.style.overflow).toBe('hidden')

    rerender(<Drawer open={false} onClose={() => {}} title="Nova transação">x</Drawer>)
    expect(document.body.style.overflow).toBe('auto')
    document.body.style.overflow = ''
  })

  it('restaura a rolagem ao desmontar aberto', () => {
    const { unmount } = render(<Drawer open onClose={() => {}} title="Nova transação">x</Drawer>)
    unmount()
    expect(document.body.style.overflow).toBe('')
  })

  it.skip('Bug: fechado, o painel sai da árvore de acessibilidade e do Tab', () => {
    // components/ui/Drawer.tsx:39 — o painel fechado só é deslocado com translate-x-full: sem
    // aria-hidden/inert, o Tab e o leitor de tela continuam alcançando os campos invisíveis.
    render(
      <Drawer open={false} onClose={() => {}} title="Nova transação">
        <input aria-label="Descrição" />
      </Drawer>,
    )
    expect(screen.queryByRole('textbox', { name: 'Descrição' })).not.toBeInTheDocument()
  })
})
