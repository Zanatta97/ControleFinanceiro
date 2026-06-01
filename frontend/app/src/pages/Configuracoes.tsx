import { useEffect, useState } from 'react'
import { getStatus, ativar, desativar } from '../api/logs'
import { listarDoUsuario, selecionarAmbiente, criar as criarAmbiente, atualizar as atualizarAmbiente, excluir as excluirAmbiente, adicionarMembro, removerMembro } from '../api/ambiente'
import { useAuth } from '../context/AuthContext'
import { useNavigate } from 'react-router-dom'
import type { AmbienteResponse } from '../types/api'
import Card from '../components/ui/Card'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Modal from '../components/ui/Modal'
import Alert from '../components/ui/Alert'

export default function Configuracoes() {
  const { setAmbienteToken, nome, email } = useAuth()
  const navigate = useNavigate()

  // Logging
  const [logAtivo, setLogAtivo] = useState<boolean | null>(null)
  const [loadingLog, setLoadingLog] = useState(true)
  const [togglingLog, setTogglingLog] = useState(false)

  // Ambientes
  const [ambientes, setAmbientes] = useState<AmbienteResponse[]>([])
  const [loadingAmbientes, setLoadingAmbientes] = useState(true)
  const [modalAmbiente, setModalAmbiente] = useState(false)
  const [editingAmbiente, setEditingAmbiente] = useState<AmbienteResponse | null>(null)
  const [nomeAmbiente, setNomeAmbiente] = useState('')
  const [savingAmbiente, setSavingAmbiente] = useState(false)
  const [errorAmbiente, setErrorAmbiente] = useState('')

  // Membros
  const [modalMembros, setModalMembros] = useState<AmbienteResponse | null>(null)
  const [emailMembro, setEmailMembro] = useState('')
  const [addingMembro, setAddingMembro] = useState(false)
  const [errorMembro, setErrorMembro] = useState('')

  useEffect(() => {
    carregarLogStatus()
    carregarAmbientes()
  }, [])

  async function carregarLogStatus() {
    setLoadingLog(true)
    try {
      const { data } = await getStatus()
      const d = data as { dados?: { logAllRequests?: boolean }; logAllRequests?: boolean }
      setLogAtivo(d?.dados?.logAllRequests ?? d?.logAllRequests ?? false)
    } catch {
      setLogAtivo(false)
    } finally {
      setLoadingLog(false)
    }
  }

  async function toggleLog() {
    setTogglingLog(true)
    try {
      if (logAtivo) {
        await desativar()
        setLogAtivo(false)
      } else {
        await ativar()
        setLogAtivo(true)
      }
    } finally {
      setTogglingLog(false)
    }
  }

  async function carregarAmbientes() {
    setLoadingAmbientes(true)
    try {
      const { data } = await listarDoUsuario()
      setAmbientes(data.dados ?? [])
    } finally {
      setLoadingAmbientes(false)
    }
  }

  async function handleSelecionarAmbiente(id: string) {
    const { data } = await selecionarAmbiente(id)
    if (data.dados?.accessToken && data.dados.refreshToken) {
      setAmbienteToken(data.dados.accessToken, data.dados.refreshToken)
      navigate('/')
    }
  }

  function openNovoAmbiente() {
    setEditingAmbiente(null)
    setNomeAmbiente('')
    setErrorAmbiente('')
    setModalAmbiente(true)
  }

  function openEditAmbiente(a: AmbienteResponse) {
    setEditingAmbiente(a)
    setNomeAmbiente(a.nome ?? '')
    setErrorAmbiente('')
    setModalAmbiente(true)
  }

  async function handleSalvarAmbiente() {
    if (!nomeAmbiente.trim()) { setErrorAmbiente('Nome é obrigatório.'); return }
    setSavingAmbiente(true)
    setErrorAmbiente('')
    try {
      if (editingAmbiente) {
        await atualizarAmbiente(editingAmbiente.id, { nome: nomeAmbiente.trim() })
      } else {
        await criarAmbiente({ nome: nomeAmbiente.trim() })
      }
      setModalAmbiente(false)
      await carregarAmbientes()
    } catch {
      setErrorAmbiente('Erro ao salvar ambiente.')
    } finally {
      setSavingAmbiente(false)
    }
  }

  async function handleExcluirAmbiente(id: string) {
    if (!confirm('Excluir este ambiente? Todos os dados serão perdidos.')) return
    await excluirAmbiente(id)
    await carregarAmbientes()
  }

  async function handleAdicionarMembro() {
    if (!modalMembros || !emailMembro.trim()) return
    setAddingMembro(true)
    setErrorMembro('')
    try {
      await adicionarMembro(modalMembros.id, emailMembro.trim())
      setEmailMembro('')
      await carregarAmbientes()
      const { data } = await listarDoUsuario()
      const atualizado = (data.dados ?? []).find((a) => a.id === modalMembros.id)
      if (atualizado) setModalMembros(atualizado)
    } catch {
      setErrorMembro('Erro ao adicionar membro. Verifique o e-mail.')
    } finally {
      setAddingMembro(false)
    }
  }

  async function handleRemoverMembro(ambienteId: string, membroId: string) {
    if (!confirm('Remover este membro?')) return
    await removerMembro(ambienteId, membroId)
    await carregarAmbientes()
    const { data } = await listarDoUsuario()
    const atualizado = (data.dados ?? []).find((a) => a.id === ambienteId)
    setModalMembros(atualizado ?? null)
  }

  return (
    <div className="space-y-6 max-w-3xl">
      <h1 className="text-2xl font-bold text-gray-900">Configurações</h1>

      {/* Perfil */}
      <Card>
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold text-gray-800">Perfil</h2>
        </div>
        <div className="px-6 py-5 flex items-center gap-4">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-green-100 text-green-700 text-xl font-bold">
            {nome?.[0]?.toUpperCase() ?? '?'}
          </div>
          <div>
            <p className="font-semibold text-gray-800">{nome ?? 'Usuário'}</p>
            <p className="text-sm text-gray-500">{email ?? ''}</p>
          </div>
        </div>
      </Card>

      {/* Logging */}
      <Card>
        <div className="border-b px-6 py-4">
          <h2 className="font-semibold text-gray-800">Log Detalhado de Requisições</h2>
          <p className="text-xs text-gray-500 mt-0.5">Quando ativo, todas as requisições com corpo são registradas no banco.</p>
        </div>
        <div className="px-6 py-5 flex items-center justify-between">
          <div>
            {loadingLog ? (
              <p className="text-sm text-gray-400">Verificando status...</p>
            ) : (
              <p className="text-sm text-gray-700">
                Status atual:{' '}
                <span className={`font-semibold ${logAtivo ? 'text-green-600' : 'text-gray-500'}`}>
                  {logAtivo ? '🟢 Ativo' : '⚫ Inativo'}
                </span>
              </p>
            )}
            <p className="text-xs text-gray-400 mt-1">
              {logAtivo ? 'Todas as requisições estão sendo logadas (incluindo bodies).' : 'Apenas erros 4xx/5xx são registrados.'}
            </p>
          </div>
          <button
            onClick={toggleLog}
            disabled={togglingLog || loadingLog}
            className={`relative inline-flex h-7 w-12 items-center rounded-full transition-colors focus:outline-none focus:ring-2 focus:ring-green-500 focus:ring-offset-1 disabled:opacity-50 ${logAtivo ? 'bg-green-600' : 'bg-gray-300'}`}
          >
            <span className={`inline-block h-5 w-5 rounded-full bg-white shadow transition-transform ${logAtivo ? 'translate-x-6' : 'translate-x-1'}`} />
          </button>
        </div>
      </Card>

      {/* Ambientes */}
      <Card>
        <div className="flex items-center justify-between border-b px-6 py-4">
          <div>
            <h2 className="font-semibold text-gray-800">Ambientes</h2>
            <p className="text-xs text-gray-500 mt-0.5">Gerencie seus espaços financeiros.</p>
          </div>
          <Button size="sm" onClick={openNovoAmbiente}>+ Novo</Button>
        </div>
        <div className="divide-y">
          {loadingAmbientes ? (
            <p className="px-6 py-5 text-sm text-gray-400">Carregando...</p>
          ) : ambientes.length === 0 ? (
            <p className="px-6 py-5 text-sm text-gray-400 text-center">Nenhum ambiente.</p>
          ) : ambientes.map((a) => (
            <div key={a.id} className="flex items-center justify-between px-6 py-4">
              <div>
                <p className="font-medium text-gray-800">{a.nome}</p>
                <p className="text-xs text-gray-500">{a.membros?.length ?? 0} membro(s)</p>
              </div>
              <div className="flex gap-2">
                <Button size="sm" variant="ghost" onClick={() => handleSelecionarAmbiente(a.id)}>Entrar</Button>
                <Button size="sm" variant="ghost" onClick={() => setModalMembros(a)}>👥</Button>
                <Button size="sm" variant="ghost" onClick={() => openEditAmbiente(a)}>✏️</Button>
                <Button size="sm" variant="ghost" onClick={() => handleExcluirAmbiente(a.id)}>🗑️</Button>
              </div>
            </div>
          ))}
        </div>
      </Card>

      {/* Modal Ambiente */}
      <Modal open={modalAmbiente} onClose={() => setModalAmbiente(false)} title={editingAmbiente ? 'Editar Ambiente' : 'Novo Ambiente'} size="sm">
        <div className="space-y-4">
          {errorAmbiente && <Alert type="error" message={errorAmbiente} />}
          <Input label="Nome" value={nomeAmbiente} onChange={(e) => setNomeAmbiente(e.target.value)} placeholder="Ex: Pessoal, Família..." autoFocus onKeyDown={(e) => { if (e.key === 'Enter') handleSalvarAmbiente() }} />
          <div className="flex justify-end gap-2">
            <Button variant="secondary" onClick={() => setModalAmbiente(false)}>Cancelar</Button>
            <Button onClick={handleSalvarAmbiente} loading={savingAmbiente}>Salvar</Button>
          </div>
        </div>
      </Modal>

      {/* Modal Membros */}
      <Modal open={!!modalMembros} onClose={() => setModalMembros(null)} title={`Membros — ${modalMembros?.nome}`} size="md">
        <div className="space-y-4">
          {errorMembro && <Alert type="error" message={errorMembro} />}
          <div className="flex gap-2">
            <Input
              placeholder="E-mail do novo membro..."
              value={emailMembro}
              onChange={(e) => setEmailMembro(e.target.value)}
              className="flex-1"
              onKeyDown={(e) => { if (e.key === 'Enter') handleAdicionarMembro() }}
            />
            <Button onClick={handleAdicionarMembro} loading={addingMembro} size="sm">Adicionar</Button>
          </div>
          <div className="divide-y max-h-64 overflow-y-auto rounded-lg border">
            {modalMembros?.membros?.length === 0 && <p className="px-4 py-3 text-sm text-gray-400">Nenhum membro.</p>}
            {modalMembros?.membros?.map((m) => (
              <div key={m.usuario?.id} className="flex items-center justify-between px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-gray-800">{m.usuario?.nome}</p>
                  <p className="text-xs text-gray-500">{m.usuario?.email}</p>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs text-gray-400 bg-gray-100 rounded px-2 py-0.5">{m.role}</span>
                  {m.usuario?.id && (
                    <button onClick={() => handleRemoverMembro(modalMembros.id, m.usuario!.id!)} className="text-xs text-red-500 hover:text-red-700">Remover</button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      </Modal>
    </div>
  )
}
