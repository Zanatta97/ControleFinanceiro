import { useEffect, useState } from 'react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/transacao'
import { listarDoAmbiente as listarContas } from '../api/conta'
import { listarDoAmbiente as listarCategorias } from '../api/categoria'
import type { TransacaoResponse, TransacaoRequest, ContaResponse, CategoriaResponse } from '../types/api'
import { TipoTransacao } from '../types/api'
import { formatCurrency, formatDate, formatMesCompetencia } from '../utils/format'
import Button from '../components/ui/Button'
import Drawer from '../components/ui/Drawer'
import Input from '../components/ui/Input'
import Select from '../components/ui/Select'
import Card from '../components/ui/Card'
import Alert from '../components/ui/Alert'
import Badge from '../components/ui/Badge'
import CurrencyInput from '../components/ui/CurrencyInput'

const tipoOptions = [
  { value: TipoTransacao.Receita, label: 'Receita' },
  { value: TipoTransacao.Despesa, label: 'Despesa' },
]

function currentMonth() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

// "YYYY-MM" → "YYYY-MM-01" (formato DateOnly esperado pelo backend)
function monthToDateOnly(ym: string) {
  return `${ym}-01`
}

// "YYYY-MM-DD" → "YYYY-MM" (para preencher o input type="month")
function dateOnlyToMonth(iso: string | null | undefined) {
  if (!iso) return currentMonth()
  return iso.slice(0, 7)
}

function emptyForm(contas: ContaResponse[], categorias: CategoriaResponse[]): TransacaoRequest {
  return {
    descricao: '',
    valor: 0,
    data: new Date().toISOString().slice(0, 16),
    observacao: null,
    tipoTransacao: TipoTransacao.Despesa,
    categoriaId: categorias[0]?.id ?? '',
    contaId: contas[0]?.id ?? '',
    mesCompetencia: monthToDateOnly(currentMonth()),
    parcelas: 1,
  }
}

function previewParcelas(mesInicio: string, n: number): string {
  const [year, month] = mesInicio.split('-').map(Number)
  return Array.from({ length: n }, (_, i) => {
    const d = new Date(year, month - 1 + i, 1)
    return d.toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' })
      .replace('. de ', '/').replace('.', '')
  }).join(', ')
}

