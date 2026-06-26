import { useEffect, useState, useCallback } from 'react'
import { Icon } from '@iconify/react'
import { getSaldoMensal, salvarSaldoInicial, calcularSaldoInicial, type SaldoMensalContaResponse } from '../api/saldoMensal'
import { formatCurrency } from '../utils/format'
import Card from './ui/Card'
import Button from './ui/Button'
import ConfirmDialog from './ui/ConfirmDialog'

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

  // Confirmações
  const [confirmSaldo, setConfirmSaldo] = useState<{ contaId: string; nome: string; atual: number; novo: number } | null>(null)
  const [salvandoSaldo, setSalvandoSaldo] = useState(false)
  const [confirmCalcular, setConfirmCalcular] = useState(false)

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
    setConfirmCalcular(false)
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

  // Abre o diálogo de confirmação (Atual → Novo) ao invés de salvar direto.
  function requestSaveEdit(conta: SaldoMensalContaResponse) {
    const valor = parseFloat(valorEdit.replace(',', '.'))
    if (isNaN(valor)) { setEditando(null); return }
    if (valor === conta.saldoInicial) { setEditando(null); return }
    setConfirmSaldo({ contaId: conta.contaId, nome: conta.nomeConta ?? 'conta', atual: conta.saldoInicial, novo: valor })
  }

  async function confirmSaveSaldo() {
    if (!confirmSaldo) return
    setSalvandoSaldo(true)
    try {
      await salvarSaldoInicial(confirmSaldo.contaId, mes, ano, confirmSaldo.novo)
      setConfirmSaldo(null)
      setEditando(null)
      await load()
    } finally {
      setSalvandoSaldo(false)
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
        <Button size="sm" variant="secondary" onClick={() => setConfirmCalcular(true)} loading={calculando}>
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
                      <div className="flex items-center justify-end gap-1.5">
                        <input
                          autoFocus
                          type="number"
                          step="0.01"
                          value={valorEdit}
                          onChange={(e) => setValorEdit(e.target.value)}
                          onKeyDown={(e) => {
                            if (e.key === 'Enter') requestSaveEdit(d)
                            if (e.key === 'Escape') setEditando(null)
                          }}
                          className="w-[104px] rounded-[7px] border-[1.5px] border-fin-brand bg-fin-surface px-2 py-1 text-right font-fin-mono text-sm text-fin-text-primary focus:outline-none focus:shadow-fin-focus"
                        />
                        <button onClick={() => requestSaveEdit(d)} title="Salvar" className="flex h-7 w-7 items-center justify-center rounded-[7px] bg-fin-brand text-white transition hover:bg-fin-brand-hover">
                          <Icon icon="lucide:check" width={14} height={14} />
                        </button>
                        <button onClick={() => setEditando(null)} title="Cancelar" className="flex h-7 w-7 items-center justify-center rounded-[7px] border border-fin-border text-fin-text-muted transition hover:bg-fin-surface-2">
                          <Icon icon="lucide:x" width={13} height={13} />
                        </button>
                      </div>
                    ) : (
                      <button
                        onClick={() => startEdit(d.contaId, d.saldoInicial)}
                        className="group inline-flex items-center justify-end gap-1.5 rounded-[7px] border border-transparent px-1.5 py-1 font-fin-mono text-sm transition hover:border-fin-border hover:bg-fin-surface-2"
                        title="Clique para editar"
                      >
                        <span className={d.saldoInicial !== 0 ? 'text-fin-text-secondary' : 'text-fin-text-muted'}>
                          {formatCurrency(d.saldoInicial)}
                        </span>
                        <Icon icon="lucide:pencil" width={12} height={12} className="text-fin-text-muted group-hover:text-fin-brand" />
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

      {/* Confirmação de alteração do saldo inicial (Atual → Novo) */}
      <ConfirmDialog
        open={!!confirmSaldo}
        variant="info"
        title="Alterar saldo inicial"
        confirmLabel="Salvar"
        loading={salvandoSaldo}
        onConfirm={confirmSaveSaldo}
        onClose={() => { setConfirmSaldo(null); setEditando(null) }}
        message={
          <>
            Confirma o novo saldo inicial da conta <strong className="text-fin-text-primary">{confirmSaldo?.nome}</strong> para {mes}/{ano}?
            <div className="mt-4 flex items-center gap-3 rounded-[11px] bg-fin-surface-2 p-3.5">
              <div className="flex-1">
                <p className="text-[10.5px] font-semibold uppercase tracking-wide text-fin-text-muted">Atual</p>
                <p className="mt-1 font-fin-mono text-[15px] text-fin-text-secondary">{formatCurrency(confirmSaldo?.atual ?? 0)}</p>
              </div>
              <Icon icon="lucide:arrow-right" width={20} height={20} className="text-fin-text-muted" />
              <div className="flex-1">
                <p className="text-[10.5px] font-semibold uppercase tracking-wide text-fin-brand">Novo</p>
                <p className="mt-1 font-fin-mono text-[15px] font-medium text-fin-text-primary">{formatCurrency(confirmSaldo?.novo ?? 0)}</p>
              </div>
            </div>
          </>
        }
      />

      {/* Confirmação antes de recalcular o saldo inicial */}
      <ConfirmDialog
        open={confirmCalcular}
        variant="warning"
        title="Calcular saldo inicial"
        message="Isso recalcula o saldo inicial de todas as contas a partir das transações e sobrescreve valores ajustados manualmente. Deseja continuar?"
        confirmLabel="Calcular"
        loading={calculando}
        onConfirm={handleCalcular}
        onClose={() => setConfirmCalcular(false)}
      />
    </Card>
  )
}
