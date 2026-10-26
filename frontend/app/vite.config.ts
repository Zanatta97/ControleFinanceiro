import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    react(),
    VitePWA({
      // 'prompt': a versão nova só entra quando o usuário clicar em "Atualizar"
      // (o 'autoUpdate' recarregaria a página e poderia perder um lançamento sendo digitado).
      registerType: 'prompt',
      // O registro é feito pelo hook useRegisterSW, no componente AvisoAtualizacao.
      injectRegister: false,
      // Service worker desligado no `pnpm dev`, para não interferir no proxy nem no HMR.
      devOptions: { enabled: false },
      manifest: {
        name: 'Controle Financeiro',
        short_name: 'Finanças',
        description: 'Controle suas contas, transações e orçamentos.',
        lang: 'pt-BR',
        start_url: '/',
        scope: '/',
        display: 'standalone',
        theme_color: '#2D5BE3',
        background_color: '#1F45C2',
        icons: [
          { src: '/icons/icon-192.png', sizes: '192x192', type: 'image/png', purpose: 'any' },
          { src: '/icons/icon-512.png', sizes: '512x512', type: 'image/png', purpose: 'any' },
          { src: '/icons/icon-maskable-512.png', sizes: '512x512', type: 'image/png', purpose: 'maskable' },
        ],
      },
      workbox: {
        // Só os arquivos do build. Sem runtimeCaching: a API nunca é cacheada (e não há modo offline).
        globPatterns: ['**/*.{js,css,html,png,svg,ico,webmanifest}'],
        navigateFallback: 'index.html',
        navigateFallbackDenylist: [/^\/api\//],
        cleanupOutdatedCaches: true,
      },
    }),
  ],
  server: {
    proxy: {
      '/api': {
        target: 'https://localhost:7234',
        changeOrigin: true,
        secure: false, // certificado autoassinado do .NET em dev
      },
    },
  },
})
