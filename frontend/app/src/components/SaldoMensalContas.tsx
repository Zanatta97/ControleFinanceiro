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

  if (loading) return <div className="py-4 text-center text-sm text-gray-400">Carregando saldos...</div>

  const temDados = dados.some((d) => d.saldoInicial !== 0 || d.totalEntradas !== 0 || d.totalSaidas !== 0)

  return (
    <Card>
      <div className="flex items-center justify-between border-b px-5 py-4">
        <div>
          <h2 className="font-semibold text-gray-800">Saldo das Contas</h2>
          <p className="text-xs text-gray-400 mt-0.5">Clique no saldo inicial para editar manualmente.</p>
        </div>
        <Button size="sm" variant="secondary" onClick={handleCalcular} loading={calculando}>
          Calcular saldo inicial
        </Button>
      </div>

      {dados.length === 0 ? (
        <p className="px-5 py-6 text-center text-sm text-gray-400">Nenhuma conta encontrada.</p>
      ) : (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left text-xs font-medium uppercase tracking-wide text-gray-500">
                <th className="px-5 py-3">Conta</th>
                <th className="px-5 py-3 text-right">Saldo Inicial</th>
                <th className="px-5 py-3 text-right">Entradas</th>
                <th className="px-5 py-3 text-right">Saídas</th>
                <th className="px-5 py-3 text-right">Saldo Final</th>
                <th className="px-5 py-3 text-right">Diferença</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {dados.map((d) => (
                <tr key={d.contaId} className="hover:bg-gray-50 transition">
                  <td className="px-5 py-3 font-medium text-gray-800">{d.nomeConta}</td>

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
                          className="w-28 rounded border border-green-400 px-2 py-0.5 text-right text-sm focus:outline-none focus:ring-1 focus:ring-green-500"
                        />
                        <button onClick={() => confirmEdit(d.contaId)} className="text-green-600 hover:text-green-800 text-xs font-medium">✓</button>
                        <button onClick={() => setEditando(null)} className="text-gray-400 hover:text-gray-600 text-xs">✕</button>
                      </div>
                    ) : (
                      <button
                        onClick={() => startEdit(d.contaId, d.saldoInicial)}
                        className="group flex items-center justify-end gap-1 w-full text-right hover:text-green-700 transition"
                        title="Clique para editar"
                      >
                        <span className={d.saldoInicial !== 0 ? 'text-gray-700' : 'text-gray-400'}>
                          {formatCurrency(d.saldoInicial)}
                        </span>
                        <span className="text-gray-300 group-hover:text-green-500 text-xs">✏</span>
                      </button>
                    )}
                  </td>

                  <td className="px-5 py-3 text-right text-green-600">{formatCurrency(d.totalEntradas)}</td>
                  <td className="px-5 py-3 text-right text-red-500">{formatCurrency(d.totalSaidas)}</td>
                  <td className={`px-5 py-3 text-right font-semibold ${d.saldoFinal >= 0 ? 'text-gray-800' : 'text-red-600'}`}>
                    {formatCurrency(d.saldoFinal)}
                  </td>
                  <td className={`px-5 py-3 text-right text-sm ${d.diferenca >= 0 ? 'text-green-600' : 'text-red-500'}`}>
                    {d.diferenca >= 0 ? '+' : ''}{formatCurrency(d.diferenca)}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          {!temDados && (
            <p className="px-5 py-3 text-center text-xs text-gray-400">
              Nenhuma transação ou saldo registrado neste mês. Use "Calcular saldo inicial" para inicializar a partir das transações existentes.
            </p>
          )}
        </div>
      )}
    </Card>
  )
}
