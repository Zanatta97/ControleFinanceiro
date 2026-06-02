import { useEffect, useState, useCallback } from 'react'
import { getSaldoMensal, salvarSaldoInicial, calcularSaldoInicial, type SaldoMensalContaResponse } from '../api/saldoMensal'
import { formatCurrency } from '../utils/format'
import Card from './ui/Card'
import Button from './ui/Button'

interface Props {
  mes: number
  ano: number
}

export default function SaldoMensalContas({ mes, ano }: Props) {
  const [dados, setDados] = useState<SaldoMensalContaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [calculando, setCalculando] = useState(false)
  const [editando, setEditando] = useState<string | null>(null)
  const [valorEdit, setValorEdit] = useState('')

  const load = useCallback(async () => {
    setLoading(true)
    try {
      const { data } = await getSaldoMensal(mes, ano)
      setDados(data.dados ?? [])
    } catch {
      setDados([])
    } finally {
      setLoading(false)
    }
  }, [mes, ano])

  useEffect(() => { load() }, [load])

  async function handleCalcular() {
    setCalculando(true)
    try {
      const { data } = await calcularSaldoInicial(mes, ano)
      setDados(data.dados ?? [])
    } finally {
      setCalculando(false)
    }
  }

  function startEdit(contaId: string, saldoAtual: number) {
    setEditando(contaId)
    setValorEdit(String(saldoAtual))
  }

  async function confirmEdit(contaId: string) {
    const valor = parseFloat(valorEdit.replace(',', '.'))
    if (isNaN(valor)) { setEditando(null); return }
    try {
      await salvarSaldoInicial(contaId, mes, ano, valor)
      await load()
    } finally {
      setEditando(null)
    }
  }

  if (loading) return <div className="py-4 text-center text-sm text-fin-text-muted">Carregando saldos...</div>

  const temDados = dados.some((d) => d.saldoInicial !== 0 || d.totalEntradas !== 0 || d.totalSaidas !== 0)

  return (
    <Card>
      <div className="flex items-center justify-between border-b border-fin-border px-5 py-4">
        <div>
          <h2 className="font-semibold text-fin-text-primary">Saldo das Contas</h2>
          <p className="text-xs text-fin-text-muted mt-0.5">Clique no saldo inicial para editar manualmente.</p>
        </div>
        <Button size="sm" variant="secondary" onClick={handleCalcular} loading={calculando}>
          Calcular saldo inicial
        </Button>
      </div>

      {dados.length === 0 ? (
        <p className="px-5 py-6 text-center text-sm text-fin-text-muted">Nenhuma conta encontrada.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-fin-border text-left text-xs font-medium uppercase tracking-wide text-fin-text-muted">
                <th className="px-5 py-3">Conta</th>
                <th className="px-5 py-3 text-right">Saldo Inicial</th>
                <th className="px-5 py-3 text-right">Entradas</th>
                <th className="px-5 py-3 text-right">Saídas</th>
                <th className="px-5 py-3 text-right">Saldo Final</th>
                <th className="px-5 py-3 text-right">Diferença</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-fin-border">
              {dados.map((d) => (
                <tr key={d.contaId} className="hover:bg-fin-highlight-row transition">
                  <td className="px-5 py-3 font-medium text-fin-text-primary">{d.nomeConta}</td>

                  {/* Saldo inicial — editável */}
                  <td className="px-5 py-3 text-right">
                    {editando === d.contaId ? (
                      <div className="flex items-center justify-end gap-1">
                        <input
                          autoFocus
                          type="number"
                          step="0.01"
                          value={valorEdit}
                          onChange={(e) => setValorEdit(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') confirmEdit(d.contaId)
                            if (e.key === 'Escape') setEditando(null)
                          }}
                          className="w-28 rounded border border-fin-brand bg-fin-surface text-fin-text-primary px-2 py-0.5 text-right text-sm focus:outline-none focus:shadow-fin-focus"
                        />
                        <button onClick={() => confirmEdit(d.contaId)} className="text-fin-positive hover:text-fin-positive-hover text-xs font-medium">✓</button>
                        <button onClick={() => setEditando(null)} className="text-fin-text-muted hover:text-fin-text-secondary text-xs">✕</button>
                      </div>
                    ) : (
                      <button
                        onClick={() => startEdit(d.contaId, d.saldoInicial)}
                        className="group flex items-center justify-end gap-1 w-full text-right hover:text-fin-brand transition"
                        title="Clique para editar"
                      >
                        <span className={d.saldoInicial !== 0 ? 'text-fin-text-secondary' : 'text-fin-text-muted'}>
                          {formatCurrency(d.saldoInicial)}
                        </span>
                        <span className="text-fin-text-muted group-hover:text-fin-brand text-xs">✏</span>
                      </button>
                    )}
                  </td>

                  <td className="px-5 py-3 text-right text-fin-positive">{formatCurrency(d.totalEntradas)}</td>
                  <td className="px-5 py-3 text-right text-fin-negative">{formatCurrency(d.totalSaidas)}</td>
                  <td className={`px-5 py-3 text-right font-semibold ${d.saldoFinal >= 0 ? 'text-fin-text-primary' : 'text-fin-negative'}`}>
                    {formatCurrency(d.saldoFinal)}
                  </td>
                  <td className={`px-5 py-3 text-right text-sm ${d.diferenca >= 0 ? 'text-fin-positive' : 'text-fin-negative'}`}>
                    {d.diferenca >= 0 ? '+' : ''}{formatCurrency(d.diferenca)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!temDados && (
            <p className="px-5 py-3 text-center text-xs text-fin-text-muted">
              Nenhuma transação ou saldo registrado neste mês. Use "Calcular saldo inicial" para inicializar a partir das transações existentes.
            </p>
          )}
        </div>
      )}
    </Card>
  )
}
