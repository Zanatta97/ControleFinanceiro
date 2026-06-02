import { useEffect, useState } from 'react'
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

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    try {
      const [o, s, cat] = await Promise.all([
        listarDoAmbiente(),
        statusOrcamentos(),
        listarCategorias(),
      ])
      setOrcamentos(o.data.dados ?? [])
      setStatus(s.data.dados ?? [])
      setCategorias(cat.data.dados ?? [])
    } finally {
      setLoading(false)
    }
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

  async function handleDelete(id: string) {
    if (!confirm('Excluir este orçamento?')) return
    await excluir(id)
    await load()
  }

  const filtrados = filtro === 'ativos'
    ? orcamentos.filter((o) => o.statusOrcamento === StatusOrcamento.Ativo)
    : orcamentos

  function getStatus(id: string) {
    return status.find((s) => s.id === id)
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Orçamentos</h1>
        <Button onClick={openNew}>+ Novo Orçamento</Button>
      </div>

      <div className="flex gap-2">
        {(['ativos', 'todos'] as const).map((f) => (
          <button
            key={f}
            onClick={() => setFiltro(f)}
            className={`rounded-lg px-4 py-1.5 text-sm font-medium transition ${filtro === f ? 'bg-green-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'}`}
          >
            {f === 'ativos' ? 'Ativos' : 'Todos'}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Carregando...</div>
      ) : filtrados.length === 0 ? (
        <Card className="p-12 text-center">
          <p className="text-4xl mb-3">📋</p>
          <p className="text-gray-500">Nenhum orçamento encontrado.</p>
          <Button className="mt-4" onClick={openNew}>Criar primeiro orçamento</Button>
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {filtrados.map((o) => {
            const st = getStatus(o.id)
            const pct = st ? Math.min(st.percentual, 100) : 0
            const color = pct >= 90 ? '#ef4444' : pct >= 70 ? '#f97316' : '#22c55e'
            return (
              <Card key={o.id} className="p-5">
                <div className="flex items-start justify-between mb-3">
                  <div>
                    <p className="font-semibold text-gray-800">{o.nome}</p>
                    <p className="text-xs text-gray-500">{st?.nomeCategoria ?? categorias.find(c => c.id === o.categoriaId)?.nome ?? '-'}</p>
                  </div>
                  <div className="flex gap-1">
                    <button onClick={() => openEdit(o)} className="rounded p-1 hover:bg-gray-100 text-gray-400">✏️</button>
                    <button onClick={() => handleDelete(o.id)} className="rounded p-1 hover:bg-red-50 text-gray-400 hover:text-red-600">🗑️</button>
                  </div>
                </div>

                {st && (
                  <>
                    <div className="flex justify-between text-sm mb-1">
                      <span className="text-gray-600">{formatCurrency(st.valorGasto)}</span>
                      <span className="text-gray-400">de {formatCurrency(st.valorLimite)}</span>
                    </div>
                    <div className="h-2 rounded-full bg-gray-100">
                      <div className="h-2 rounded-full transition-all" style={{ width: `${pct}%`, backgroundColor: color }} />
                    </div>
                    <p className="mt-1 text-xs text-gray-400">{pct.toFixed(1)}% utilizado</p>
                  </>
                )}
                {!st && (
                  <p className="text-sm text-gray-500">Limite: {formatCurrency(o.valorLimite)}</p>
                )}

                <p className="mt-2 text-xs text-gray-400">Vence em {formatDate(o.dataLimite)}</p>
                <span className={`mt-2 inline-block rounded-full px-2 py-0.5 text-xs font-medium ${o.statusOrcamento === StatusOrcamento.Ativo ? 'bg-green-100 text-green-700' : 'bg-gray-100 text-gray-500'}`}>
                  {o.statusOrcamento === StatusOrcamento.Ativo ? 'Ativo' : 'Encerrado'}
                </span>
              </Card>
            )
          })}
        </div>
      )}

      <Drawer open={modal} onClose={() => setModal(false)} title={editing ? 'Editar Orçamento' : 'Novo Orçamento'}>
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
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModal(false)}>Cancelar</Button>
            <Button onClick={handleSave} loading={saving}>Salvar</Button>
          </div>
        </div>
      </Drawer>
    </div>
  )
}
