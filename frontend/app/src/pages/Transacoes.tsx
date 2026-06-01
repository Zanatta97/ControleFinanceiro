import { useEffect, useState } from 'react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/transacao'
import { listarDoAmbiente as listarContas } from '../api/conta'
import { listarDoAmbiente as listarCategorias } from '../api/categoria'
import type { TransacaoResponse, TransacaoRequest, ContaResponse, CategoriaResponse } from '../types/api'
import { TipoTransacao } from '../types/api'
import { formatCurrency, formatDate } from '../utils/format'
import Button from '../components/ui/Button'
import Modal from '../components/ui/Modal'
import Input from '../components/ui/Input'
import Select from '../components/ui/Select'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'
import Badge from '../components/ui/Badge'

const tipoOptions = [
  { value: TipoTransacao.Receita, label: 'Receita' },
  { value: TipoTransacao.Despesa, label: 'Despesa' },
]

function emptyForm(contas: ContaResponse[], categorias: CategoriaResponse[]): TransacaoRequest {
  return {
    descricao: '',
    valor: 0,
    data: new Date().toISOString().slice(0, 16),
    observacao: '',
    tipoTransacao: TipoTransacao.Despesa,
    categoriaId: categorias[0]?.id ?? '',
    contaId: contas[0]?.id ?? '',
  }
}

