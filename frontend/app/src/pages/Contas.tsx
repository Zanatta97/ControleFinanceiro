import { useEffect, useState } from 'react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/conta'
import type { ContaResponse, ContaRequest } from '../types/api'
import { TipoConta } from '../types/api'
import { formatCurrency } from '../utils/format'
import Button from '../components/ui/Button'
import Modal from '../components/ui/Modal'
import Input from '../components/ui/Input'
import Select from '../components/ui/Select'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'

const tipoOptions = [
  { value: TipoConta.Corrente, label: 'Conta Corrente' },
  { value: TipoConta.Poupanca, label: 'Poupança' },
  { value: TipoConta.Carteira, label: 'Carteira' },
  { value: TipoConta.Cartao, label: 'Cartão' },
]

const tipoLabel: Record<TipoConta, string> = {
  [TipoConta.Corrente]: 'Corrente',
  [TipoConta.Poupanca]: 'Poupança',
  [TipoConta.Carteira]: 'Carteira',
  [TipoConta.Cartao]: 'Cartão',
}

const tipoIcon: Record<TipoConta, string> = {
  [TipoConta.Corrente]: '🏦',
  [TipoConta.Poupanca]: '🐷',
  [TipoConta.Carteira]: '👛',
  [TipoConta.Cartao]: '💳',
}

const emptyForm: ContaRequest = { nome: '', tipoConta: TipoConta.Corrente, saldo: 0 }

export default function Contas() {
  const [contas, setContas] = useState<ContaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(false)
  const [editing, setEditing] = useState<ContaResponse | null>(null)
  const [form, setForm] = useState<ContaRequest>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    try {
      const { data } = await listarDoAmbiente()
      setContas(data.dados ?? [])
    } finally {
      setLoading(false)
    }
  }

  function openNew() {
    setEditing(null)
    setForm(emptyForm)
    setError('')
    setModal(true)
  }

  function openEdit(c: ContaResponse) {
    setEditing(c)
    setForm({ nome: c.nome, tipoConta: c.tipoConta, saldo: c.saldo })
    setError('')
    setModal(true)
  }

  async function handleSave() {
    if (!form.nome.trim()) { setError('Nome é obrigatório.'); return }
    setSaving(true)
    setError('')
    try {
      if (editing) {
        await atualizar(editing.id, form)
      } else {
        await criar(form)
      }
      setModal(false)
      await load()
    } catch {
      setError('Erro ao salvar conta.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Excluir esta conta?')) return
    await excluir(id)
    await load()
  }

  const saldoTotal = contas.reduce((s, c) => s + c.saldo, 0)

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">Contas</h1>
          <p className="text-sm text-gray-500">Saldo total: <span className="font-semibold text-gray-800">{formatCurrency(saldoTotal)}</span></p>
        </div>
        <Button onClick={openNew}>+ Nova Conta</Button>
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Carregando...</div>
      ) : contas.length === 0 ? (
        <Card className="p-12 text-center">
          <p className="text-4xl mb-3">🏦</p>
          <p className="text-gray-500">Nenhuma conta cadastrada.</p>
          <Button className="mt-4" onClick={openNew}>Criar primeira conta</Button>
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {contas.map((c) => (
            <Card key={c.id} className="p-5">
              <div className="flex items-start justify-between">
                <div className="flex items-center gap-3">
                  <span className="text-3xl">{tipoIcon[c.tipoConta]}</span>
                  <div>
                    <p className="font-semibold text-gray-800">{c.nome}</p>
                    <p className="text-xs text-gray-500">{tipoLabel[c.tipoConta]}</p>
                  </div>
                </div>
                <div className="flex gap-1">
                  <button onClick={() => openEdit(c)} className="rounded p-1 hover:bg-gray-100 text-gray-400 hover:text-gray-600">✏️</button>
                  <button onClick={() => handleDelete(c.id)} className="rounded p-1 hover:bg-red-50 text-gray-400 hover:text-red-600">🗑️</button>
                </div>
              </div>
              <p className={`mt-4 text-2xl font-bold ${c.saldo >= 0 ? 'text-gray-900' : 'text-red-600'}`}>{formatCurrency(c.saldo)}</p>
            </Card>
          ))}
        </div>
      )}

      <Modal open={modal} onClose={() => setModal(false)} title={editing ? 'Editar Conta' : 'Nova Conta'}>
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}
          <Input label="Nome" value={form.nome} onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))} placeholder="Ex: Nubank, Bradesco..." required />
          <Select
            label="Tipo"
            value={form.tipoConta}
            onChange={(e) => setForm((f) => ({ ...f, tipoConta: Number(e.target.value) as TipoConta }))}
            options={tipoOptions}
          />
          <Input
            label="Saldo Inicial"
            type="number"
            step="0.01"
            value={form.saldo ?? 0}
            onChange={(e) => setForm((f) => ({ ...f, saldo: parseFloat(e.target.value) || 0 }))}
          />
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModal(false)}>Cancelar</Button>
            <Button onClick={handleSave} loading={saving}>Salvar</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
