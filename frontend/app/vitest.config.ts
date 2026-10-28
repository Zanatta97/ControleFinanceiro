import { defineConfig } from 'vitest/config'
import react from '@vitejs/plugin-react'

// Config própria dos testes: sem o plugin PWA, que só interessa ao build.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      // Caminho iniciado em "/" é resolvido a partir da raiz do projeto
      'virtual:pwa-register/react': '/src/test/pwaRegisterStub.ts',
    },
  },
  test: {
    environment: 'jsdom',
    // Fuso fixo para os testes de data não dependerem da máquina (o app roda no Brasil)
    env: { TZ: 'America/Sao_Paulo' },
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    restoreMocks: true,
    unstubGlobals: true,
  },
})
