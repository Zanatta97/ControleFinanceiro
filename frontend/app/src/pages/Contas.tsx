import { useEffect, useState } from 'react'
import { Icon } from '@iconify/react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/conta'
import type { ContaResponse, ContaRequest } from '../types/api'
import { TipoConta } from '../types/api'
import { formatCurrency } from '../utils/format'
import Button from '../components/ui/Button'
import Drawer from '../components/ui/Drawer'
import Input from '../components/ui/Input'
import Select from '../components/ui/Select'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'
import ConfirmDialog from '../components/ui/ConfirmDialog'

const tipoOptions = [
  { value: TipoConta.Corrente, label: 'Conta Corrente' },
  { value: TipoConta.Poupanca, label: 'Poupança' },
  { value: TipoConta.Investimento, label: 'Investimento' },
  { value: TipoConta.CartaoCredito, label: 'Cartão de Crédito' },
]

const tipoLabel: Record<TipoConta, string> = {
  [TipoConta.Corrente]: 'Corrente',
  [TipoConta.Poupanca]: 'Poupança',
  [TipoConta.Investimento]: 'Investimento',
  [TipoConta.CartaoCredito]: 'Cartão de Crédito',
}

// Ícone (Lucide) + cores por tipo de conta.
const tipoStyle: Record<TipoConta, { icon: string; cor: string; soft: string }> = {
  [TipoConta.Corrente]: { icon: 'lucide:landmark', cor: 'var(--fin-brand)', soft: 'var(--fin-brand-soft)' },
  [TipoConta.Poupanca]: { icon: 'lucide:piggy-bank', cor: 'var(--fin-positive)', soft: 'var(--fin-positive-soft)' },
  [TipoConta.Investimento]: { icon: 'lucide:trending-up', cor: 'var(--fin-invest)', soft: 'var(--fin-invest-soft)' },
  [TipoConta.CartaoCredito]: { icon: 'lucide:credit-card', cor: 'var(--fin-warning)', soft: 'var(--fin-warning-soft)' },
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
  const [toDelete, setToDelete] = useState<ContaResponse | null>(null)
  const [deleting, setDeleting] = useState(false)

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

  async function handleDelete() {
    if (!toDelete) return
    setDeleting(true)
    try {
      await excluir(toDelete.id)
      setToDelete(null)
      await load()
    } finally {
      setDeleting(false)
    }
  }

  const saldoTotal = contas.reduce((s, c) => s + c.saldo, 0)

  return (
    <div className="flex flex-col gap-[18px]">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-[22px] font-bold text-fin-text-primary">Contas</h1>
          <p className="text-sm text-fin-text-secondary">Saldo total: <span className="font-semibold text-fin-text-primary">{formatCurrency(saldoTotal)}</span></p>
        </div>
        <Button onClick={openNew}>
          <Icon icon="lucide:plus" width={16} height={16} />
          Nova conta
        </Button>
      </div>

      {loading ? (
        <div className="py-12 text-center text-fin-text-muted">Carregando...</div>
      ) : contas.length === 0 ? (
        <Card className="p-12 text-center">
          <Icon icon="lucide:landmark" width={40} height={40} className="mx-auto mb-3 text-fin-text-muted" />
          <p className="text-fin-text-secondary">Nenhuma conta cadastrada.</p>
          <Button className="mt-4" onClick={openNew}>Criar primeira conta</Button>
        </Card>
      ) : (
        <div className="grid gap-3.5 sm:grid-cols-2 lg:grid-cols-3">
          {contas.map((c) => {
            const st = tipoStyle[c.tipoConta]
            return (
              <Card key={c.id} className="p-[18px]">
                <div className="flex items-start justify-between">
                  <div className="flex items-center gap-3">
                    <div className="flex h-[42px] w-[42px] items-center justify-center rounded-[11px]" style={{ backgroundColor: st.soft, color: st.cor }}>
                      <Icon icon={st.icon} width={20} height={20} />
                    </div>
                    <div>
                      <p className="font-semibold text-fin-text-primary">{c.nome}</p>
                      <p className="text-[11.5px] text-fin-text-muted">{tipoLabel[c.tipoConta]}</p>
                    </div>
                  </div>
                  <div className="flex gap-0.5">
                    <button onClick={() => openEdit(c)} title="Editar" className="flex h-7 w-7 items-center justify-center rounded-[7px] text-fin-text-muted transition hover:bg-fin-surface-2 hover:text-fin-text-primary">
                      <Icon icon="lucide:pencil" width={15} height={15} />
                    </button>
                    <button onClick={() => setToDelete(c)} title="Excluir" className="flex h-7 w-7 items-center justify-center rounded-[7px] text-fin-text-muted transition hover:bg-fin-negative-soft hover:text-fin-negative">
                      <Icon icon="lucide:trash-2" width={15} height={15} />
                    </button>
                  </div>
                </div>
                <p className={`mt-4 font-fin-mono text-[23px] font-medium ${c.saldo >= 0 ? 'text-fin-text-primary' : 'text-fin-negative'}`}>{formatCurrency(c.saldo)}</p>
              </Card>
            )
          })}
        </div>
      )}

      <Drawer
        open={modal}
        onClose={() => setModal(false)}
        title={editing ? 'Editar Conta' : 'Nova Conta'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(false)} className="hidden sm:inline-flex">Cancelar</Button>
            <Button onClick={handleSave} loading={saving} className="flex-1 sm:flex-none">Salvar</Button>
          </>
        }
      >
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
        </div>
      </Drawer>

      <ConfirmDialog
        open={!!toDelete}
        variant="delete"
        title="Excluir conta"
        message={<>Tem certeza que deseja excluir a conta <strong className="text-fin-text-primary">{toDelete?.nome}</strong>? Esta ação não pode ser desfeita.</>}
        confirmLabel="Excluir"
        loading={deleting}
        onConfirm={handleDelete}
        onClose={() => setToDelete(null)}
      />
    </div>
  )
}
