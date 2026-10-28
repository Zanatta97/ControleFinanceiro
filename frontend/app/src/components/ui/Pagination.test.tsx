import { describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Pagination from './Pagination'

function renderizar(pagina: number, totalPaginas = 3, total = 45, itensPorPagina = 20) {
  const onPagina = vi.fn()
  const utils = render(
    <Pagination pagina={pagina} totalPaginas={totalPaginas} total={total} itensPorPagina={itensPorPagina} onPagina={onPagina} />,
  )
  return { ...utils, onPagina }
}

const botao = (nome: string) => screen.getByRole('button', { name: nome })

describe('Pagination', () => {
  it.each([0, 1])('com %i página(s) não renderiza nada', (totalPaginas) => {
    const { container } = renderizar(1, totalPaginas, 5)
    expect(container).toBeEmptyDOMElement()
  })

  it('mostra o intervalo de itens e a página atual', () => {
    renderizar(2)
    expect(screen.getByText('21–40 de 45')).toBeInTheDocument()
    expect(screen.getByText('2 / 3')).toBeInTheDocument()
  })

  it('na última página, o fim do intervalo é o total', () => {
    renderizar(3)
    expect(screen.getByText('41–45 de 45')).toBeInTheDocument()
  })

  it('na primeira página, desabilita voltar e primeira', () => {
    renderizar(1)
    expect(botao('«')).toBeDisabled()
    expect(botao('‹')).toBeDisabled()
    expect(botao('›')).toBeEnabled()
    expect(botao('»')).toBeEnabled()
  })

  it('na última página, desabilita avançar e última', () => {
    renderizar(3)
    expect(botao('«')).toBeEnabled()
    expect(botao('‹')).toBeEnabled()
    expect(botao('›')).toBeDisabled()
    expect(botao('»')).toBeDisabled()
  })

  it('cada botão pede a página certa', async () => {
    const { onPagina } = renderizar(2)
    await userEvent.click(botao('«'))
    await userEvent.click(botao('‹'))
    await userEvent.click(botao('›'))
    await userEvent.click(botao('»'))
    expect(onPagina.mock.calls.map(([p]) => p)).toEqual([1, 1, 3, 3])
  })

  it('botão desabilitado não dispara onPagina', async () => {
    const { onPagina } = renderizar(1)
    await userEvent.click(botao('‹'))
    expect(onPagina).not.toHaveBeenCalled()
  })
})
