import { useEffect, useState, useMemo } from 'react'
import { listarDoAmbiente, criar, atualizar, excluir } from '../api/transacao'
import { listarDoAmbiente as listarContas } from '../api/conta'
import { listarDoAmbiente as listarCategorias } from '../api/categoria'
import type { TransacaoResponse, TransacaoRequest, ContaResponse, CategoriaResponse } from '../types/api'
import { TipoTransacao } from '../types/api'
import { formatCurrency, formatDate, formatMesCompetencia } from '../utils/format'
import type { AxiosError } from 'axios'
import Button from '../components/ui/Button'
import Drawer from '../components/ui/Drawer'
import Input from '../components/ui/Input'
import Select from '../components/ui/Select'
import Card from '../components/ui/Card'
import Toast from '../components/ui/Toast'
import Badge from '../components/ui/Badge'
import CurrencyInput from '../components/ui/CurrencyInput'
import Pagination from '../components/ui/Pagination'
import { usePagination } from '../hooks/usePagination'

const tipoOptions = [
  { value: TipoTransacao.Receita, label: 'Receita' },
  { value: TipoTransacao.Despesa, label: 'Despesa' },
]

type SortField = 'descricao' | 'categoriaNome' | 'contaNome' | 'mesCompetencia' | 'data' | 'valor'
type SortDir = 'asc' | 'desc'

function SortIcon({ active, dir }: { active: boolean; dir: SortDir }) {
  if (!active) return <span className="ml-1 text-fin-text-muted">↕</span>
  return <span className="ml-1">{dir === 'asc' ? '↑' : '↓'}</span>
}

