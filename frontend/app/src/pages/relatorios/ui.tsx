import type { ReactNode } from 'react'
import { Icon } from '@iconify/react'
import Card from '../../components/ui/Card'
import Alert from '../../components/ui/Alert'
import type { EstadoRelatorio } from '../../hooks/useRelatorio'
import { formatVariacao } from '../../utils/format'

// ── Estados ─────────────────────────────────────────────────────────────

interface EstadoViewProps<T> {
  estado: EstadoRelatorio<T>
  /** Texto do estado vazio quando a API não traz mensagem própria. */
  vazio?: string
  /** O que mostrar enquanto a busca está desabilitada (filtro incompleto). */
  ocioso?: ReactNode
  children: (dados: T) => ReactNode
}

export function EstadoView<T>({ estado, vazio = 'Nenhum dado encontrado no período.', ocioso, children }: EstadoViewProps<T>) {
  switch (estado.status) {
    case 'ocioso':
      return <>{ocioso ?? null}</>
    case 'carregando':
      return (
        <Card className="flex h-48 items-center justify-center gap-2 text-sm text-fin-text-muted">
          <Icon icon="lucide:loader-2" width={16} height={16} className="animate-spin" />
          Carregando...
        </Card>
      )
    case 'vazio':
      return <EstadoVazio mensagem={estado.mensagem ?? vazio} />
    case 'erro':
      return <Alert type="error" message={estado.mensagem} />
    case 'sucesso':
      return <>{children(estado.dados)}</>
  }
}

export function EstadoVazio({ mensagem, icone = 'lucide:inbox' }: { mensagem: string; icone?: string }) {
  return (
    <Card className="flex flex-col items-center justify-center gap-2 px-5 py-12 text-center">
      <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-fin-surface-2 text-fin-text-muted">
        <Icon icon={icone} width={20} height={20} />
      </div>
      <p className="text-sm text-fin-text-secondary">{mensagem}</p>
    </Card>
  )
}

// ── Blocos ──────────────────────────────────────────────────────────────

interface SecaoProps {
  titulo: string
  descricao?: string
  acao?: ReactNode
  children: ReactNode
}

export function Secao({ titulo, descricao, acao, children }: SecaoProps) {
  return (
    <Card>
      <div className="flex flex-wrap items-center justify-between gap-3 border-b border-fin-border px-5 py-4">
        <div>
          <h2 className="text-[15px] font-semibold text-fin-text-primary">{titulo}</h2>
          {descricao && <p className="mt-0.5 text-xs text-fin-text-muted">{descricao}</p>}
        </div>
        {acao}
      </div>
      {children}
    </Card>
  )
}

type Tom = 'neutro' | 'positivo' | 'negativo' | 'marca' | 'invest' | 'alerta'

const tons: Record<Tom, string> = {
  neutro: 'bg-fin-surface-2 text-fin-text-secondary',
  positivo: 'bg-fin-positive-soft text-fin-positive',
  negativo: 'bg-fin-negative-soft text-fin-negative',
  marca: 'bg-fin-brand-soft text-fin-brand',
  invest: 'bg-fin-invest-soft text-fin-invest',
  alerta: 'bg-fin-warning-soft text-fin-warning',
}

interface KpiProps {
  titulo: string
  valor: string
  icone: string
  tom?: Tom
  detalhe?: ReactNode
  destaque?: boolean
}

// Cartão de indicador no mesmo formato dos cartões de resumo da Dashboard
export function Kpi({ titulo, valor, icone, tom = 'neutro', detalhe, destaque = false }: KpiProps) {
  if (destaque) {
    return (
      <div className="rounded-[13px] border border-fin-brand bg-fin-brand p-[18px]">
        <div className="flex items-center justify-between">
          <span className="text-[11px] font-semibold uppercase tracking-[0.05em] text-white/75">{titulo}</span>
          <div className="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-white/[0.16] text-white">
            <Icon icon={icone} width={16} height={16} />
          </div>
        </div>
        <p className="mt-3 font-fin-mono text-[22px] font-medium text-white">{valor}</p>
        {detalhe && <div className="mt-1.5 text-[12px] text-white/75">{detalhe}</div>}
      </div>
    )
  }
  return (
    <Card className="p-[18px]">
      <div className="flex items-center justify-between">
        <span className="text-[11px] font-semibold uppercase tracking-[0.05em] text-fin-text-muted">{titulo}</span>
        <div className={`flex h-[30px] w-[30px] items-center justify-center rounded-lg ${tons[tom]}`}>
          <Icon icon={icone} width={16} height={16} />
        </div>
      </div>
      <p className="mt-3 font-fin-mono text-[22px] font-medium text-fin-text-primary">{valor}</p>
      {detalhe && <div className="mt-1.5 text-[12px] text-fin-text-muted">{detalhe}</div>}
    </Card>
  )
}

