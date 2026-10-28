import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { act, render, renderHook, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import type { ReactNode } from 'react'
import { ThemeProvider, useTheme } from './ThemeContext'

const wrapper = ({ children }: { children: ReactNode }) => <ThemeProvider>{children}</ThemeProvider>

function preferenciaDoSistema(escuro: boolean) {
  vi.spyOn(window, 'matchMedia').mockImplementation((query: string) => ({
    matches: escuro && query === '(prefers-color-scheme: dark)',
    media: query,
    onchange: null,
    addEventListener: () => {},
    removeEventListener: () => {},
    addListener: () => {},
    removeListener: () => {},
    dispatchEvent: () => false,
  }))
}

const metaThemeColor = () => document.querySelector('meta[name="theme-color"]')

describe('ThemeContext', () => {
  beforeEach(() => {
    preferenciaDoSistema(false)
    const meta = document.createElement('meta')
    meta.name = 'theme-color'
    meta.content = '#2D5BE3'
    document.head.appendChild(meta)
  })

  afterEach(() => {
    metaThemeColor()?.remove()
    document.documentElement.classList.remove('dark')
    document.documentElement.style.removeProperty('--fin-surface')
  })

  it('usa o tema escuro salvo', () => {
    localStorage.setItem('theme', 'dark')
    const { result } = renderHook(() => useTheme(), { wrapper })
    expect(result.current.isDark).toBe(true)
    expect(document.documentElement).toHaveClass('dark')
  })

  it('o tema claro salvo vence a preferência do sistema', () => {
    localStorage.setItem('theme', 'light')
    preferenciaDoSistema(true)
    const { result } = renderHook(() => useTheme(), { wrapper })
    expect(result.current.isDark).toBe(false)
    expect(document.documentElement).not.toHaveClass('dark')
  })

  it('sem preferência salva, segue o sistema (escuro)', () => {
    preferenciaDoSistema(true)
    const { result } = renderHook(() => useTheme(), { wrapper })
    expect(result.current.isDark).toBe(true)
  })

  it('sem preferência salva, segue o sistema (claro)', () => {
    const { result } = renderHook(() => useTheme(), { wrapper })
    expect(result.current.isDark).toBe(false)
  })

  it('valor desconhecido salvo é ignorado e segue o sistema', () => {
    localStorage.setItem('theme', 'azul')
    preferenciaDoSistema(true)
    const { result } = renderHook(() => useTheme(), { wrapper })
    expect(result.current.isDark).toBe(true)
  })

  it('toggleTheme alterna, salva a escolha e atualiza a classe do <html>', () => {
    const { result } = renderHook(() => useTheme(), { wrapper })

    act(() => result.current.toggleTheme())
    expect(result.current.isDark).toBe(true)
    expect(localStorage.getItem('theme')).toBe('dark')
    expect(document.documentElement).toHaveClass('dark')

    act(() => result.current.toggleTheme())
    expect(result.current.isDark).toBe(false)
    expect(localStorage.getItem('theme')).toBe('light')
    expect(document.documentElement).not.toHaveClass('dark')
  })

  it('sem a variável --fin-surface, a cor da barra usa o padrão de cada tema', () => {
    const { result } = renderHook(() => useTheme(), { wrapper })
    expect(metaThemeColor()).toHaveAttribute('content', '#FFFFFF')

    act(() => result.current.toggleTheme())
    expect(metaThemeColor()).toHaveAttribute('content', '#1C1F29')
  })

  it('a cor da barra acompanha a variável --fin-surface quando ela existe', () => {
    document.documentElement.style.setProperty('--fin-surface', '#123456')
    renderHook(() => useTheme(), { wrapper })
    expect(metaThemeColor()).toHaveAttribute('content', '#123456')
  })

  it('não quebra se a página não tiver a meta theme-color', () => {
    metaThemeColor()?.remove()
    expect(() => renderHook(() => useTheme(), { wrapper })).not.toThrow()
  })

  it('componentes filhos alternam o tema pelo contexto', async () => {
    function Botao() {
      const { isDark, toggleTheme } = useTheme()
      return <button onClick={toggleTheme}>{isDark ? 'Escuro' : 'Claro'}</button>
    }
    render(<ThemeProvider><Botao /></ThemeProvider>)

    await userEvent.click(screen.getByRole('button', { name: 'Claro' }))
    expect(screen.getByRole('button', { name: 'Escuro' })).toBeInTheDocument()
  })

  it('useTheme fora do provider devolve o padrão (claro) sem erro', () => {
    const { result } = renderHook(() => useTheme())
    expect(result.current.isDark).toBe(false)
    expect(() => result.current.toggleTheme()).not.toThrow()
  })
})
