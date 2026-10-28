import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ComponentProps } from 'react'
import ConfirmDialog from './ConfirmDialog'

function renderizar(props: Partial<ComponentProps<typeof ConfirmDialog>> = {}) {
  const onConfirm = vi.fn()
  const onClose = vi.fn()
  const utils = render(
    <ConfirmDialog
      open
      title="Excluir conta"
      message="Tem certeza?"
      onConfirm={onConfirm}
      onClose={onClose}
      {...props}
    />,
  )
  return { ...utils, onConfirm, onClose }
}

describe('ConfirmDialog', () => {
  it('fechado não renderiza nada', () => {
    const { container } = renderizar({ open: false })
    expect(container).toBeEmptyDOMElement()
  })

  it('aberto mostra título, mensagem e os botões padrão', () => {
    renderizar()
    expect(screen.getByRole('heading', { name: 'Excluir conta' })).toBeInTheDocument()
    expect(screen.getByText('Tem certeza?')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Confirmar' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Cancelar' })).toBeInTheDocument()
  })

  it('aceita mensagem em JSX e rótulos personalizados', () => {
    renderizar({
      message: <>Excluir <strong>Nubank</strong>?</>,
      confirmLabel: 'Excluir',
      cancelLabel: 'Voltar',
    })
    expect(screen.getByText('Nubank')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Excluir' })).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Voltar' })).toBeInTheDocument()
  })

  it('confirmar chama onConfirm', async () => {
    const { onConfirm, onClose } = renderizar()
    await userEvent.click(screen.getByRole('button', { name: 'Confirmar' }))
    expect(onConfirm).toHaveBeenCalledTimes(1)
    expect(onClose).not.toHaveBeenCalled()
  })

  it('cancelar chama onClose', async () => {
    const { onConfirm, onClose } = renderizar()
    await userEvent.click(screen.getByRole('button', { name: 'Cancelar' }))
    expect(onClose).toHaveBeenCalledTimes(1)
    expect(onConfirm).not.toHaveBeenCalled()
  })

  it('clicar no fundo fecha; clicar dentro da caixa não', async () => {
    const { onClose } = renderizar()
    await userEvent.click(screen.getByText('Tem certeza?'))
    expect(onClose).not.toHaveBeenCalled()

    const fundo = screen.getByRole('heading', { name: 'Excluir conta' }).closest('.fixed') as HTMLElement
    await userEvent.click(fundo)
    expect(onClose).toHaveBeenCalledTimes(1)
  })

  it('carregando desabilita os botões e troca o rótulo por "..."', () => {
    renderizar({ loading: true })
    const botoes = screen.getAllByRole('button')
    expect(botoes).toHaveLength(2)
    botoes.forEach((b) => expect(b).toBeDisabled())
    expect(screen.getByRole('button', { name: '...' })).toBeInTheDocument()
    expect(screen.queryByRole('button', { name: 'Confirmar' })).not.toBeInTheDocument()
  })

  it.each([
    ['delete', 'lucide:trash-2', 'bg-fin-negative'],
    ['warning', 'lucide:alert-triangle', 'bg-fin-warning'],
    ['info', 'lucide:check-circle', 'bg-fin-brand'],
  ] as const)('variante %s usa o ícone e a cor certos', (variant, icone, cor) => {
    const { container } = renderizar({ variant })
    expect(container.querySelector(`[data-icon="${icone}"]`)).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Confirmar' })).toHaveClass(cor)
  })

  it('a variante padrão é delete', () => {
    const { container } = renderizar()
    expect(container.querySelector('[data-icon="lucide:trash-2"]')).toBeInTheDocument()
  })
})
