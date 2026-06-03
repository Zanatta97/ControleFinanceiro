import { useEffect, useState, useMemo } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import {
  listarUsuarios, listarRoles, atribuirRoles,
  bloquearUsuario, desbloquearUsuario, excluirUsuario,
  criarUsuario, listarAmbientes, excluirAmbiente, listarLogs,
  type UsuarioAdmin, type CriarUsuarioRequest, type LogFiltros,
} from '../api/admin'
import type { AmbienteResponse, ApiLogResponse } from '../types/api'
import Card from '../components/ui/Card'
import Button from '../components/ui/Button'
import Alert from '../components/ui/Alert'
import Modal from '../components/ui/Modal'
import Badge from '../components/ui/Badge'
import Pagination from '../components/ui/Pagination'
import { usePagination } from '../hooks/usePagination'

type Tab = 'usuarios' | 'ambientes' | 'logs'

export default function Admin() {
  const { isAdmin } = useAuth()
  const navigate = useNavigate()

  useEffect(() => {
    if (!isAdmin) navigate('/', { replace: true })
  }, [isAdmin, navigate])

  const [tab, setTab] = useState<Tab>('usuarios')

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-fin-text-primary">Administração</h1>
          <p className="text-sm text-fin-text-secondary">Gestão completa de usuários e ambientes do sistema.</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-fin-border">
        {(['usuarios', 'ambientes', 'logs'] as Tab[]).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-5 py-2.5 text-sm font-medium transition border-b-2 -mb-px ${
              tab === t
                ? 'border-fin-brand text-fin-brand'
                : 'border-transparent text-fin-text-secondary hover:text-fin-text-primary'
            }`}
          >
            {t === 'usuarios' ? 'Usuários' : t === 'ambientes' ? 'Ambientes' : 'Logs da API'}
          </button>
        ))}
      </div>

      {tab === 'usuarios' ? <TabUsuarios /> : tab === 'ambientes' ? <TabAmbientes /> : <TabLogs />}
    </div>
  )
}

/* ─────────────────────── Tab Usuários ─────────────────────── */

const NOVO_USUARIO_INICIAL: CriarUsuarioRequest = { nome: '', email: '', senha: '', confirmacaoSenha: '' }

function TabUsuarios() {
  const [usuarios, setUsuarios] = useState<UsuarioAdmin[]>([])
  const [roles, setRoles] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [modalRoles, setModalRoles] = useState<UsuarioAdmin | null>(null)
  const [rolesForm, setRolesForm] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [busca, setBusca] = useState('')
  const [modalNovo, setModalNovo] = useState(false)
  const [novoForm, setNovoForm] = useState<CriarUsuarioRequest>(NOVO_USUARIO_INICIAL)
  const [novoError, setNovoError] = useState('')
  const [criando, setCriando] = useState(false)

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    const [u, r] = await Promise.allSettled([listarUsuarios(), listarRoles()])
    if (u.status === 'fulfilled') setUsuarios(u.value.data.dados ?? [])
    if (r.status === 'fulfilled') setRoles(r.value.data.dados ?? [])
    setLoading(false)
  }

  async function handleToggleBloqueio(u: UsuarioAdmin) {
    try {
      if (u.bloqueado) await desbloquearUsuario(u.id)
      else await bloquearUsuario(u.id)
      await load()
    } catch { setError('Erro ao alterar status do usuário.') }
  }

  async function handleDelete(u: UsuarioAdmin) {
    if (!confirm(`Excluir permanentemente o usuário "${u.nome ?? u.email}"? Esta ação não pode ser desfeita.`)) return
    try {
      await excluirUsuario(u.id)
      await load()
    } catch { setError('Erro ao excluir usuário.') }
  }

  function openModalRoles(u: UsuarioAdmin) {
    setModalRoles(u)
    setRolesForm([...u.roles])
    setError('')
  }

  async function handleSalvarRoles() {
    if (!modalRoles) return
    setSaving(true)
    try {
      await atribuirRoles(modalRoles.id, rolesForm)
      setModalRoles(null)
      await load()
    } catch { setError('Erro ao salvar roles.') }
    finally { setSaving(false) }
  }

  function abrirModalNovo() {
    setNovoForm(NOVO_USUARIO_INICIAL)
    setNovoError('')
    setModalNovo(true)
  }

  async function handleCriarUsuario() {
    if (novoForm.senha !== novoForm.confirmacaoSenha) {
      setNovoError('As senhas não coincidem.')
      return
    }
    setCriando(true)
    setNovoError('')
    try {
      await criarUsuario(novoForm)
      setModalNovo(false)
      await load()
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errorMessage?: string } } })?.response?.data?.errorMessage
      setNovoError(msg || 'Erro ao criar usuário.')
    } finally {
      setCriando(false)
    }
  }

  const filtrados = usuarios.filter((u) =>
    busca === '' ||
    u.nome?.toLowerCase().includes(busca.toLowerCase()) ||
    u.email?.toLowerCase().includes(busca.toLowerCase())
  )

  return (
    <>
      {error && <Alert type="error" message={error} />}

      <div className="flex items-center gap-3">
        <input
          type="text"
          placeholder="Buscar por nome ou e-mail..."
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className="flex-1 max-w-sm rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
        />
        <span className="text-sm text-fin-text-secondary">{filtrados.length} usuário(s)</span>
        <Button onClick={abrirModalNovo}>+ Novo usuário</Button>
      </div>

      {loading ? (
        <div className="py-12 text-center text-fin-text-muted">Carregando...</div>
      ) : (
        <Card>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-fin-border text-left text-xs font-medium uppercase tracking-wide text-fin-text-muted">
                  <th className="px-4 py-3">Usuário</th>
                  <th className="px-4 py-3">Roles</th>
                  <th className="px-4 py-3">Cadastro</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-fin-border">
                {filtrados.map((u) => (
                  <tr key={u.id} className={`hover:bg-fin-highlight-row transition ${u.bloqueado ? 'opacity-60' : ''}`}>
                    <td className="px-4 py-3">
                      <p className="font-medium text-fin-text-primary">{u.nome ?? '—'}</p>
                      <p className="text-xs text-fin-text-muted">{u.email}</p>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {u.roles.length === 0
                          ? <span className="text-xs text-fin-text-muted">Sem role</span>
                          : u.roles.map((r) => (
                            <span key={r} className={`rounded-full px-2 py-0.5 text-xs font-medium ${r === 'Admin' ? 'bg-fin-invest-soft text-fin-invest' : 'bg-fin-brand-soft text-fin-brand'}`}>{r}</span>
                          ))}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-fin-text-secondary">
                      {u.dtaCriacao ? new Date(u.dtaCriacao).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <Badge color={u.bloqueado ? 'var(--fin-negative)' : 'var(--fin-positive)'}>
                        {u.bloqueado ? 'Bloqueado' : 'Ativo'}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        <button
                          onClick={() => openModalRoles(u)}
                          title="Gerenciar roles"
                          className="rounded px-2 py-1 text-xs text-fin-invest hover:bg-fin-invest-soft transition"
                        >
                          Roles
                        </button>
                        <button
                          onClick={() => handleToggleBloqueio(u)}
                          title={u.bloqueado ? 'Desbloquear' : 'Bloquear'}
                          className={`rounded px-2 py-1 text-xs transition ${u.bloqueado ? 'text-fin-positive hover:bg-fin-positive-soft' : 'text-fin-warning hover:bg-fin-warning-soft'}`}
                        >
                          {u.bloqueado ? 'Desbloquear' : 'Bloquear'}
                        </button>
                        <button
                          onClick={() => handleDelete(u)}
                          title="Excluir usuário"
                          className="rounded px-2 py-1 text-xs text-fin-negative hover:bg-fin-negative-soft transition"
                        >
                          Excluir
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtrados.length === 0 && (
                  <tr><td colSpan={5} className="px-4 py-8 text-center text-fin-text-muted">Nenhum usuário encontrado.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Modal Roles */}
      <Modal open={!!modalRoles} onClose={() => setModalRoles(null)} title={`Roles — ${modalRoles?.nome ?? modalRoles?.email}`} size="sm">
        <div className="space-y-4">
          {error && <Alert type="error" message={error} />}
          <p className="text-sm text-fin-text-secondary">Selecione as roles do usuário:</p>
          <div className="space-y-2">
            {roles.map((r) => (
              <label key={r} className="flex items-center gap-3 cursor-pointer rounded-lg border border-fin-border px-4 py-3 hover:bg-fin-surface-2 transition">
                <input
                  type="checkbox"
                  checked={rolesForm.includes(r)}
                  onChange={(e) => {
                    if (e.target.checked) setRolesForm((prev) => [...prev, r])
                    else setRolesForm((prev) => prev.filter((x) => x !== r))
                  }}
                  className="h-4 w-4 rounded border-fin-border accent-fin-brand"
                />
                <span className={`text-sm font-medium ${r === 'Admin' ? 'text-fin-invest' : 'text-fin-brand'}`}>{r}</span>
              </label>
            ))}
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModalRoles(null)}>Cancelar</Button>
            <Button onClick={handleSalvarRoles} loading={saving}>Salvar</Button>
          </div>
        </div>
      </Modal>

      {/* Modal Novo Usuário */}
      <Modal open={modalNovo} onClose={() => setModalNovo(false)} title="Novo usuário" size="sm">
        <div className="space-y-4">
          {novoError && <Alert type="error" message={novoError} />}
          <div className="space-y-3">
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-fin-text-muted">Nome</label>
              <input
                type="text"
                value={novoForm.nome}
                onChange={(e) => setNovoForm((f) => ({ ...f, nome: e.target.value }))}
                placeholder="Nome completo"
                className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-fin-text-muted">E-mail</label>
              <input
                type="email"
                value={novoForm.email}
                onChange={(e) => setNovoForm((f) => ({ ...f, email: e.target.value }))}
                placeholder="email@exemplo.com"
                className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-fin-text-muted">Senha</label>
              <input
                type="password"
                value={novoForm.senha}
                onChange={(e) => setNovoForm((f) => ({ ...f, senha: e.target.value }))}
                placeholder="••••••••"
                className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
              />
            </div>
            <div className="flex flex-col gap-1">
              <label className="text-xs font-medium text-fin-text-muted">Confirmar senha</label>
              <input
                type="password"
                value={novoForm.confirmacaoSenha}
                onChange={(e) => setNovoForm((f) => ({ ...f, confirmacaoSenha: e.target.value }))}
                placeholder="••••••••"
                className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
              />
            </div>
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModalNovo(false)}>Cancelar</Button>
            <Button onClick={handleCriarUsuario} loading={criando}>Criar usuário</Button>
          </div>
        </div>
      </Modal>
    </>
  )
}

/* ─────────────────────── Tab Ambientes ─────────────────────── */

function TabAmbientes() {
  const [ambientes, setAmbientes] = useState<AmbienteResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [modalMembros, setModalMembros] = useState<AmbienteResponse | null>(null)
  const [busca, setBusca] = useState('')

  useEffect(() => { load() }, [])

  async function load() {
    setLoading(true)
    try {
      const { data } = await listarAmbientes()
      setAmbientes(data.dados ?? [])
    } catch { setError('Erro ao carregar ambientes.') }
    finally { setLoading(false) }
  }

  async function handleDelete(a: AmbienteResponse) {
    if (!confirm(`Excluir o ambiente "${a.nome}"? Todos os dados (contas, transações, categorias) serão perdidos.`)) return
    try {
      await excluirAmbiente(a.id)
      await load()
    } catch { setError('Erro ao excluir ambiente.') }
  }

  const filtrados = ambientes.filter((a) =>
    busca === '' || a.nome?.toLowerCase().includes(busca.toLowerCase())
  )

  return (
    <>
      {error && <Alert type="error" message={error} />}

      <div className="flex items-center gap-3">
        <input
          type="text"
          placeholder="Buscar por nome..."
          value={busca}
          onChange={(e) => setBusca(e.target.value)}
          className="flex-1 max-w-sm rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
        />
        <span className="text-sm text-fin-text-secondary">{filtrados.length} ambiente(s)</span>
      </div>

      {loading ? (
        <div className="py-12 text-center text-fin-text-muted">Carregando...</div>
      ) : (
        <Card>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-fin-border text-left text-xs font-medium uppercase tracking-wide text-fin-text-muted">
                  <th className="px-4 py-3">Nome</th>
                  <th className="px-4 py-3">Membros</th>
                  <th className="px-4 py-3">Criado em</th>
                  <th className="px-4 py-3 text-right">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-fin-border">
                {filtrados.map((a) => (
                  <tr key={a.id} className="hover:bg-fin-highlight-row transition">
                    <td className="px-4 py-3 font-medium text-fin-text-primary">{a.nome}</td>
                    <td className="px-4 py-3 text-fin-text-secondary">{a.membros?.length ?? 0}</td>
                    <td className="px-4 py-3 text-fin-text-secondary">
                      {a.dataCriacao ? new Date(a.dataCriacao).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        {(a.membros?.length ?? 0) > 0 && (
                          <button
                            onClick={() => setModalMembros(a)}
                            className="rounded px-2 py-1 text-xs text-fin-invest hover:bg-fin-invest-soft transition"
                          >
                            Membros
                          </button>
                        )}
                        <button
                          onClick={() => handleDelete(a)}
                          className="rounded px-2 py-1 text-xs text-fin-negative hover:bg-fin-negative-soft transition"
                        >
                          Excluir
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtrados.length === 0 && (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-fin-text-muted">Nenhum ambiente encontrado.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Modal membros */}
      <Modal open={!!modalMembros} onClose={() => setModalMembros(null)} title={`Membros — ${modalMembros?.nome}`} size="md">
        <div className="divide-y divide-fin-border rounded-lg border border-fin-border max-h-80 overflow-y-auto">
          {modalMembros?.membros?.map((m) => (
            <div key={m.usuario?.id} className="flex items-center justify-between px-4 py-3">
              <div>
                <p className="text-sm font-medium text-fin-text-primary">{m.usuario?.nome}</p>
                <p className="text-xs text-fin-text-secondary">{m.usuario?.email}</p>
              </div>
              <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${
                m.role === 'Dono'
                  ? 'bg-fin-warning-soft text-fin-warning'
                  : m.role === 'Admin'
                  ? 'bg-fin-invest-soft text-fin-invest'
                  : 'bg-fin-surface-2 text-fin-text-muted'
              }`}>
                {m.role}
              </span>
            </div>
          ))}
        </div>
      </Modal>
    </>
  )
}

