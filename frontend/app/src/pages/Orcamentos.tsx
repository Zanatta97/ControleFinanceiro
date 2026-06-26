import { useEffect, useState } from 'react'
import { Icon } from '@iconify/react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/orcamento'
import { listarDoAmbiente as listarCategorias } from '../api/categoria'
import { statusOrcamentos } from '../api/relatorio'
import type { OrcamentoResponse, OrcamentoRequest, CategoriaResponse, OrcamentoStatusResponse } from '../types/api'
import { StatusOrcamento } from '../types/api'
import { formatCurrency, formatDate } from '../utils/format'
import Button from '../components/ui/Button'
import Drawer from '../components/ui/Drawer'
import Input from '../components/ui/Input'
import Select from '../components/ui/Select'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'
import ConfirmDialog from '../components/ui/ConfirmDialog'

const statusOptions = [
  { value: StatusOrcamento.Ativo, label: 'Ativo' },
  { value: StatusOrcamento.Encerrado, label: 'Encerrado' },
]

function emptyForm(categorias: CategoriaResponse[]): OrcamentoRequest {
  return {
    nome: '',
    descricao: '',
    valorLimite: 0,
    dataLimite: new Date(new Date().getFullYear(), new Date().getMonth() + 1, 0).toISOString().slice(0, 16),
    statusOrcamento: StatusOrcamento.Ativo,
    categoriaId: categorias[0]?.id ?? '',
  }
}

