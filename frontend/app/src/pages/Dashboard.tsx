import { useEffect, useState } from 'react'
import { resumoMensal, gastoPorCategoria, statusOrcamentos } from '../api/relatorio'
import type { ResumoFinanceiroResponse, GastoPorCategoriaResponse, OrcamentoStatusResponse } from '../types/api'
import { formatCurrency } from '../utils/format'
import Card from '../components/ui/Card'

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

export default function Dashboard() {
  const init = currentYM()
  const [mes, setMes] = useState(init.mes)
  const [ano, setAno] = useState(init.ano)

  const [resumo, setResumo] = useState<ResumoFinanceiroResponse | null>(null)
  const [categorias, setCategorias] = useState<GastoPorCategoriaResponse[]>([])
  const [orcamentos, setOrcamentos] = useState<OrcamentoStatusResponse[]>([])
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    load(mes, ano)
  }, [mes, ano])

  async function load(m: number, a: number) {
    setLoading(true)
    const [r, c, o] = await Promise.allSettled([
      resumoMensal(m, a),
      gastoPorCategoria(m, a),
      statusOrcamentos(),
    ])
    if (r.status === 'fulfilled') setResumo(r.value.data.dados)
    if (c.status === 'fulfilled') setCategorias(c.value.data.dados ?? [])
    if (o.status === 'fulfilled') setOrcamentos(o.value.data.dados ?? [])
    setLoading(false)
  }

  function handleMes(val: string) {
    const { mes: m, ano: a } = inputToYM(val)
    setMes(m)
    setAno(a)
  }

  return (
    <div className="space-y-6">
      {/* Cabeçalho com seletor de mês */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Dashboard</h1>
          <p className="text-sm text-gray-500 capitalize">{labelMes(mes, ano)}</p>
        </div>
        <div className="flex items-center gap-2">
          <button
            onClick={() => {
              const d = new Date(ano, mes - 2, 1)
              setMes(d.getMonth() + 1)
              setAno(d.getFullYear())
            }}
            className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 transition"
          >
            ‹
          </button>
          <input
            type="month"
            value={ymToInput(mes, ano)}
            onChange={(e) => handleMes(e.target.value)}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          />
          <button
            onClick={() => {
              const d = new Date(ano, mes, 1)
              setMes(d.getMonth() + 1)
              setAno(d.getFullYear())
            }}
            className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm text-gray-600 hover:bg-gray-50 transition"
          >
            ›
          </button>
        </div>
      </div>

      {loading ? (
        <div className="flex h-64 items-center justify-center text-gray-400">Carregando...</div>
      ) : (
        <>
          {/* Resumo */}
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Card className="p-5">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500">Receitas</p>
              <p className="mt-1 text-2xl font-bold text-green-600">{formatCurrency(resumo?.totalReceitas ?? 0)}</p>
            </Card>
            <Card className="p-5">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500">Despesas</p>
              <p className="mt-1 text-2xl font-bold text-red-500">{formatCurrency(resumo?.totalDespesas ?? 0)}</p>
            </Card>
            <Card className="p-5">
              <p className="text-xs font-medium uppercase tracking-wide text-gray-500">Saldo do Mês</p>
              <p className={`mt-1 text-2xl font-bold ${(resumo?.saldo ?? 0) >= 0 ? 'text-gray-900' : 'text-red-600'}`}>
                {formatCurrency(resumo?.saldo ?? 0)}
              </p>
            </Card>
          </div>

          <div className="grid grid-cols-1 gap-6 lg:grid-cols-2">
            {/* Gastos por Categoria */}
            <Card>
              <div className="border-b px-5 py-4">
                <h2 className="font-semibold text-gray-800">Gastos por Categoria</h2>
              </div>
              <div className="p-5 space-y-3">
                {categorias.length === 0
                  ? <p className="text-sm text-gray-400 text-center py-4">Sem despesas neste mês.</p>
                  : categorias.map((c) => (
                    <div key={c.categoriaId}>
                      <div className="flex items-center justify-between mb-1">
                        <div className="flex items-center gap-2">
                          <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: c.cor ?? '#9ca3af' }} />
                          <span className="text-sm text-gray-700">{c.nomeCategoria}</span>
                        </div>
                        <span className="text-sm font-medium text-gray-800">{formatCurrency(c.totalGasto)}</span>
                      </div>
                      <div className="h-1.5 rounded-full bg-gray-100">
                        <div
                          className="h-1.5 rounded-full"
                          style={{ width: `${Math.min(c.percentual, 100)}%`, backgroundColor: c.cor ?? '#22c55e' }}
                        />
                      </div>
                    </div>
                  ))}
              </div>
            </Card>

            {/* Status dos Orçamentos */}
            <Card>
              <div className="border-b px-5 py-4">
                <h2 className="font-semibold text-gray-800">Orçamentos Ativos</h2>
              </div>
              <div className="p-5 space-y-4">
                {orcamentos.length === 0
                  ? <p className="text-sm text-gray-400 text-center py-4">Nenhum orçamento ativo.</p>
                  : orcamentos.slice(0, 5).map((o) => {
                    const pct = Math.min(o.percentual, 100)
                    const color = pct >= 90 ? '#ef4444' : pct >= 70 ? '#f97316' : '#22c55e'
                    return (
                      <div key={o.id}>
                        <div className="flex items-center justify-between mb-1">
                          <span className="text-sm font-medium text-gray-700">{o.nomeOrcamento}</span>
                          <span className="text-xs text-gray-500">{formatCurrency(o.valorGasto)} / {formatCurrency(o.valorLimite)}</span>
                        </div>
                        <div className="h-2 rounded-full bg-gray-100">
                          <div className="h-2 rounded-full transition-all" style={{ width: `${pct}%`, backgroundColor: color }} />
                        </div>
                        <p className="mt-0.5 text-xs text-gray-400">{o.nomeCategoria} · {pct.toFixed(1)}% usado</p>
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
