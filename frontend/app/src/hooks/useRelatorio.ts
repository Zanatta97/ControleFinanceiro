import { useEffect, useRef, useState } from 'react'
import type { AxiosResponse } from 'axios'
import type { ApiResponse } from '../types/api'

export type EstadoRelatorio<T> =
  | { status: 'ocioso' }
  | { status: 'carregando' }
  | { status: 'vazio'; mensagem: string | null }
  | { status: 'erro'; mensagem: string }
  | { status: 'sucesso'; dados: T }

interface Opcoes<T> {
  /** Quando false, não chama a API (ex.: filtro incompleto). */
  habilitado?: boolean
  /** Decide se um 200 sem conteúdo útil deve virar estado vazio. Listas vazias já contam como vazio. */
  vazio?: (dados: T) => boolean
}

type RespostaErro = { response?: { status?: number; data?: unknown } }

const MENSAGEM_ROTA_INEXISTENTE = 'Relatório indisponível na API. Verifique se o backend está atualizado.'

/** Mensagem do corpo quando ele é um ApiResponse com `errorMessage` preenchido; null caso contrário. */
function mensagemApiResponse(data: unknown): string | null {
  if (data == null || typeof data !== 'object' || !('errorMessage' in data)) return null
  const { errorMessage } = data as Partial<ApiResponse<unknown>>
  return typeof errorMessage === 'string' && errorMessage.trim() !== '' ? errorMessage : null
}

/**
 * Busca de um relatório com os estados carregando, vazio e erro.
 * 404 só vira "vazio" quando o corpo é um ApiResponse com `errorMessage` (ex.: "Nenhuma despesa
 * encontrada no período."): é assim que o backend responde a período sem dados. 404 sem esse corpo
 * significa rota inexistente (ex.: backend desatualizado, sem o endpoint) e vira erro — tratá-lo como
 * "sem dados" esconderia o problema.
 * `chave` resume os parâmetros da chamada; a busca roda de novo sempre que ela muda.
 */
export function useRelatorio<T>(
  buscar: () => Promise<AxiosResponse<ApiResponse<T>>>,
  chave: string,
  { habilitado = true, vazio }: Opcoes<T> = {},
): EstadoRelatorio<T> {
  const [estado, setEstado] = useState<EstadoRelatorio<T>>({ status: habilitado ? 'carregando' : 'ocioso' })

  // Refs para usar a versão mais recente das funções sem refazer a busca a cada render
  const buscarRef = useRef(buscar)
  const vazioRef = useRef(vazio)
  buscarRef.current = buscar
  vazioRef.current = vazio

  useEffect(() => {
    if (!habilitado) {
      setEstado({ status: 'ocioso' })
      return
    }

    // Descarta a resposta de uma busca antiga que chegue depois da atual
    let ativo = true
    setEstado({ status: 'carregando' })

    buscarRef.current()
      .then(({ data }) => {
        if (!ativo) return
        const dados = data.dados
        const semDados =
          dados == null ||
          (Array.isArray(dados) && dados.length === 0) ||
          (vazioRef.current ? vazioRef.current(dados) : false)
        setEstado(semDados ? { status: 'vazio', mensagem: null } : { status: 'sucesso', dados })
      })
      .catch((err: unknown) => {
        if (!ativo) return
        const { response } = err as RespostaErro
        const mensagem = mensagemApiResponse(response?.data)
        if (response?.status === 404) {
          setEstado(mensagem ? { status: 'vazio', mensagem } : { status: 'erro', mensagem: MENSAGEM_ROTA_INEXISTENTE })
          return
        }
        setEstado({ status: 'erro', mensagem: mensagem ?? 'Não foi possível carregar o relatório.' })
      })

    return () => { ativo = false }
  }, [chave, habilitado])

  return estado
}