export default function Orcamentos() {
  const [orcamentos, setOrcamentos] = useState<OrcamentoResponse[]>([])
  const [status, setStatus] = useState<OrcamentoStatusResponse[]>([])
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(false)
  const [editing, setEditing] = useState<OrcamentoResponse | null>(null)
  const [form, setForm] = useState<OrcamentoRequest>(emptyForm([]))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [filtro, setFiltro] = useState<'todos' | 'ativos'>('ativos')
  const [toDelete, setToDelete] = useState<OrcamentoResponse | null>(null)
  const [deleting, setDeleting] = useState(false)

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    const [o, s, cat] = await Promise.allSettled([
      listarDoAmbiente(),
      statusOrcamentos(),
      listarCategorias(),
    ])
    if (o.status === 'fulfilled') setOrcamentos(o.value.data.dados ?? [])
    if (s.status === 'fulfilled') setStatus(s.value.data.dados ?? [])
    if (cat.status === 'fulfilled') setCategorias(cat.value.data.dados ?? [])
    setLoading(false)
  }

  function openNew() {
    setEditing(null)
    setForm(emptyForm(categorias))
    setError('')
    setModal(true)
  }

  function openEdit(o: OrcamentoResponse) {
    setEditing(o)
    setForm({
      nome: o.nome ?? '',
      descricao: o.descricao ?? '',
      valorLimite: o.valorLimite,
      dataLimite: o.dataLimite.slice(0, 16),
      statusOrcamento: o.statusOrcamento,
      categoriaId: o.categoriaId,
    })
    setError('')
    setModal(true)
  }

  async function handleSave() {
    if (!form.nome.trim()) { setError('Nome é obrigatório.'); return }
    if (!form.categoriaId) { setError('Selecione uma categoria.'); return }
    setSaving(true)
    setError('')
    try {
      const payload = { ...form, dataLimite: new Date(form.dataLimite).toISOString() }
      if (editing) {
        await atualizar(editing.id, payload)
      } else {
        await criar(payload)
      }
      setModal(false)
      await load()
    } catch {
      setError('Erro ao salvar orçamento.')
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

  const filtrados = filtro === 'ativos'
    ? orcamentos.filter((o) => o.statusOrcamento === StatusOrcamento.Ativo)
    : orcamentos

  function getStatus(id: string) {
    return status.find((s) => s.id === id)
  }

  return (
    <div className="flex flex-col gap-4">
      <div className="flex items-center justify-between">
        <h1 className="text-[22px] font-bold text-fin-text-primary">Orçamentos</h1>
        <Button onClick={openNew}>
          <Icon icon="lucide:plus" width={16} height={16} />
          Novo orçamento
        </Button>
      </div>

      <div className="flex gap-2">
        {(['ativos', 'todos'] as const).map((f) => (
          <button
            key={f}
            onClick={() => setFiltro(f)}
            className={`rounded-lg px-4 py-1.5 text-sm font-medium transition ${filtro === f ? 'bg-fin-brand text-white' : 'bg-fin-surface border border-fin-border text-fin-text-secondary hover:bg-fin-surface-2'}`}
          >
            {f === 'ativos' ? 'Ativos' : 'Todos'}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-12 text-fin-text-muted">Carregando...</div>
      ) : filtrados.length === 0 ? (
        <Card className="p-12 text-center">
          <Icon icon="lucide:target" width={40} height={40} className="mx-auto mb-3 text-fin-text-muted" />
          <p className="text-fin-text-secondary">Nenhum orçamento encontrado.</p>
          <Button className="mt-4" onClick={openNew}>Criar primeiro orçamento</Button>
        </Card>
      ) : (
        <div className="grid gap-3.5 sm:grid-cols-2 lg:grid-cols-3">
          {filtrados.map((o) => {
            const st = getStatus(o.id)
            const pct = st ? Math.min(st.percentual, 100) : 0
            const color = pct >= 90 ? 'var(--fin-negative)' : pct >= 70 ? 'var(--fin-warning)' : 'var(--fin-positive)'
            const ativo = o.statusOrcamento === StatusOrcamento.Ativo
            return (
              <Card key={o.id} className="p-[18px]">
                <div className="mb-3 flex items-start justify-between">
                  <div>
                    <p className="font-semibold text-fin-text-primary">{o.nome}</p>
                    <p className="text-[11.5px] text-fin-text-muted">{st?.nomeCategoria ?? categorias.find(c => c.id === o.categoriaId)?.nome ?? '-'}</p>
                  </div>
                  <div className="flex gap-0.5">
                    <button onClick={() => openEdit(o)} title="Editar" className="flex h-7 w-7 items-center justify-center rounded-[7px] text-fin-text-muted transition hover:bg-fin-surface-2 hover:text-fin-text-primary">
                      <Icon icon="lucide:pencil" width={15} height={15} />
                    </button>
                    <button onClick={() => setToDelete(o)} title="Excluir" className="flex h-7 w-7 items-center justify-center rounded-[7px] text-fin-text-muted transition hover:bg-fin-negative-soft hover:text-fin-negative">
                      <Icon icon="lucide:trash-2" width={15} height={15} />
                    </button>
                  </div>
                </div>

                {st ? (
                  <>
                    <div className="mb-1.5 flex justify-between text-[12.5px]">
                      <span className="font-fin-mono text-fin-text-secondary">{formatCurrency(st.valorGasto)}</span>
                      <span className="text-fin-text-muted">de {formatCurrency(st.valorLimite)}</span>
                    </div>
                    <div className="h-2 overflow-hidden rounded-[5px] bg-fin-surface-2">
                      <div className="h-2 rounded-[5px] transition-all" style={{ width: `${pct}%`, backgroundColor: color }} />
                    </div>
                    <p className="mt-[7px] text-[11.5px] text-fin-text-muted">{pct.toFixed(1).replace('.', ',')}% utilizado · vence {formatDate(o.dataLimite)}</p>
                  </>
                ) : (
                  <>
                    <p className="text-sm text-fin-text-secondary">Limite: <span className="font-fin-mono">{formatCurrency(o.valorLimite)}</span></p>
                    <p className="mt-[7px] text-[11.5px] text-fin-text-muted">Vence em {formatDate(o.dataLimite)}</p>
                  </>
                )}

                <span className={`mt-[9px] inline-block rounded-full px-[9px] py-0.5 text-[11px] font-semibold ${ativo ? 'bg-fin-positive-soft text-fin-positive' : 'bg-fin-surface-2 text-fin-text-muted'}`}>
                  {ativo ? 'Ativo' : 'Encerrado'}
                </span>
              </Card>
            )
          })}
        </div>
      )}

      <Drawer
        open={modal}
        onClose={() => setModal(false)}
        title={editing ? 'Editar Orçamento' : 'Novo Orçamento'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(false)} className="hidden sm:inline-flex">Cancelar</Button>
            <Button onClick={handleSave} loading={saving} className="flex-1 sm:flex-none">Salvar</Button>
          </>
        }
      >
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}
          <div className="grid grid-cols-1 gap-4">
            <Input label="Nome" value={form.nome} onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))} placeholder="Ex: Alimentação Mensal" required />
            <Input label="Descrição" value={form.descricao} onChange={(e) => setForm((f) => ({ ...f, descricao: e.target.value }))} placeholder="Opcional..." />
            <Input label="Valor Limite" type="number" step="0.01" min="0" value={form.valorLimite} onChange={(e) => setForm((f) => ({ ...f, valorLimite: parseFloat(e.target.value) || 0 }))} />
            <Input label="Data Limite" type="datetime-local" value={form.dataLimite} onChange={(e) => setForm((f) => ({ ...f, dataLimite: e.target.value }))} />
            <Select label="Categoria" value={form.categoriaId} onChange={(e) => setForm((f) => ({ ...f, categoriaId: e.target.value }))} options={categorias.map((c) => ({ value: c.id, label: c.nome ?? '' }))} />
            <Select label="Status" value={form.statusOrcamento} onChange={(e) => setForm((f) => ({ ...f, statusOrcamento: Number(e.target.value) as StatusOrcamento }))} options={statusOptions} />
          </div>
        </div>
      </Drawer>

      <ConfirmDialog
        open={!!toDelete}
        variant="delete"
        title="Excluir orçamento"
        message={<>Tem certeza que deseja excluir o orçamento <strong className="text-fin-text-primary">{toDelete?.nome}</strong>?</>}
        confirmLabel="Excluir"
        loading={deleting}
        onConfirm={handleDelete}
        onClose={() => setToDelete(null)}
      />
    </div>
  )
}