export default function Transacoes() {
  const [transacoes, setTransacoes] = useState<TransacaoResponse[]>([])
  const [contas, setContas] = useState<ContaResponse[]>([])
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(false)
  const [editing, setEditing] = useState<TransacaoResponse | null>(null)
  const [form, setForm] = useState<TransacaoRequest>(emptyForm([], []))
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [filtroTipo, setFiltroTipo] = useState<'todos' | 'Receita' | 'Despesa'>('todos')

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    try {
      const [t, c, cat] = await Promise.all([
        listarDoAmbiente(),
        listarContas(),
        listarCategorias(),
      ])
      setTransacoes(t.data.dados ?? [])
      setContas(c.data.dados ?? [])
      setCategorias(cat.data.dados ?? [])
    } finally {
      setLoading(false)
    }
  }

  function openNew() {
    setEditing(null)
    setForm(emptyForm(contas, categorias))
    setError('')
    setModal(true)
  }

  function openEdit(t: TransacaoResponse) {
    setEditing(t)
    setForm({
      descricao: t.descricao ?? '',
      valor: t.valor,
      data: t.data.slice(0, 16),
      observacao: t.observacao ?? '',
      tipoTransacao: t.tipoTransacao,
      categoriaId: t.categoriaId,
      contaId: t.contaId,
    })
    setError('')
    setModal(true)
  }

  async function handleSave() {
    if (!form.descricao.trim()) { setError('Descrição é obrigatória.'); return }
    if (!form.contaId) { setError('Selecione uma conta.'); return }
    if (!form.categoriaId) { setError('Selecione uma categoria.'); return }
    setSaving(true)
    setError('')
    try {
      const payload = { ...form, data: new Date(form.data).toISOString() }
      if (editing) {
        await atualizar(editing.id, payload)
      } else {
        await criar(payload)
      }
      setModal(false)
      await load()
    } catch {
      setError('Erro ao salvar transação.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Excluir esta transação?')) return
    await excluir(id)
    await load()
  }

  const filtradas = transacoes.filter((t) => {
    if (filtroTipo === 'todos') return true
    return filtroTipo === 'Receita' ? t.tipoTransacao === TipoTransacao.Receita : t.tipoTransacao === TipoTransacao.Despesa
  })

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Transações</h1>
        <Button onClick={openNew}>+ Nova Transação</Button>
      </div>

      {/* Filtros */}
      <div className="flex gap-2">
        {(['todos', 'Receita', 'Despesa'] as const).map((f) => (
          <button
            key={f}
            onClick={() => setFiltroTipo(f)}
            className={`rounded-lg px-4 py-1.5 text-sm font-medium transition ${filtroTipo === f ? 'bg-green-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'}`}
          >
            {f === 'todos' ? 'Todas' : f === 'Receita' ? '📈 Receitas' : '📉 Despesas'}
          </button>
        ))}
      </div>

      {loading ? (
        <div className="text-center py-12 text-gray-400">Carregando...</div>
      ) : filtradas.length === 0 ? (
        <Card className="p-12 text-center">
          <p className="text-4xl mb-3">💸</p>
          <p className="text-gray-500">Nenhuma transação encontrada.</p>
          <Button className="mt-4" onClick={openNew}>Registrar primeira transação</Button>
        </Card>
      ) : (
        <Card>
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b text-left text-xs font-medium uppercase tracking-wide text-gray-500">
                <th className="px-4 py-3">Descrição</th>
                <th className="px-4 py-3">Categoria</th>
                <th className="px-4 py-3">Conta</th>
                <th className="px-4 py-3">Data</th>
                <th className="px-4 py-3 text-right">Valor</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50">
              {filtradas.map((t) => (
                <tr key={t.id} className="hover:bg-gray-50 transition">
                  <td className="px-4 py-3">
                    <p className="font-medium text-gray-800">{t.descricao}</p>
                    {t.observacao && <p className="text-xs text-gray-400">{t.observacao}</p>}
                  </td>
                  <td className="px-4 py-3 text-gray-600">{t.categoriaNome ?? '-'}</td>
                  <td className="px-4 py-3 text-gray-600">{t.contaNome ?? '-'}</td>
                  <td className="px-4 py-3 text-gray-500">{formatDate(t.data)}</td>
                  <td className="px-4 py-3 text-right">
                    <Badge color={t.tipoTransacao === TipoTransacao.Receita ? '#16a34a' : '#dc2626'}>
                      {t.tipoTransacao === TipoTransacao.Receita ? '+' : '-'}{formatCurrency(t.valor)}
                    </Badge>
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex gap-1 justify-end">
                      <button onClick={() => openEdit(t)} className="rounded p-1 hover:bg-gray-100 text-gray-400 hover:text-gray-600">✏️</button>
                      <button onClick={() => handleDelete(t.id)} className="rounded p-1 hover:bg-red-50 text-gray-400 hover:text-red-600">🗑️</button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>
      )}

      <Modal open={modal} onClose={() => setModal(false)} title={editing ? 'Editar Transação' : 'Nova Transação'} size="lg">
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}
          <div className="grid grid-cols-2 gap-4">
            <Input label="Descrição" value={form.descricao} onChange={(e) => setForm((f) => ({ ...f, descricao: e.target.value }))} placeholder="Ex: Mercado, Salário..." className="col-span-2" required />
            <Select
              label="Tipo"
              value={form.tipoTransacao}
              onChange={(e) => setForm((f) => ({ ...f, tipoTransacao: Number(e.target.value) as TipoTransacao }))}
              options={tipoOptions}
            />
            <Input
              label="Valor"
              type="number"
              step="0.01"
              min="0"
              value={form.valor}
              onChange={(e) => setForm((f) => ({ ...f, valor: parseFloat(e.target.value) || 0 }))}
            />
            <Select
              label="Conta"
              value={form.contaId}
              onChange={(e) => setForm((f) => ({ ...f, contaId: e.target.value }))}
              options={contas.map((c) => ({ value: c.id, label: c.nome }))}
            />
            <Select
              label="Categoria"
              value={form.categoriaId}
              onChange={(e) => setForm((f) => ({ ...f, categoriaId: e.target.value }))}
              options={categorias.map((c) => ({ value: c.id, label: c.nome ?? '' }))}
            />
            <Input
              label="Data"
              type="datetime-local"
              value={form.data}
              onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))}
              className="col-span-2"
            />
            <Input
              label="Observação"
              value={form.observacao}
              onChange={(e) => setForm((f) => ({ ...f, observacao: e.target.value }))}
              placeholder="Opcional..."
              className="col-span-2"
            />
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModal(false)}>Cancelar</Button>
            <Button onClick={handleSave} loading={saving}>Salvar</Button>
          </div>
        </div>
      </Modal>
    </div>
  )
}