// Chip de variação. Para despesa, subir é ruim (menorEhMelhor). Nulo = sem base de comparação.
export function VariacaoChip({ valor, menorEhMelhor = true }: { valor: number | null | undefined; menorEhMelhor?: boolean }) {
  if (valor == null || !Number.isFinite(valor)) {
    return (
      <span title="Sem base de comparação" className="inline-flex rounded-md bg-fin-surface-2 px-1.5 py-0.5 text-[11.5px] font-semibold text-fin-text-muted">
        —
      </span>
    )
  }
  if (valor === 0) {
    return (
      <span className="inline-flex rounded-md bg-fin-surface-2 px-1.5 py-0.5 text-[11.5px] font-semibold text-fin-text-secondary">
        {formatVariacao(valor)}
      </span>
    )
  }
  const subiu = valor > 0
  const bom = menorEhMelhor ? !subiu : subiu
  const cls = bom ? 'bg-fin-positive-soft text-fin-positive' : 'bg-fin-negative-soft text-fin-negative'
  return (
    <span className={`inline-flex items-center gap-1 whitespace-nowrap rounded-md px-1.5 py-0.5 text-[11.5px] font-semibold ${cls}`}>
      {subiu ? '▲' : '▼'} {formatVariacao(valor)}
    </span>
  )
}

export function CorCategoria({ cor }: { cor: string | null | undefined }) {
  return (
    <span
      className="inline-block h-2.5 w-2.5 flex-none rounded-[3px]"
      style={{ backgroundColor: cor ?? 'var(--fin-text-muted)' }}
    />
  )
}

// Barra horizontal de proporção (0–100)
export function BarraProporcao({ pct, cor = 'var(--fin-brand)' }: { pct: number; cor?: string }) {
  const largura = Number.isFinite(pct) ? Math.max(0, Math.min(pct, 100)) : 0
  return (
    <div className="h-[7px] overflow-hidden rounded-[5px] bg-fin-surface-2">
      <div className="h-[7px] rounded-[5px] transition-all" style={{ width: `${largura}%`, backgroundColor: cor }} />
    </div>
  )
}

// ── Gráfico de barras verticais (sem biblioteca de gráficos no projeto) ──

export interface SerieBarra {
  nome: string
  cor: string
}

export interface ItemBarra {
  rotulo: string
  valores: number[]  // um valor por série, na ordem de `series`
  dica?: string
}

interface GraficoBarrasProps {
  series: SerieBarra[]
  itens: ItemBarra[]
  altura?: number
  formatar: (v: number) => string
}

export function GraficoBarras({ series, itens, altura = 160, formatar }: GraficoBarrasProps) {
  const maximo = Math.max(0, ...itens.flatMap((i) => i.valores.map((v) => Math.abs(v))))

  return (
    <div className="px-5 py-[18px]">
      {series.length > 1 && (
        <div className="mb-3 flex flex-wrap gap-4">
          {series.map((s) => (
            <span key={s.nome} className="flex items-center gap-1.5 text-[12px] text-fin-text-secondary">
              <span className="h-2.5 w-2.5 rounded-[3px]" style={{ backgroundColor: s.cor }} />
              {s.nome}
            </span>
          ))}
        </div>
      )}
      <div className="overflow-x-auto">
        <div className="flex min-w-[520px] items-end gap-2" style={{ height: altura }}>
          {itens.map((item) => (
            <div
              key={item.rotulo}
              title={item.dica ?? series.map((s, i) => `${s.nome}: ${formatar(item.valores[i] ?? 0)}`).join('\n')}
              className="flex h-full flex-1 items-end justify-center gap-[3px]"
            >
              {item.valores.map((v, i) => (
                <div
                  key={series[i]?.nome ?? i}
                  className="w-full max-w-[18px] rounded-t-[4px]"
                  style={{
                    height: maximo > 0 ? `${Math.max((Math.abs(v) / maximo) * 100, v !== 0 ? 2 : 0)}%` : 0,
                    backgroundColor: series[i]?.cor ?? 'var(--fin-brand)',
                  }}
                />
              ))}
            </div>
          ))}
        </div>
        <div className="mt-2 flex min-w-[520px] gap-2 border-t border-fin-border pt-2">
          {itens.map((item) => (
            <span key={item.rotulo} className="flex-1 text-center text-[11px] capitalize text-fin-text-muted">
              {item.rotulo}
            </span>
          ))}
        </div>
      </div>
    </div>
  )
}

// Classes das tabelas, iguais às das outras telas
export const th = 'px-5 py-3'
export const td = 'px-5 py-3'
export const theadRow = 'border-b border-fin-border text-left text-xs font-medium uppercase tracking-wide text-fin-text-muted'
export const tbodyCls = 'divide-y divide-fin-border'
export const trCls = 'transition hover:bg-fin-highlight-row'
