import { useEffect, useRef, useState } from 'react'

/**
 * Acompanha a rolagem horizontal de um contêiner e devolve as classes do degradê
 * de borda (`fade-rolagem`, definidas no index.css), ligando o lado esquerdo e/ou
 * direito só quando há conteúdo escondido daquele lado.
 *
 * `chave` deve mudar quando o conteúdo do contêiner muda (ex.: outro grupo de abas),
 * para o cálculo ser refeito.
 */
export function useRolagemHorizontal<T extends HTMLElement>(chave?: unknown) {
  const ref = useRef<T>(null)
  const [bordas, setBordas] = useState({ esquerda: false, direita: false })

  useEffect(() => {
    const el = ref.current
    if (!el) return

    const atualizar = () => {
      // Margem de 1px para arredondamento de subpixel
      const esquerda = el.scrollLeft > 1
      const direita = el.scrollLeft + el.clientWidth < el.scrollWidth - 1
      setBordas((atual) =>
        atual.esquerda === esquerda && atual.direita === direita ? atual : { esquerda, direita },
      )
    }

    atualizar()
    el.addEventListener('scroll', atualizar, { passive: true })
    const observador = new ResizeObserver(atualizar)
    observador.observe(el)
    return () => {
      el.removeEventListener('scroll', atualizar)
      observador.disconnect()
    }
  }, [chave])

  const classeFade = [
    'fade-rolagem',
    bordas.esquerda && 'fade-rolagem-esquerda',
    bordas.direita && 'fade-rolagem-direita',
  ]
    .filter(Boolean)
    .join(' ')

  return { ref, classeFade }
}
