// Substituto do módulo virtual `virtual:pwa-register/react`, que só existe com o plugin PWA
// (fora da config de testes). Os testes que dependem dele fazem vi.mock com o comportamento desejado.
export function useRegisterSW() {
  return {
    needRefresh: [false, () => {}] as const,
    offlineReady: [false, () => {}] as const,
    updateServiceWorker: async () => {},
  }
}
