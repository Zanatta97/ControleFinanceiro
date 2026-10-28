import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'

interface ThemeContextValue {
  isDark: boolean
  toggleTheme: () => void
}

const ThemeContext = createContext<ThemeContextValue>({ isDark: false, toggleTheme: () => {} })

/**
 * Tema claro/escuro. A escolha do usuário fica no localStorage ('theme'); sem escolha salva, segue a
 * preferência do sistema operacional. O tema é aplicado pela classe `dark` no elemento <html>.
 */
export function ThemeProvider({ children }: { children: ReactNode }) {
  const [isDark, setIsDark] = useState<boolean>(() => {
    const stored = localStorage.getItem('theme')
    if (stored === 'dark') return true
    if (stored === 'light') return false
    return window.matchMedia('(prefers-color-scheme: dark)').matches
  })

  useEffect(() => {
    const root = document.documentElement
    if (isDark) {
      root.classList.add('dark')
    } else {
      root.classList.remove('dark')
    }

    // Barra do navegador / app instalado acompanha a cor de superfície do tema (--fin-surface)
    const superficie = getComputedStyle(root).getPropertyValue('--fin-surface').trim()
    document
      .querySelector('meta[name="theme-color"]')
      ?.setAttribute('content', superficie || (isDark ? '#1C1F29' : '#FFFFFF'))
  }, [isDark])

  function toggleTheme() {
    setIsDark((prev) => {
      const next = !prev
      localStorage.setItem('theme', next ? 'dark' : 'light')
      return next
    })
  }

  return (
    <ThemeContext.Provider value={{ isDark, toggleTheme }}>
      {children}
    </ThemeContext.Provider>
  )
}

/** Tema atual e a função que alterna entre claro e escuro (salvando a escolha). */
export function useTheme() {
  return useContext(ThemeContext)
}
