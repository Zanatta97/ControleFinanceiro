import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
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