function currentMonth() {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

function monthToDateOnly(ym: string) { return `${ym}-01` }

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

const ITENS_POR_PAGINA = 20

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

  // Ordenação
  const [sortField, setSortField] = useState<SortField>('data')
  const [sortDir, setSortDir] = useState<SortDir>('desc')

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

  function handleSort(field: SortField) {
    if (sortField === field) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'))
    else { setSortField(field); setSortDir('asc') }
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
    } catch (err) {
      const msg = (err as AxiosError<{ errorMessage?: string }>).response?.data?.errorMessage
      setError(msg || 'Erro ao salvar transação.')
    } finally {
      setSaving(false)
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Excluir esta transação?')) return
    await excluir(id)
    await load()
  }

  const filtradas = useMemo(() => {
    let list = transacoes.filter((t) => {
      if (filtroTipo !== 'todos') {
        const tipo = filtroTipo === 'Receita' ? TipoTransacao.Receita : TipoTransacao.Despesa
        if (t.tipoTransacao !== tipo) return false
      }
      if (filtroMes && t.mesCompetencia?.slice(0, 7) !== filtroMes) return false
      if (filtroCategoria && t.categoriaId !== filtroCategoria) return false
      if (filtroConta && t.contaId !== filtroConta) return false
      return true
    })

    list = [...list].sort((a, b) => {
      let va: string | number = ''
      let vb: string | number = ''
      if (sortField === 'descricao') { va = a.descricao ?? ''; vb = b.descricao ?? '' }
      else if (sortField === 'categoriaNome') { va = a.categoriaNome ?? ''; vb = b.categoriaNome ?? '' }
      else if (sortField === 'contaNome') { va = a.contaNome ?? ''; vb = b.contaNome ?? '' }
      else if (sortField === 'mesCompetencia') { va = a.mesCompetencia ?? ''; vb = b.mesCompetencia ?? '' }
      else if (sortField === 'data') { va = a.data; vb = b.data }
      else if (sortField === 'valor') { va = a.valor; vb = b.valor }
      if (va < vb) return sortDir === 'asc' ? -1 : 1
      if (va > vb) return sortDir === 'asc' ? 1 : -1
      return 0
    })

    return list
  }, [transacoes, filtroTipo, filtroMes, filtroCategoria, filtroConta, sortField, sortDir])

  const { paginados, pagina, totalPaginas, irPara, total } = usePagination(filtradas, ITENS_POR_PAGINA)
  const parcelas = form.parcelas ?? 1

  function ThSort({ field, label, className = '' }: { field: SortField; label: string; className?: string }) {
    return (
      <th
        className={`px-4 py-3 cursor-pointer select-none hover:text-fin-text-primary ${className}`}
        onClick={() => handleSort(field)}
      >
        {label}
        <SortIcon active={sortField === field} dir={sortDir} />
      </th>
    )
  }

  return (
    <div className="flex flex-col h-full gap-4">
      {error && <Toast message={error} onClose={() => setError('')} />}

      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold text-fin-text-primary">Transações</h1>
        <Button onClick={openNew}>+ Nova Transação</Button>
      </div>

      {/* Filtros */}
      <div className="flex flex-wrap gap-2 items-end">
        <div className="flex gap-1">
          {(['todos', 'Receita', 'Despesa'] as const).map((f) => (
            <button key={f} onClick={() => setFiltroTipo(f)}
              className={`rounded-lg px-3 py-1.5 text-sm font-medium transition ${filtroTipo === f ? 'bg-fin-brand text-white' : 'bg-fin-surface border border-fin-border text-fin-text-secondary hover:bg-fin-surface-2'}`}>
              {f === 'todos' ? 'Todas' : f === 'Receita' ? '📈 Receitas' : '📉 Despesas'}
            </button>
          ))}
        </div>
        <div className="flex flex-col gap-0.5">
          <label className="text-xs text-fin-text-muted">Competência</label>
          <input type="month" value={filtroMes} onChange={(e) => setFiltroMes(e.target.value)}
            className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
        </div>
        <div className="flex flex-col gap-0.5">
          <label className="text-xs text-fin-text-muted">Categoria</label>
          <select value={filtroCategoria} onChange={(e) => setFiltroCategoria(e.target.value)}
            className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand">
            <option value="">Todas</option>
            {categorias.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
        <div className="flex flex-col gap-0.5">
          <label className="text-xs text-fin-text-muted">Conta</label>
          <select value={filtroConta} onChange={(e) => setFiltroConta(e.target.value)}
            className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand">
            <option value="">Todas</option>
            {contas.map((c) => <option key={c.id} value={c.id}>{c.nome}</option>)}
          </select>
        </div>
        {(filtroMes || filtroCategoria || filtroConta || filtroTipo !== 'todos') && (
          <button onClick={() => { setFiltroTipo('todos'); setFiltroMes(''); setFiltroCategoria(''); setFiltroConta('') }}
            className="rounded-lg border border-fin-border px-3 py-1.5 text-sm text-fin-text-muted hover:bg-fin-surface-2 transition">
            Limpar
          </button>
        )}
      </div>

      {loading ? (
        <div className="text-center py-12 text-fin-text-muted">Carregando...</div>
      ) : filtradas.length === 0 ? (
        <Card className="p-12 text-center">
          <p className="text-4xl mb-3">💸</p>
          <p className="text-fin-text-secondary">Nenhuma transação encontrada.</p>
          <Button className="mt-4" onClick={openNew}>Registrar primeira transação</Button>
        </Card>
      ) : (
        <Card className="flex flex-col min-h-0">
          <div className="overflow-auto flex-1" style={{ maxHeight: 'calc(100vh - 320px)' }}>
            <table className="w-full text-sm">
              <thead className="sticky top-0 bg-fin-surface z-10">
                <tr className="border-b border-fin-border text-left text-xs font-medium uppercase tracking-wide text-fin-text-muted">
                  <ThSort field="descricao" label="Descrição" />
                  <ThSort field="categoriaNome" label="Categoria" />
                  <ThSort field="contaNome" label="Conta" />
                  <ThSort field="mesCompetencia" label="Competência" />
                  <ThSort field="data" label="Data" />
                  <ThSort field="valor" label="Valor" className="text-right" />
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody className="divide-y divide-fin-border">
                {paginados.map((t) => (
                  <tr key={t.id} className="hover:bg-fin-highlight-row transition">
                    <td className="px-4 py-3">
                      <p className="font-medium text-fin-text-primary">{t.descricao || '—'}</p>
                      {t.observacao && <p className="text-xs text-fin-text-muted">{t.observacao}</p>}
                    </td>
                    <td className="px-4 py-3 text-fin-text-secondary">{t.categoriaNome || '—'}</td>
                    <td className="px-4 py-3 text-fin-text-secondary">{t.contaNome || '—'}</td>
                    <td className="px-4 py-3">
                      <span className="rounded-full bg-fin-brand-soft px-2 py-0.5 text-xs font-medium text-fin-brand">
                        {formatMesCompetencia(t.mesCompetencia)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-fin-text-muted">{formatDate(t.data)}</td>
                    <td className="px-4 py-3 text-right">
                      <Badge color={t.tipoTransacao === TipoTransacao.Receita ? 'var(--fin-positive)' : 'var(--fin-negative)'}>
                        {`${t.tipoTransacao === TipoTransacao.Receita ? '+' : '-'}${formatCurrency(t.valor)}`}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1 justify-end">
                        <button onClick={() => openEdit(t)} className="rounded p-1 hover:bg-fin-surface-2 text-fin-text-muted hover:text-fin-text-primary">✏️</button>
                        <button onClick={() => handleDelete(t.id)} className="rounded p-1 hover:bg-fin-negative-soft text-fin-text-muted hover:text-fin-negative">🗑️</button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination pagina={pagina} totalPaginas={totalPaginas} total={total} itensPorPagina={ITENS_POR_PAGINA} onPagina={irPara} />
        </Card>
      )}

      <Drawer open={modal} onClose={() => setModal(false)} title={editing ? 'Editar Transação' : 'Nova Transação'}>
        <div className="space-y-4">
          <Input label="Descrição" value={form.descricao} onChange={(e) => setForm((f) => ({ ...f, descricao: e.target.value }))} placeholder="Ex: Mercado, Salário..." required />
          <Select label="Tipo" value={form.tipoTransacao}
            onChange={(e) => setForm((f) => ({ ...f, tipoTransacao: Number(e.target.value) as TipoTransacao }))}
            options={tipoOptions} />
          <CurrencyInput label="Valor (R$)" value={form.valor} onChange={(v) => setForm((f) => ({ ...f, valor: v }))} />
          <Select label="Conta" value={form.contaId}
            onChange={(e) => setForm((f) => ({ ...f, contaId: e.target.value }))}
            options={contas.map((c) => ({ value: c.id, label: c.nome }))} />
          <Select label="Categoria" value={form.categoriaId}
            onChange={(e) => setForm((f) => ({ ...f, categoriaId: e.target.value }))}
            options={categorias.map((c) => ({ value: c.id, label: c.nome ?? '' }))} />
          <Input label="Data da compra" type="datetime-local" value={form.data}
            onChange={(e) => setForm((f) => ({ ...f, data: e.target.value }))} />

          <div className="flex flex-col gap-1">
            <label className="text-sm font-medium text-fin-text-primary">Mês de competência</label>
            <input type="month" value={mesInput} onChange={(e) => handleMesChange(e.target.value)}
              className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-2 text-sm shadow-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
            <p className="text-xs text-fin-text-muted">Mês ao qual esta transação será atribuída financeiramente.</p>
          </div>

          {!editing && (
            <div className="flex flex-col gap-1">
              <label className="text-sm font-medium text-fin-text-primary">Número de parcelas</label>
              <input type="number" min={1} value={parcelas}
                onChange={(e) => setForm((f) => ({ ...f, parcelas: Math.max(1, parseInt(e.target.value) || 1) }))}
                className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-2 text-sm shadow-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
              {parcelas > 1 && (
                <div className="rounded-lg bg-fin-brand-soft border border-fin-brand px-3 py-2 text-xs text-fin-brand">
                  Serão criadas <strong>{parcelas} transações</strong>: {previewParcelas(mesInput, parcelas)}
                </div>
              )}
            </div>
          )}

          <Input label="Observação" value={form.observacao ?? ''}
            onChange={(e) => setForm((f) => ({ ...f, observacao: e.target.value || null }))}
            placeholder={parcelas > 1 ? 'Opcional — será adicionada após o número da parcela' : 'Opcional...'} />

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
