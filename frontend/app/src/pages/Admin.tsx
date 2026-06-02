import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import {
  listarUsuarios, listarRoles, atribuirRoles,
  bloquearUsuario, desbloquearUsuario, excluirUsuario,
  listarAmbientes, excluirAmbiente,
  type UsuarioAdmin,
} from '../api/admin'
import type { AmbienteResponse } from '../types/api'
import Card from '../components/ui/Card'
import Button from '../components/ui/Button'
import Alert from '../components/ui/Alert'
import Modal from '../components/ui/Modal'
import Badge from '../components/ui/Badge'

type Tab = 'usuarios' | 'ambientes'

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
          <h1 className="text-2xl font-bold text-gray-900">Administração</h1>
          <p className="text-sm text-gray-500">Gestão completa de usuários e ambientes do sistema.</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-gray-200">
        {(['usuarios', 'ambientes'] as Tab[]).map((t) => (
          <button
            key={t}
            onClick={() => setTab(t)}
            className={`px-5 py-2.5 text-sm font-medium transition border-b-2 -mb-px ${
              tab === t
                ? 'border-green-600 text-green-700'
                : 'border-transparent text-gray-500 hover:text-gray-700'
            }`}
          >
            {t === 'usuarios' ? 'Usuários' : 'Ambientes'}
          </button>
        ))}
      </div>

      {tab === 'usuarios' ? <TabUsuarios /> : <TabAmbientes />}
    </div>
  )
}

/* ─────────────────────── Tab Usuários ─────────────────────── */