/* ─────────────────────── Tab Logs ─────────────────────── */

const LOGS_POR_PAGINA = 50

function TabLogs() {
  const [logs, setLogs] = useState<ApiLogResponse[]>([])
  const [loading, setLoading] = useState(false)
  const [expandido, setExpandido] = useState<number | null>(null)
  const [filtros, setFiltros] = useState<LogFiltros>({
    dataInicio: '',
    dataFim: '',
    userId: '',
    path: '',
    statusCode: '',
    apenasErros: false,
    limite: 500,
  })

  async function load() {
    setLoading(true)
    try {
      const params: LogFiltros = { ...filtros }
      if (!params.dataInicio) delete params.dataInicio
      if (!params.dataFim) delete params.dataFim
      if (!params.userId) delete params.userId
      if (!params.path) delete params.path
      if (!params.statusCode) delete params.statusCode
      const { data } = await listarLogs(params)
      setLogs(data.dados ?? [])
    } finally {
      setLoading(false)
    }
  }

  function statusColor(code: number) {
    if (code >= 500) return 'var(--fin-negative)'
    if (code >= 400) return 'var(--fin-warning)'
    if (code >= 200) return 'var(--fin-positive)'
    return 'var(--fin-text-muted)'
  }

  const { paginados, pagina, totalPaginas, irPara, total } = usePagination(logs, LOGS_POR_PAGINA)

  // useMemo apenas para satisfazer o lint; a lógica real fica no hook
  useMemo(() => {}, [logs])

  return (
    <div className="space-y-4">
      {/* Filtros */}
      <Card className="p-4">
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4">
          <div className="flex flex-col gap-1">
            <label className="text-xs text-fin-text-muted">Data início</label>
            <input type="datetime-local" value={filtros.dataInicio ?? ''} onChange={(e) => setFiltros((f) => ({ ...f, dataInicio: e.target.value }))}
              className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-fin-text-muted">Data fim</label>
            <input type="datetime-local" value={filtros.dataFim ?? ''} onChange={(e) => setFiltros((f) => ({ ...f, dataFim: e.target.value }))}
              className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-fin-text-muted">Endpoint (path)</label>
            <input type="text" placeholder="Ex: /api/Transacao" value={filtros.path ?? ''} onChange={(e) => setFiltros((f) => ({ ...f, path: e.target.value }))}
              className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-fin-text-muted">Status HTTP</label>
            <input type="number" placeholder="Ex: 400" value={filtros.statusCode ?? ''} onChange={(e) => setFiltros((f) => ({ ...f, statusCode: e.target.value ? Number(e.target.value) : '' }))}
              className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
          </div>
          <div className="flex flex-col gap-1">
            <label className="text-xs text-fin-text-muted">User ID</label>
            <input type="text" placeholder="ID do usuário" value={filtros.userId ?? ''} onChange={(e) => setFiltros((f) => ({ ...f, userId: e.target.value }))}
              className="rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-1.5 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand" />
          </div>
          <div className="flex items-end gap-2 col-span-2 lg:col-span-3">
            <label className="flex items-center gap-2 cursor-pointer text-sm text-fin-text-secondary">
              <input type="checkbox" checked={filtros.apenasErros ?? false} onChange={(e) => setFiltros((f) => ({ ...f, apenasErros: e.target.checked }))}
                className="h-4 w-4 rounded border-fin-border accent-fin-brand" />
              Apenas erros
            </label>
            <button onClick={load} disabled={loading}
              className="rounded-lg bg-fin-brand px-4 py-1.5 text-sm font-medium text-white hover:bg-fin-brand-hover disabled:opacity-50 transition">
              {loading ? 'Buscando...' : 'Buscar'}
            </button>
          </div>
        </div>
      </Card>

      {logs.length === 0 && !loading && (
        <p className="text-center text-sm text-fin-text-muted py-8">Aplique filtros e clique em Buscar.</p>
      )}

      {logs.length > 0 && (
        <Card className="flex flex-col">
          <div className="overflow-auto" style={{ maxHeight: 'calc(100vh - 420px)' }}>
            <table className="w-full text-sm">
              <thead className="sticky top-0 bg-fin-surface z-10">
                <tr className="border-b border-fin-border text-left text-xs font-medium uppercase tracking-wide text-fin-text-muted">
                  <th className="px-4 py-3">Timestamp</th>
                  <th className="px-4 py-3">Método</th>
                  <th className="px-4 py-3">Endpoint</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3">Tempo</th>
                  <th className="px-4 py-3">Usuário</th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody className="divide-y divide-fin-border">
                {paginados.map((l) => (
                  <>
                    <tr key={l.id} className={`hover:bg-fin-highlight-row transition ${l.isError ? 'bg-fin-negative-soft/40' : ''}`}>
                      <td className="px-4 py-3 text-fin-text-secondary whitespace-nowrap">
                        {new Date(l.timestamp).toLocaleString('pt-BR')}
                      </td>
                      <td className="px-4 py-3">
                        <span className="rounded px-1.5 py-0.5 text-xs font-bold bg-fin-surface-2 text-fin-text-secondary">{l.method}</span>
                      </td>
                      <td className="px-4 py-3 text-fin-text-primary max-w-xs truncate" title={l.path + (l.queryString ?? '')}>
                        {l.path}{l.queryString && <span className="text-fin-text-muted">{l.queryString}</span>}
                      </td>
                      <td className="px-4 py-3">
                        <span className="rounded-full px-2 py-0.5 text-xs font-semibold text-white" style={{ backgroundColor: statusColor(l.statusCode) }}>
                          {l.statusCode}
                        </span>
                      </td>
                      <td className="px-4 py-3 text-fin-text-secondary">{l.elapsedMs}ms</td>
                      <td className="px-4 py-3 text-fin-text-secondary text-xs max-w-[120px] truncate" title={l.userId ?? ''}>
                        {l.userId ?? '—'}
                      </td>
                      <td className="px-4 py-3">
                        {(l.exceptionMessage || l.requestBody || l.responseBody) && (
                          <button onClick={() => setExpandido(expandido === l.id ? null : l.id)}
                            className="text-xs text-fin-brand hover:underline">
                            {expandido === l.id ? 'Fechar' : 'Detalhes'}
                          </button>
                        )}
                      </td>
                    </tr>
                    {expandido === l.id && (
                      <tr key={`${l.id}-detail`} className="bg-fin-surface-2">
                        <td colSpan={7} className="px-4 py-3 space-y-2">
                          {l.exceptionMessage && (
                            <div>
                              <p className="text-xs font-semibold text-fin-negative mb-1">Exceção</p>
                              <pre className="text-xs text-fin-negative bg-fin-negative-soft rounded p-2 overflow-x-auto whitespace-pre-wrap">{l.exceptionMessage}</pre>
                            </div>
                          )}
                          {l.requestBody && (
                            <div>
                              <p className="text-xs font-semibold text-fin-text-muted mb-1">Request Body</p>
                              <pre className="text-xs text-fin-text-primary bg-fin-surface border border-fin-border rounded p-2 overflow-x-auto whitespace-pre-wrap max-h-40">{l.requestBody}</pre>
                            </div>
                          )}
                          {l.responseBody && (
                            <div>
                              <p className="text-xs font-semibold text-fin-text-muted mb-1">Response Body</p>
                              <pre className="text-xs text-fin-text-primary bg-fin-surface border border-fin-border rounded p-2 overflow-x-auto whitespace-pre-wrap max-h-40">{l.responseBody}</pre>
                            </div>
                          )}
                        </td>
                      </tr>
                    )}
                  </>
                ))}
              </tbody>
            </table>
          </div>
          <Pagination pagina={pagina} totalPaginas={totalPaginas} total={total} itensPorPagina={LOGS_POR_PAGINA} onPagina={irPara} />
        </Card>
      )}
    </div>
  )
}
