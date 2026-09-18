import { useEffect, useState } from 'react'
import { Icon } from '@iconify/react'
import { resumoMensal, gastoPorCategoria, statusOrcamentos, evolucaoMensal } from '../api/relatorio'
import type {
  ResumoFinanceiroResponse,
  GastoPorCategoriaResponse,
  OrcamentoStatusResponse,
  EvolucaoMensalResponse,
} from '../types/api'
import { formatCurrency } from '../utils/format'
import Card from '../components/ui/Card'
import Donut from '../components/ui/Donut'
import SaldoMensalContas from '../components/SaldoMensalContas'

function currentYM() {
  const d = new Date()
  return { mes: d.getMonth() + 1, ano: d.getFullYear() }
}

function ymToInput(mes: number, ano: number) {
  return `${ano}-${String(mes).padStart(2, '0')}`
}

function inputToYM(val: string) {
  const [ano, mes] = val.split('-').map(Number)
  return { mes, ano }
}

function labelMes(mes: number, ano: number) {
  return new Date(ano, mes - 1, 1)
    .toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
}

function nomeMesAnterior(mes: number) {
  return new Date(2000, mes - 2, 1).toLocaleDateString('pt-BR', { month: 'short' }).replace('.', '')
}

// Calcula a variação percentual entre o valor atual e o anterior.
function delta(atual: number, anterior: number | undefined): number | null {
  if (anterior == null || anterior === 0) return null
  return ((atual - anterior) / Math.abs(anterior)) * 100
}

interface DeltaChipProps {
  pct: number | null
  lowerIsBetter?: boolean
  mesAnterior: string
  inverse?: boolean // card de saldo destacado (texto branco)
}

function DeltaChip({ pct, lowerIsBetter = false, mesAnterior, inverse = false }: DeltaChipProps) {
  if (pct == null) return null
  const subiu = pct > 0
  const bom = lowerIsBetter ? !subiu : subiu
  const seta = subiu ? '▲' : '▼'
  const sinal = pct > 0 ? '+' : ''

  if (inverse) {
    return (
      <span className="mt-2 inline-flex items-center gap-1 rounded-md bg-white/[0.18] px-1.5 py-0.5 text-[11.5px] font-semibold text-white">
        {seta} {sinal}{pct.toFixed(1).replace('.', ',')}% <span className="font-normal text-white/70">vs. {mesAnterior}</span>
      </span>
    )
  }
  const cls = bom
    ? 'bg-fin-positive-soft text-fin-positive'
    : 'bg-fin-negative-soft text-fin-negative'
  return (
    <span className={`mt-2 inline-flex items-center gap-1 rounded-md px-1.5 py-0.5 text-[11.5px] font-semibold ${cls}`}>
      {seta} {sinal}{pct.toFixed(1).replace('.', ',')}% <span className="font-normal text-fin-text-muted">vs. {mesAnterior}</span>
    </span>
  )
}

