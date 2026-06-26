import { useEffect, useState } from 'react'
import { Icon } from '@iconify/react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/categoria'
import type { CategoriaResponse, CategoriaRequest } from '../types/api'
import Button from '../components/ui/Button'
import Drawer from '../components/ui/Drawer'
import Input from '../components/ui/Input'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'
import IconPicker from '../components/ui/IconPicker'
import ConfirmDialog from '../components/ui/ConfirmDialog'

function isIconifyName(value: string) {
  return !!value && value.includes(':') && !value.startsWith('http')
}

const emptyForm: CategoriaRequest = { nome: '', cor: '#2D5BE3', urlIcone: '' }

export default function Categorias() {
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(false)
  const [editing, setEditing] = useState<CategoriaResponse | null>(null)
  const [form, setForm] = useState<CategoriaRequest>(emptyForm)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [toDelete, setToDelete] = useState<CategoriaResponse | null>(null)
  const [deleting, setDeleting] = useState(false)

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
    setForm({ nome: c.nome ?? '', cor: c.cor ?? '#2D5BE3', urlIcone: c.urlIcone ?? '' })
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

  return (
    <div className="flex flex-col gap-[18px]">
      <div className="flex items-center justify-between">
        <h1 className="text-[22px] font-bold text-fin-text-primary">Categorias</h1>
        <Button onClick={openNew}>
          <Icon icon="lucide:plus" width={16} height={16} />
          Nova categoria
        </Button>
      </div>

      {loading ? (
        <div className="text-center py-12 text-fin-text-muted">Carregando...</div>
      ) : categorias.length === 0 ? (
        <Card className="p-12 text-center">
          <Icon icon="lucide:tag" width={40} height={40} className="mx-auto mb-3 text-fin-text-muted" />
          <p className="text-fin-text-secondary">Nenhuma categoria cadastrada.</p>
          <Button className="mt-4" onClick={openNew}>Criar primeira categoria</Button>
        </Card>
      ) : (
        <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
          {categorias.map((c) => (
            <Card key={c.id} className="flex items-center gap-3.5 p-3.5">
              <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full text-[15px] font-bold text-white" style={{ backgroundColor: c.cor ?? 'var(--fin-text-muted)' }}>
                {c.urlIcone && isIconifyName(c.urlIcone)
                  ? <Icon icon={c.urlIcone} width={20} height={20} color="white" />
                  : c.urlIcone
                  ? <img src={c.urlIcone} alt="" className="h-5 w-5" />
                  : c.nome?.[0]?.toUpperCase()}
              </span>
              <div className="min-w-0 flex-1">
                <p className="truncate font-semibold text-fin-text-primary">{c.nome}</p>
                <p className="font-fin-mono text-[11px] text-fin-text-muted">{c.cor}</p>
              </div>
              <div className="flex shrink-0 gap-0.5">
                <button onClick={() => openEdit(c)} title="Editar" className="flex h-7 w-7 items-center justify-center rounded-[7px] text-fin-text-muted transition hover:bg-fin-surface-2 hover:text-fin-text-primary">
                  <Icon icon="lucide:pencil" width={15} height={15} />
                </button>
                <button onClick={() => setToDelete(c)} title="Excluir" className="flex h-7 w-7 items-center justify-center rounded-[7px] text-fin-text-muted transition hover:bg-fin-negative-soft hover:text-fin-negative">
                  <Icon icon="lucide:trash-2" width={15} height={15} />
                </button>
              </div>
            </Card>
          ))}
        </div>
      )}

      <Drawer
        open={modal}
        onClose={() => setModal(false)}
        title={editing ? 'Editar Categoria' : 'Nova Categoria'}
        footer={
          <>
            <Button variant="secondary" onClick={() => setModal(false)} className="hidden sm:inline-flex">Cancelar</Button>
            <Button onClick={handleSave} loading={saving} className="flex-1 sm:flex-none">Salvar</Button>
          </>
        }
      >
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}
          <Input label="Nome" value={form.nome} onChange={(e) => setForm((f) => ({ ...f, nome: e.target.value }))} placeholder="Ex: Alimentação, Transporte..." required />
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-fin-text-primary">Cor</label>
            <div className="flex items-center gap-3">
              <input type="color" value={form.cor} onChange={(e) => setForm((f) => ({ ...f, cor: e.target.value }))} className="h-10 w-16 cursor-pointer rounded border border-fin-border p-0.5" />
              <span className="text-sm font-mono text-fin-text-secondary">{form.cor}</span>
            </div>
          </div>
          <IconPicker
            value={form.urlIcone ?? ''}
            onChange={(icon) => setForm((f) => ({ ...f, urlIcone: icon }))}
            previewColor={form.cor}
          />
        </div>
      </Drawer>

      <ConfirmDialog
        open={!!toDelete}
        variant="delete"
        title="Excluir categoria"
        message={<>Tem certeza que deseja excluir a categoria <strong className="text-fin-text-primary">{toDelete?.nome}</strong>?</>}
        confirmLabel="Excluir"
        loading={deleting}
        onConfirm={handleDelete}
        onClose={() => setToDelete(null)}
      />
    </div>
  )
}
