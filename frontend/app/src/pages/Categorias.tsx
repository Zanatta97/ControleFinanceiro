import { useEffect, useState } from 'react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/categoria'
import type { CategoriaResponse, CategoriaRequest } from '../types/api'
import Button from '../components/ui/Button'
import Modal from '../components/ui/Modal'
import Input from '../components/ui/Input'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'

const emptyForm: CategoriaRequest = { nome: '', cor: '#22c55e', urlIcone: '' }

export default function Categorias() {
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(false)
  const [editing, setEditing] = useState<CategoriaResponse | null>(null)
  const [form, setForm] = useState<CategoriaRequest>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    try {
      const { data } = await listarDoAmbiente()
      setCategorias(data.dados ?? [])
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

  function openEdit(c: CategoriaResponse) {
    setEditing(c)
    setForm({ nome: c.nome ?? '', cor: c.cor ?? '#22c55e', urlIcone: c.urlIcone ?? '' })
    setError('')
    setModal(true)
  }

  async function handleSave() {
    if (!form.nome.trim()) { setError('Nome é obrigatório.'); return }
    if (!form.cor) { setError('Cor é obrigatória.'); return }
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
      setError('Erro ao salvar categoria.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Excluir esta categoria?')) return
    await excluir(id)
    await load()
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Categorias</h1>
        <Button onClick={openNew}>+ Nova Categoria</Button>
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Carregando...</div>
      ) : categorias.length === 0 ? (
        <Card className="p-12 text-center">
          <p className="text-4xl mb-3">🏷️</p>
          <p className="text-gray-500">Nenhuma categoria cadastrada.</p>
          <Button className="mt-4" onClick={openNew}>Criar primeira categoria</Button>
        </Card>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {categorias.map((c) => (
            <Card key={c.id} className="flex items-center gap-4 p-4">
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-white font-bold" style={{ backgroundColor: c.cor ?? '#9ca3af' }}>
                {c.urlIcone ? <img src={c.urlIcone} alt="" className="h-5 w-5" /> : c.nome?.[0]?.toUpperCase()}
              </span>
              <div className="flex-1 min-w-0">
                <p className="font-medium text-gray-800 truncate">{c.nome}</p>
                <p className="text-xs text-gray-400 font-mono">{c.cor}</p>
              </div>
              <div className="flex gap-1 shrink-0">
                <button onClick={() => openEdit(c)} className="rounded p-1 hover:bg-gray-100 text-gray-400 hover:text-gray-600">✏️</button>
                <button onClick={() => handleDelete(c.id)} className="rounded p-1 hover:bg-red-50 text-gray-400 hover:text-red-600">🗑️</button>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Modal open={modal} onClose={() => setModal(false)} title={editing ? 'Editar Categoria' : 'Nova Categoria'}>
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}
          <Input label="Nome" value={form.nome} onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))} placeholder="Ex: Alimentação, Transporte..." required />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Cor</label>
            <div className="flex items-center gap-3">
              <input type="color" value={form.cor} onChange={(e) => setForm((f) => ({ ...f, cor: e.target.value }))} className="h-10 w-16 cursor-pointer rounded border border-gray-300 p-0.5" />
              <span className="text-sm font-mono text-gray-500">{form.cor}</span>
            </div>
          </div>
          <Input label="URL do Ícone (opcional)" value={form.urlIcone ?? ''} onChange={(e) => setForm((f) => ({ ...f, urlIcone: e.target.value }))} placeholder="https://..." />
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModal(false)}>Cancelar</Button>
            <Button onClick={handleSave} loading={saving}>Salvar</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