export default function Dashboard() {
  const init = currentYM()
  const [mes, setMes] = useState(init.mes)
  const [ano, setAno] = useState(init.ano)

  const [resumo, setResumo] = useState<ResumoFinanceiroResponse | null>(null)
  const [categorias, setCategorias] = useState<GastoPorCategoriaResponse[]>([])
  const [orcamentos, setOrcamentos] = useState<OrcamentoStatusResponse[]>([])
  const [evolucao, setEvolucao] = useState<EvolucaoMensalResponse | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    load(mes, ano)
  }, [mes, ano])

  async function load(m: number, a: number) {
    setLoading(true)
    const [r, c, o, e] = await Promise.allSettled([
      resumoMensal(m, a),
      gastoPorCategoria(m, a),
      statusOrcamentos(),
      evolucaoMensal(a),
    ])
    setResumo(r.status === 'fulfilled' ? r.value.data.dados : null)
    setCategorias(c.status === 'fulfilled' ? (c.value.data.dados ?? []) : [])
    setOrcamentos(o.status === 'fulfilled' ? (o.value.data.dados ?? []) : [])
    setEvolucao(e.status === 'fulfilled' ? e.value.data.dados : null)
    setLoading(false)
  }

  function handleMes(val: string) {
    const { mes: m, ano: a } = inputToYM(val)
    setMes(m)
    setAno(a)
  }

  // Mês anterior (dentro do mesmo ano carregado na evolução).
  const mesAnt = evolucao?.meses.find((x) => x.mes === mes - 1)
  const mesAntLabel = nomeMesAnterior(mes)
  const deltaReceitas = delta(resumo?.totalReceitas ?? 0, mesAnt?.totalReceitas)
  const deltaDespesas = delta(resumo?.totalDespesas ?? 0, mesAnt?.totalDespesas)
  const deltaSaldo = delta(resumo?.saldo ?? 0, mesAnt?.saldo)

  // Quebra das despesas: cartão de crédito x demais contas.
  const totalDespesas = resumo?.totalDespesas ?? 0
  const despesasCartao = resumo?.despesasCartao ?? 0
  const despesasOutras = resumo?.despesasOutras ?? 0
  const pctCartao = totalDespesas > 0 ? (despesasCartao / totalDespesas) * 100 : 0

  const totalGasto = categorias.reduce((acc, c) => acc + c.totalGasto, 0)
  const orcamentosAtivos = orcamentos
    .filter((o) => new Date(o.dataLimite) >= new Date(ano, mes - 1, 1))

  return (
    <div className="space-y-5">
      {/* Cabeçalho com seletor de mês */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-[22px] font-bold text-fin-text-primary">Dashboard</h1>
          <p className="text-sm capitalize text-fin-text-secondary">{labelMes(mes, ano)}</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => {
              const d = new Date(ano, mes - 2, 1)
              setMes(d.getMonth() + 1)
              setAno(d.getFullYear())
            }}
            className="rounded-lg border border-fin-border px-3 py-1.5 text-sm text-fin-text-secondary transition hover:bg-fin-surface-2"
          >
            ‹
          </button>
          <input
            type="month"
            value={ymToInput(mes, ano)}
            onChange={(e) => handleMes(e.target.value)}
            className="rounded-lg border border-fin-border bg-fin-surface px-3 py-1.5 text-sm text-fin-text-primary focus:border-fin-brand focus:shadow-fin-focus focus:outline-none"
          />
          <button
            onClick={() => {
              const d = new Date(ano, mes, 1)
              setMes(d.getMonth() + 1)
              setAno(d.getFullYear())
            }}
            className="rounded-lg border border-fin-border px-3 py-1.5 text-sm text-fin-text-secondary transition hover:bg-fin-surface-2"
          >
            ›
          </button>
        </div>
      </div>

      {loading ? (
        <div className="flex h-48 items-center justify-center text-fin-text-muted">Carregando...</div>
      ) : (
        <>
          {/* Cards de resumo */}
          <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
            {/* Receitas */}
            <Card className="p-[18px]">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-semibold uppercase tracking-[0.05em] text-fin-text-muted">Receitas</span>
                <div className="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-fin-positive-soft text-fin-positive">
                  <Icon icon="lucide:arrow-down-circle" width={16} height={16} />
                </div>
              </div>
              <p className="mt-3 font-fin-mono text-[22px] font-medium text-fin-text-primary">{formatCurrency(resumo?.totalReceitas ?? 0)}</p>
              <DeltaChip pct={deltaReceitas} mesAnterior={mesAntLabel} />
            </Card>

            {/* Despesas */}
            <Card className="p-[18px]">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-semibold uppercase tracking-[0.05em] text-fin-text-muted">Despesas</span>
                <div className="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-fin-negative-soft text-fin-negative">
                  <Icon icon="lucide:arrow-up-circle" width={16} height={16} />
                </div>
              </div>
              <p className="mt-3 font-fin-mono text-[22px] font-medium text-fin-text-primary">{formatCurrency(resumo?.totalDespesas ?? 0)}</p>
              <DeltaChip pct={deltaDespesas} lowerIsBetter mesAnterior={mesAntLabel} />

              {/* Quebra: cartão de crédito x contas */}
              <div className="mt-3 border-t border-fin-border pt-3">
                <div className="flex h-[5px] overflow-hidden rounded-[3px] bg-fin-surface-2">
                  {totalDespesas > 0 && (
                    <>
                      <div className="h-full bg-fin-invest" style={{ width: `${pctCartao}%` }} />
                      <div className="h-full bg-fin-brand" style={{ width: `${100 - pctCartao}%` }} />
                    </>
                  )}
                </div>
                <div className="mt-2.5 flex flex-col gap-1.5">
                  <div className="flex items-center gap-2">
                    <span className="h-2 w-2 flex-none rounded-[2px] bg-fin-invest" />
                    <span className="flex-1 text-[12px] text-fin-text-secondary">Cartão de crédito</span>
                    <span className="font-fin-mono text-[11.5px] text-fin-text-primary">{formatCurrency(despesasCartao)}</span>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="h-2 w-2 flex-none rounded-[2px] bg-fin-brand" />
                    <span className="flex-1 text-[12px] text-fin-text-secondary">Contas</span>
                    <span className="font-fin-mono text-[11.5px] text-fin-text-primary">{formatCurrency(despesasOutras)}</span>
                  </div>
                </div>
              </div>
            </Card>

            {/* Saldo do mês (destacado) */}
            <div className="rounded-[13px] border border-fin-brand bg-fin-brand p-[18px]">
              <div className="flex items-center justify-between">
                <span className="text-[11px] font-semibold uppercase tracking-[0.05em] text-white/75">Saldo do mês</span>
                <div className="flex h-[30px] w-[30px] items-center justify-center rounded-lg bg-white/[0.16] text-white">
                  <Icon icon="lucide:credit-card" width={16} height={16} />
                </div>
              </div>
              <p className="mt-3 font-fin-mono text-[22px] font-medium text-white">{formatCurrency(resumo?.saldo ?? 0)}</p>
              <DeltaChip pct={deltaSaldo} mesAnterior={mesAntLabel} inverse />
            </div>
          </div>

          {/* Saldo mensal por conta */}
          <SaldoMensalContas mes={mes} ano={ano} />

          <div className="grid grid-cols-1 gap-3.5 lg:grid-cols-2">
            {/* Gastos por categoria (donut) */}
            <Card>
              <div className="border-b border-fin-border px-5 py-4">
                <h2 className="text-[15px] font-semibold text-fin-text-primary">Gastos por categoria</h2>
              </div>
              {categorias.length === 0 ? (
                <p className="py-8 text-center text-sm text-fin-text-muted">Sem despesas neste mês.</p>
              ) : (
                <div className="flex items-center gap-[18px] px-5 py-[18px]">
                  <Donut segments={categorias.map((c) => ({ value: c.percentual, cor: c.cor ?? 'var(--fin-text-muted)' }))}>
                    <span className="text-[9.5px] font-semibold uppercase text-fin-text-muted">Total</span>
                    <span className="font-fin-mono text-[14px] text-fin-text-primary">{formatCurrency(totalGasto)}</span>
                  </Donut>
                  <div className="flex flex-1 flex-col gap-2.5">
                    {categorias.map((c) => (
                      <div key={c.categoriaId} className="flex items-center gap-2.5">
                        <span className="h-2.5 w-2.5 flex-none rounded-[3px]" style={{ backgroundColor: c.cor ?? 'var(--fin-text-muted)' }} />
                        <span className="flex-1 truncate text-[12.5px] text-fin-text-secondary">{c.nomeCategoria}</span>
                        <span className="text-[11px] text-fin-text-muted">{c.percentual.toFixed(1).replace('.', ',')}%</span>
                        <span className="min-w-[70px] text-right font-fin-mono text-[11.5px] text-fin-text-primary">{formatCurrency(c.totalGasto)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </Card>

            {/* Orçamentos ativos */}
            <Card>
              <div className="border-b border-fin-border px-5 py-4">
                <h2 className="text-[15px] font-semibold text-fin-text-primary">Orçamentos ativos</h2>
              </div>
              <div className="flex flex-col gap-4 px-5 py-[18px]">
                {orcamentosAtivos.length === 0 && (
                  <p className="py-4 text-center text-sm text-fin-text-muted">Nenhum orçamento ativo.</p>
                )}
                {orcamentosAtivos.slice(0, 5).map((o) => {
                  const pct = Math.min(o.percentual, 100)
                  const color = pct >= 90 ? 'var(--fin-negative)' : pct >= 70 ? 'var(--fin-warning)' : 'var(--fin-positive)'
                  return (
                    <div key={o.id}>
                      <div className="mb-1.5 flex items-center justify-between">
                        <span className="text-[13px] font-semibold text-fin-text-primary">{o.nomeOrcamento}</span>
                        <span className="font-fin-mono text-[11.5px] text-fin-text-muted">{formatCurrency(o.valorGasto)} / {formatCurrency(o.valorLimite)}</span>
                      </div>
                      <div className="h-[7px] overflow-hidden rounded-[5px] bg-fin-surface-2">
                        <div className="h-[7px] rounded-[5px] transition-all" style={{ width: `${pct}%`, backgroundColor: color }} />
                      </div>
                      <div className="mt-1.5 flex justify-between">
                        <span className="text-[11px] text-fin-text-muted">{o.nomeCategoria}</span>
                        <span className="text-[11px] font-semibold" style={{ color }}>{pct.toFixed(1).replace('.', ',')}% usado</span>
                      </div>
                    </div>
                  )
                })}
              </div>
            </Card>
          </div>
        </>
      )}
    </div>
  )
}