export default function Transacoes() {
  const [transacoes, setTransacoes] = useState<TransacaoResponse[]>([])
  const [contas, setContas] = useState<ContaResponse[]>([])
  const [categorias, setCategorias] = useState<CategoriaResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [modal, setModal] = useState(false)
  const [editing, setEditing] = useState<TransacaoResponse | null>(null)
  const [form, setForm] = useState<TransacaoRequest>(emptyForm([], []))
  const [mesInput, setMesInput] = useState(currentMonth())
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')

  // Filtros
  const [filtroTipo, setFiltroTipo] = useState<'todos' | 'Receita' | 'Despesa'>('todos')
  const [filtroMes, setFiltroMes] = useState('')
  const [filtroCategoria, setFiltroCategoria] = useState('')
  const [filtroConta, setFiltroConta] = useState('')

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    const [t, c, cat] = await Promise.allSettled([
      listarDoAmbiente(),
      listarContas(),
      listarCategorias(),
    ])
    if (t.status === 'fulfilled') setTransacoes(t.value.data.dados ?? [])
    if (c.status === 'fulfilled') setContas(c.value.data.dados ?? [])
    if (cat.status === 'fulfilled') setCategorias(cat.value.data.dados ?? [])
    setLoading(false)
  }

  function openNew() {
    setEditing(null)
    const mes = currentMonth()
    setMesInput(mes)
    setForm({ ...emptyForm(contas, categorias), mesCompetencia: monthToDateOnly(mes) })
    setError('')
    setModal(true)
  }

  function openEdit(t: TransacaoResponse) {
    setEditing(t)
    const mes = dateOnlyToMonth(t.mesCompetencia)
    setMesInput(mes)
    setForm({
      descricao: t.descricao ?? '',
      valor: t.valor,
      data: t.data.slice(0, 16),
      observacao: t.observacao ?? null,
      tipoTransacao: t.tipoTransacao,
      categoriaId: t.categoriaId,
      contaId: t.contaId,
      mesCompetencia: monthToDateOnly(mes),
      parcelas: 1,
    })
    setError('')
    setModal(true)
  }

  function handleMesChange(ym: string) {
    setMesInput(ym)
    setForm((f) => ({ ...f, mesCompetencia: monthToDateOnly(ym) }))
  }

  async function handleSave() {
    if (!form.descricao.trim()) { setError('Descrição é obrigatória.'); return }
    if (!form.contaId) { setError('Selecione uma conta.'); return }
    if (!form.categoriaId) { setError('Selecione uma categoria.'); return }
    setSaving(true)
    setError('')
    try {
      const payload = {
        ...form,
        data: new Date(form.data).toISOString(),
        observacao: form.observacao?.trim() || null,
      }
      if (editing) {
        await atualizar(editing.id, { ...payload, parcelas: 1 })
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
    if (filtroTipo !== 'todos') {
      const tipo = filtroTipo === 'Receita' ? TipoTransacao.Receita : TipoTransacao.Despesa
      if (t.tipoTransacao !== tipo) return false
    }
    if (filtroMes && t.mesCompetencia?.slice(0, 7) !== filtroMes) return false
    if (filtroCategoria && t.categoriaId !== filtroCategoria) return false
    if (filtroConta && t.contaId !== filtroConta) return false
    return true
  })

  const parcelas = form.parcelas ?? 1

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-gray-900">Transações</h1>
        <Button onClick={openNew}>+ Nova Transação</Button>
      </div>

      {/* Filtros */}
      <div className="flex flex-wrap gap-2 items-end">
        {/* Tipo */}
        <div className="flex gap-1">
          {(['todos', 'Receita', 'Despesa'] as const).map((f) => (
            <button
              key={f}
              onClick={() => setFiltroTipo(f)}
              className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${filtroTipo === f ? 'bg-green-600 text-white' : 'bg-white border border-gray-200 text-gray-600 hover:bg-gray-50'}`}
            >
              {f === 'todos' ? 'Todas' : f === 'Receita' ? '📈 Receitas' : '📉 Despesas'}
            </button>
          ))}
        </div>

        {/* Mês de competência */}
        <div className="flex flex-col gap-0.5">
          <label className="text-xs text-gray-500">Competência</label>
          <input
            type="month"
            value={filtroMes}
            onChange={(e) => setFiltroMes(e.target.value)}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          />
        </div>

        {/* Categoria */}
        <div className="flex flex-col gap-0.5">
          <label className="text-xs text-gray-500">Categoria</label>
          <select
            value={filtroCategoria}
            onChange={(e) => setFiltroCategoria(e.target.value)}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          >
            <option value="">Todas</option>
            {categorias.map((c) => (
              <option key={c.id} value={c.id}>{c.nome}</option>
            ))}
          </select>
        </div>

        {/* Conta */}
        <div className="flex flex-col gap-0.5">
          <label className="text-xs text-gray-500">Conta</label>
          <select
            value={filtroConta}
            onChange={(e) => setFiltroConta(e.target.value)}
            className="rounded-lg border border-gray-300 px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
          >
            <option value="">Todas</option>
            {contas.map((c) => (
              <option key={c.id} value={c.id}>{c.nome}</option>
            ))}
          </select>
        </div>

        {/* Limpar filtros */}
        {(filtroMes || filtroCategoria || filtroConta || filtroTipo !== 'todos') && (
          <button
            onClick={() => { setFiltroTipo('todos'); setFiltroMes(''); setFiltroCategoria(''); setFiltroConta('') }}
            className="rounded-lg border border-gray-200 px-3 py-1.5 text-sm text-gray-500 hover:bg-gray-50 transition"
          >
            Limpar
          </button>
        )}
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
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-xs font-medium uppercase tracking-wide text-gray-500">
                  <th className="px-4 py-3">Descrição</th>
                  <th className="px-4 py-3">Categoria</th>
                  <th className="px-4 py-3">Conta</th>
                  <th className="px-4 py-3">Competência</th>
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
                    <td className="px-4 py-3">
                      <span className="rounded-full bg-blue-50 px-2 py-0.5 text-xs font-medium text-blue-700">
                        {formatMesCompetencia(t.mesCompetencia)}
                      </span>
                    </td>
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
          </div>
        </Card>
      )}

      <Drawer open={modal} onClose={() => setModal(false)} title={editing ? 'Editar Transação' : 'Nova Transação'}>
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}

          <Input label="Descrição" value={form.descricao} onChange={(e) => setForm((f) => ({ ...f, descricao: e.target.value }))} placeholder="Ex: Mercado, Salário..." required />
          <Select
            label="Tipo"
            value={form.tipoTransacao}
            onChange={(e) => setForm((f) => ({ ...f, tipoTransacao: Number(e.target.value) as TipoTransacao }))}
            options={tipoOptions}
          />
          <CurrencyInput
            label="Valor (R$)"
            value={form.valor}
            onChange={(v) => setForm((f) => ({ ...f, valor: v }))}
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
            label="Data da compra"
            type="datetime-local"
            value={form.data}
            onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))}
          />

          {/* Mês de competência */}
          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-gray-700">Mês de competência</label>
            <input
              type="month"
              value={mesInput}
              onChange={(e) => handleMesChange(e.target.value)}
              className="rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-green-500"
            />
            <p className="text-xs text-gray-400">Mês ao qual esta transação será atribuída financeiramente.</p>
          </div>

          {/* Parcelas — apenas no cadastro */}
          {!editing && (
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-gray-700">Número de parcelas</label>
              <input
                type="number"
                min={1}
                value={parcelas}
                onChange={(e) => setForm((f) => ({ ...f, parcelas: Math.max(1, parseInt(e.target.value) || 1) }))}
                className="rounded-lg border border-gray-300 px-3 py-2 text-sm shadow-sm focus:outline-none focus:ring-2 focus:ring-green-500"
              />
              {parcelas > 1 && (
                <div className="rounded-lg bg-blue-50 border border-blue-200 px-3 py-2 text-xs text-blue-700">
                  Serão criadas <strong>{parcelas} transações</strong>:{' '}
                  {previewParcelas(mesInput, parcelas)}
                </div>
              )}
            </div>
          )}

          <Input
            label="Observação"
            value={form.observacao ?? ''}
            onChange={(e) => setForm((f) => ({ ...f, observacao: e.target.value || null }))}
            placeholder={parcelas > 1 ? 'Opcional — será adicionada após o número da parcela' : 'Opcional...'}
          />

          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModal(false)}>Cancelar</Button>
            <Button onClick={handleSave} loading={saving}>
              {parcelas > 1 ? `Criar ${parcelas} parcelas` : 'Salvar'}
            </Button>
          </div>
        </div>
      </Drawer>
    </div>
  )
}
