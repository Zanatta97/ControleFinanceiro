import type { FiltroCompetenciaOuPeriodo } from '../../types/api'
import { TipoConta } from '../../types/api'

export type ModoFiltro = 'competencia' | 'periodo'

// Estado do filtro "competência OU período". Os dois modos guardam seus valores,
// mas só o do modo ativo vai para a API.
export interface FiltroPeriodoValor {
  modo: ModoFiltro
  mes: number
  ano: number
  dataInicio: string  // YYYY-MM-DD
  dataFim: string     // YYYY-MM-DD
}

// Categorias de gasto fixo e de recebimento. Fica na página de relatórios para
// valer nas duas subabas que usam (projeção do próximo mês e fixos × recebimentos).
export interface SelecaoCategorias {
  fixas: string[]
  recebimento: string[]
}

export interface SelecaoCategoriasProps {
  selecao: SelecaoCategorias
  onSelecao: (s: SelecaoCategorias) => void
}

export const corNeutra = 'var(--fin-text-muted)'

// Data local em YYYY-MM-DD (toISOString usaria UTC e poderia virar o dia)
export function toISODate(d: Date): string {
  const mm = String(d.getMonth() + 1).padStart(2, '0')
  const dd = String(d.getDate()).padStart(2, '0')
  return `${d.getFullYear()}-${mm}-${dd}`
}

export function mesAtual() {
  const d = new Date()
  return { mes: d.getMonth() + 1, ano: d.getFullYear() }
}

export function proximoMes() {
  const d = new Date()
  const p = new Date(d.getFullYear(), d.getMonth() + 1, 1)
  return { mes: p.getMonth() + 1, ano: p.getFullYear() }
}

export function somarMeses(mes: number, ano: number, delta: number) {
  const d = new Date(ano, mes - 1 + delta, 1)
  return { mes: d.getMonth() + 1, ano: d.getFullYear() }
}

// "setembro de 2026"
export function labelCompetencia(mes: number, ano: number): string {
  return new Date(ano, mes - 1, 1).toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
}

// "set/26"
export function labelMesCurto(mes: number, ano: number): string {
  const nome = new Date(ano, mes - 1, 1).toLocaleDateString('pt-BR', { month: 'short' }).replace('.', '')
  return `${nome}/${String(ano).slice(2)}`
}

export function filtroInicial(): FiltroPeriodoValor {
  const hoje = new Date()
  return {
    modo: 'competencia',
    mes: hoje.getMonth() + 1,
    ano: hoje.getFullYear(),
    dataInicio: toISODate(new Date(hoje.getFullYear(), hoje.getMonth(), 1)),
    dataFim: toISODate(hoje),
  }
}

// Mensagem do período inválido, ou null se o período pode ir para a API
export function erroPeriodo(f: FiltroPeriodoValor): string | null {
  if (f.modo !== 'periodo') return null
  if (!f.dataInicio || !f.dataFim) return 'Informe a data inicial e a final.'
  if (f.dataInicio > f.dataFim) return 'A data inicial deve ser anterior ou igual à final.'
  return null
}

// Parâmetros da API: só os do modo ativo, nunca os dois
export function filtroParaParams(f: FiltroPeriodoValor): FiltroCompetenciaOuPeriodo {
  return f.modo === 'competencia'
    ? { mes: f.mes, ano: f.ano }
    : { dataInicio: f.dataInicio, dataFim: f.dataFim }
}

export function chaveFiltro(f: FiltroPeriodoValor): string {
  return f.modo === 'competencia' ? `c:${f.mes}-${f.ano}` : `p:${f.dataInicio}_${f.dataFim}`
}

export function descricaoFiltro(f: FiltroPeriodoValor): string {
  if (f.modo === 'competencia') return `Competência de ${labelCompetencia(f.mes, f.ano)}`
  const fmt = (iso: string) => new Date(`${iso}T00:00:00`).toLocaleDateString('pt-BR')
  return `De ${fmt(f.dataInicio)} a ${fmt(f.dataFim)}`
}

export const nomeTipoConta: Record<TipoConta, string> = {
  [TipoConta.Corrente]: 'Corrente',
  [TipoConta.Poupanca]: 'Poupança',
  [TipoConta.Investimento]: 'Investimento',
  [TipoConta.CartaoCredito]: 'Cartão de crédito',
}

// Data sem hora vinda da API ("2026-09-01") exibida no fuso local, sem voltar um dia
export function formatDataLocal(iso: string): string {
  const soData = iso.slice(0, 10)
  return new Date(`${soData}T00:00:00`).toLocaleDateString('pt-BR')
}
