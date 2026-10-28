import '@testing-library/jest-dom/vitest'
import { createElement } from 'react'
import { afterEach, vi } from 'vitest'
import { cleanup } from '@testing-library/react'

// O <Icon> do Iconify busca o SVG na internet; nos testes vira um <span> com o nome do ícone.
vi.mock('@iconify/react', () => ({
  Icon: ({ icon }: { icon: string }) => createElement('span', { 'data-icon': icon }),
}))

// O jsdom não implementa matchMedia; o padrão é "nenhuma media query bate".
if (!window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    configurable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addEventListener: () => {},
      removeEventListener: () => {},
      addListener: () => {},
      removeListener: () => {},
      dispatchEvent: () => false,
    }),
  })
}

afterEach(() => {
  cleanup()
  localStorage.clear()
})
