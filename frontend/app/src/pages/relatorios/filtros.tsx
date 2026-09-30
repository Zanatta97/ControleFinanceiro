import type { ReactNode } from 'react'
import { Icon } from '@iconify/react'
import Card from '../../components/ui/Card'
import Alert from '../../components/ui/Alert'
import { listarDoAmbiente as listarCategorias } from '../../api/categoria'
import { useRelatorio } from '../../hooks/useRelatorio'
import type { CategoriaResponse } from '../../types/api'
import {
  erroPeriodo,
  somarMeses,
  type FiltroPeriodoValor,
  type ModoFiltro,
  type SelecaoCategoriasProps,
} from './utils'

const campo =
  'rounded-lg border border-fin-border bg-fin-surface px-3 py-1.5 text-sm text-fin-text-primary focus:border-fin-brand focus:shadow-fin-focus focus:outline-none'
const botaoSeta =
  'rounded-lg border border-fin-border px-3 py-1.5 text-sm text-fin-text-secondary transition hover:bg-fin-surface-2'

export function BarraFiltros({ children }: { children: ReactNode }) {
  return <Card className="flex flex-wrap items-end gap-4 px-5 py-4">{children}</Card>
}

export function RotuloFiltro({ children }: { children: ReactNode }) {
  return <span className="mb-1.5 block text-[11px] font-semibold uppercase tracking-[0.05em] text-fin-text-muted">{children}</span>
}

// ── Competência ─────────────────────────────────────────────────────────

interface SeletorCompetenciaProps {
  mes: number
  ano: number
  onChange: (mes: number, ano: number) => void
  rotulo?: string
}

export function SeletorCompetencia({ mes, ano, onChange, rotulo = 'Competência' }: SeletorCompetenciaProps) {
  function mover(delta: number) {
    const p = somarMeses(mes, ano, delta)
    onChange(p.mes, p.ano)
  }
  return (
    <div>
      <RotuloFiltro>{rotulo}</RotuloFiltro>
      <div className="flex items-center gap-2">
        <button type="button" aria-label="Mês anterior" onClick={() => mover(-1)} className={botaoSeta}>‹</button>
        <input
          type="month"
          aria-label={rotulo}
          value={`${ano}-${String(mes).padStart(2, '0')}`}
          onChange={(e) => {
            if (!e.target.value) return
            const [a, m] = e.target.value.split('-').map(Number)
            onChange(m, a)
          }}
          className={campo}
        />
        <button type="button" aria-label="Próximo mês" onClick={() => mover(1)} className={botaoSeta}>›</button>
      </div>
    </div>
  )
}

// ── Ano ─────────────────────────────────────────────────────────────────

export function SeletorAno({ ano, onChange }: { ano: number; onChange: (ano: number) => void }) {
  return (
    <div>
      <RotuloFiltro>Ano</RotuloFiltro>
      <div className="flex items-center gap-2">
        <button type="button" aria-label="Ano anterior" onClick={() => onChange(ano - 1)} className={botaoSeta}>‹</button>
        <span className="min-w-[64px] rounded-lg border border-fin-border bg-fin-surface px-3 py-1.5 text-center font-fin-mono text-sm text-fin-text-primary">
          {ano}
        </span>
        <button type="button" aria-label="Próximo ano" onClick={() => onChange(ano + 1)} className={botaoSeta}>›</button>
      </div>
    </div>
  )
}

// ── Competência OU período ──────────────────────────────────────────────

interface FiltroPeriodoProps {
  valor: FiltroPeriodoValor
  onChange: (v: FiltroPeriodoValor) => void
}

const modos: { valor: ModoFiltro; rotulo: string }[] = [
  { valor: 'competencia', rotulo: 'Competência' },
  { valor: 'periodo', rotulo: 'Período' },
]

// Os dois modos são exclusivos: só os campos do modo ativo aparecem e vão para a API
export function FiltroPeriodo({ valor, onChange }: FiltroPeriodoProps) {
  const erro = erroPeriodo(valor)
  return (
    <>
      <div>
        <RotuloFiltro>Filtrar por</RotuloFiltro>
        <div role="radiogroup" aria-label="Filtrar por" className="inline-flex rounded-[9px] border border-fin-border bg-fin-surface-2 p-0.5">
          {modos.map((m) => {
            const ativo = valor.modo === m.valor
            return (
              <button
                key={m.valor}
                type="button"
                role="radio"
                aria-checked={ativo}
                onClick={() => onChange({ ...valor, modo: m.valor })}
                className={`rounded-[7px] px-3 py-1 text-[13px] font-semibold transition ${
                  ativo ? 'bg-fin-surface text-fin-brand shadow-sm' : 'text-fin-text-secondary hover:text-fin-text-primary'
                }`}
              >
                {m.rotulo}
              </button>
            )
          })}
        </div>
      </div>

      {valor.modo === 'competencia' ? (
        <SeletorCompetencia mes={valor.mes} ano={valor.ano} onChange={(mes, ano) => onChange({ ...valor, mes, ano })} />
      ) : (
        <div>
          <RotuloFiltro>Período (data da transação)</RotuloFiltro>
          <div className="flex flex-wrap items-center gap-2">
            <input
              type="date"
              aria-label="Data inicial"
              value={valor.dataInicio}
              onChange={(e) => onChange({ ...valor, dataInicio: e.target.value })}
              className={campo}
            />
            <span className="text-sm text-fin-text-muted">até</span>
            <input
              type="date"
              aria-label="Data final"
              value={valor.dataFim}
              onChange={(e) => onChange({ ...valor, dataFim: e.target.value })}
              className={campo}
            />
          </div>
        </div>
      )}

      {erro && <p className="basis-full text-xs text-fin-negative">{erro}</p>}
      {valor.modo === 'periodo' && !erro && (
        <p className="basis-full text-xs text-fin-text-muted">
          No filtro por período, uma compra parcelada conta inteira na data da compra.
        </p>
      )}
    </>
  )
}