function TabUsuarios() {
  const [usuarios, setUsuarios] = useState<UsuarioAdmin[]>([])
  const [roles, setRoles] = useState<string[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [modalRoles, setModalRoles] = useState<UsuarioAdmin | null>(null)
  const [rolesForm, setRolesForm] = useState<string[]>([])
  const [saving, setSaving] = useState(false)
  const [busca, setBusca] = useState('')

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
          className="flex-1 max-w-sm rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
        />
        <span className="text-sm text-gray-500">{filtrados.length} usuário(s)</span>
      </div>

      {loading ? (
        <div className="py-12 text-center text-gray-400">Carregando...</div>
      ) : (
        <Card>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-xs font-medium uppercase tracking-wide text-gray-500">
                  <th className="px-4 py-3">Usuário</th>
                  <th className="px-4 py-3">Roles</th>
                  <th className="px-4 py-3">Cadastro</th>
                  <th className="px-4 py-3">Status</th>
                  <th className="px-4 py-3 text-right">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {filtrados.map((u) => (
                  <tr key={u.id} className={`hover:bg-gray-50 transition ${u.bloqueado ? 'opacity-60' : ''}`}>
                    <td className="px-4 py-3">
                      <p className="font-medium text-gray-800">{u.nome ?? '—'}</p>
                      <p className="text-xs text-gray-400">{u.email}</p>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex flex-wrap gap-1">
                        {u.roles.length === 0
                          ? <span className="text-xs text-gray-400">Sem role</span>
                          : u.roles.map((r) => (
                            <span key={r} className={`rounded-full px-2 py-0.5 text-xs font-medium ${r === 'Admin' ? 'bg-purple-100 text-purple-700' : 'bg-blue-100 text-blue-700'}`}>{r}</span>
                          ))}
                      </div>
                    </td>
                    <td className="px-4 py-3 text-gray-500">
                      {u.dtaCriacao ? new Date(u.dtaCriacao).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <Badge color={u.bloqueado ? '#dc2626' : '#16a34a'}>
                        {u.bloqueado ? 'Bloqueado' : 'Ativo'}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        <button
                          onClick={() => openModalRoles(u)}
                          title="Gerenciar roles"
                          className="rounded px-2 py-1 text-xs text-indigo-600 hover:bg-indigo-50 transition"
                        >
                          Roles
                        </button>
                        <button
                          onClick={() => handleToggleBloqueio(u)}
                          title={u.bloqueado ? 'Desbloquear' : 'Bloquear'}
                          className={`rounded px-2 py-1 text-xs transition ${u.bloqueado ? 'text-green-600 hover:bg-green-50' : 'text-orange-600 hover:bg-orange-50'}`}
                        >
                          {u.bloqueado ? 'Desbloquear' : 'Bloquear'}
                        </button>
                        <button
                          onClick={() => handleDelete(u)}
                          title="Excluir usuário"
                          className="rounded px-2 py-1 text-xs text-red-500 hover:bg-red-50 transition"
                        >
                          Excluir
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtrados.length === 0 && (
                  <tr><td colSpan={5} className="px-4 py-8 text-center text-gray-400">Nenhum usuário encontrado.</td></tr>
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
          <p className="text-sm text-gray-500">Selecione as roles do usuário:</p>
          <div className="space-y-2">
            {roles.map((r) => (
              <label key={r} className="flex items-center gap-3 cursor-pointer rounded-lg border px-4 py-3 hover:bg-gray-50 transition">
                <input
                  type="checkbox"
                  checked={rolesForm.includes(r)}
                  onChange={(e) => {
                    if (e.target.checked) setRolesForm((prev) => [...prev, r])
                    else setRolesForm((prev) => prev.filter((x) => x !== r))
                  }}
                  className="h-4 w-4 rounded border-gray-300 text-green-600"
                />
                <span className={`text-sm font-medium ${r === 'Admin' ? 'text-purple-700' : 'text-blue-700'}`}>{r}</span>
              </label>
            ))}
          </div>
          <div className="flex justify-end gap-2 pt-2">
            <Button variant="secondary" onClick={() => setModalRoles(null)}>Cancelar</Button>
            <Button onClick={handleSalvarRoles} loading={saving}>Salvar</Button>
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
          className="flex-1 max-w-sm rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
        />
        <span className="text-sm text-gray-500">{filtrados.length} ambiente(s)</span>
      </div>

      {loading ? (
        <div className="py-12 text-center text-gray-400">Carregando...</div>
      ) : (
        <Card>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left text-xs font-medium uppercase tracking-wide text-gray-500">
                  <th className="px-4 py-3">Nome</th>
                  <th className="px-4 py-3">Membros</th>
                  <th className="px-4 py-3">Criado em</th>
                  <th className="px-4 py-3 text-right">Ações</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-50">
                {filtrados.map((a) => (
                  <tr key={a.id} className="hover:bg-gray-50 transition">
                    <td className="px-4 py-3 font-medium text-gray-800">{a.nome}</td>
                    <td className="px-4 py-3 text-gray-500">{a.membros?.length ?? 0}</td>
                    <td className="px-4 py-3 text-gray-500">
                      {a.dataCriacao ? new Date(a.dataCriacao).toLocaleDateString('pt-BR') : '—'}
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex justify-end gap-1">
                        {(a.membros?.length ?? 0) > 0 && (
                          <button
                            onClick={() => setModalMembros(a)}
                            className="rounded px-2 py-1 text-xs text-indigo-600 hover:bg-indigo-50 transition"
                          >
                            Membros
                          </button>
                        )}
                        <button
                          onClick={() => handleDelete(a)}
                          className="rounded px-2 py-1 text-xs text-red-500 hover:bg-red-50 transition"
                        >
                          Excluir
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
                {filtrados.length === 0 && (
                  <tr><td colSpan={4} className="px-4 py-8 text-center text-gray-400">Nenhum ambiente encontrado.</td></tr>
                )}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Modal membros */}
      <Modal open={!!modalMembros} onClose={() => setModalMembros(null)} title={`Membros — ${modalMembros?.nome}`} size="md">
        <div className="divide-y rounded-lg border max-h-80 overflow-y-auto">
          {modalMembros?.membros?.map((m) => (
            <div key={m.usuario?.id} className="flex items-center justify-between px-4 py-3">
              <div>
                <p className="text-sm font-medium text-gray-800">{m.usuario?.nome}</p>
                <p className="text-xs text-gray-500">{m.usuario?.email}</p>
              </div>
              <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${m.role === 'Dono' ? 'bg-yellow-100 text-yellow-700' : m.role === 'Admin' ? 'bg-purple-100 text-purple-700' : 'bg-gray-100 text-gray-500'}`}>
                {m.role}
              </span>
            </div>
          ))}
        </div>
      </Modal>
    </>
  )
}