// ── Seleção múltipla de categorias ──────────────────────────────────────

interface SeletorCategoriasProps {
  titulo: string
  descricao?: string
  categorias: CategoriaResponse[]
  selecionadas: string[]
  /** Categorias escolhidas na outra lista: o backend recusa a mesma categoria nas duas. */
  bloqueadas?: string[]
  motivoBloqueio?: string
  onChange: (ids: string[]) => void
}

export function SeletorCategorias({
  titulo,
  descricao,
  categorias,
  selecionadas,
  bloqueadas = [],
  motivoBloqueio = 'Já selecionada na outra lista',
  onChange,
}: SeletorCategoriasProps) {
  function alternar(id: string) {
    onChange(selecionadas.includes(id) ? selecionadas.filter((x) => x !== id) : [...selecionadas, id])
  }

  return (
    <div className="min-w-[260px] flex-1">
      <div className="mb-1.5 flex items-center justify-between gap-2">
        <RotuloFiltro>
          {titulo} {selecionadas.length > 0 && <span className="text-fin-brand">({selecionadas.length})</span>}
        </RotuloFiltro>
        {selecionadas.length > 0 && (
          <button type="button" onClick={() => onChange([])} className="mb-1.5 text-[11.5px] font-semibold text-fin-brand hover:underline">
            Limpar
          </button>
        )}
      </div>
      {descricao && <p className="-mt-1 mb-2 text-xs text-fin-text-muted">{descricao}</p>}
      {categorias.length === 0 ? (
        <p className="text-xs text-fin-text-muted">Nenhuma categoria cadastrada neste ambiente.</p>
      ) : (
        <div className="flex flex-wrap gap-1.5">
          {categorias.map((c) => {
            const ativa = selecionadas.includes(c.id)
            const bloqueada = !ativa && bloqueadas.includes(c.id)
            return (
              <button
                key={c.id}
                type="button"
                aria-pressed={ativa}
                disabled={bloqueada}
                title={bloqueada ? motivoBloqueio : undefined}
                onClick={() => alternar(c.id)}
                className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[12.5px] font-medium transition disabled:cursor-not-allowed disabled:opacity-40 ${
                  ativa
                    ? 'border-fin-brand bg-fin-brand-soft text-fin-brand'
                    : 'border-fin-border bg-fin-surface text-fin-text-secondary hover:bg-fin-surface-2'
                }`}
              >
                {ativa ? (
                  <Icon icon="lucide:check" width={12} height={12} />
                ) : (
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: c.cor ?? 'var(--fin-text-muted)' }} />
                )}
                {c.nome}
              </button>
            )
          })}
        </div>
      )}
    </div>
  )
}

// ── Categorias de fixos e de recebimentos (duas listas exclusivas) ─────

interface CategoriasFixosRecebimentosProps extends SelecaoCategoriasProps {
  obrigatorio?: boolean
}

export function CategoriasFixosRecebimentos({ selecao, onSelecao, obrigatorio = false }: CategoriasFixosRecebimentosProps) {
  const estado = useRelatorio(() => listarCategorias(), 'categorias')

  if (estado.status === 'carregando' || estado.status === 'ocioso') {
    return <Card className="px-5 py-4 text-sm text-fin-text-muted">Carregando categorias...</Card>
  }
  if (estado.status === 'erro') return <Alert type="error" message={estado.mensagem} />

  const categorias = estado.status === 'sucesso' ? estado.dados : []
  const sufixo = obrigatorio ? 'Selecione ao menos uma.' : 'Opcional.'

  return (
    <Card className="flex flex-wrap gap-6 px-5 py-4">
      <SeletorCategorias
        titulo="Categorias de gasto fixo"
        descricao={`Aluguel, contas da casa, assinaturas... ${sufixo}`}
        categorias={categorias}
        selecionadas={selecao.fixas}
        bloqueadas={selecao.recebimento}
        motivoBloqueio="Já selecionada como recebimento"
        onChange={(fixas) => onSelecao({ ...selecao, fixas })}
      />
      <SeletorCategorias
        titulo="Categorias de recebimento"
        descricao={`Salário, pró-labore, aluguel recebido... ${sufixo}`}
        categorias={categorias}
        selecionadas={selecao.recebimento}
        bloqueadas={selecao.fixas}
        motivoBloqueio="Já selecionada como gasto fixo"
        onChange={(recebimento) => onSelecao({ ...selecao, recebimento })}
      />
    </Card>
  )
}
