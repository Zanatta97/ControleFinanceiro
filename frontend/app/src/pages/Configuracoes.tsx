import { useEffect, useState } from 'react'
import { Icon } from '@iconify/react'
import { getStatus, ativar, desativar } from '../api/logs'
import { listarDoUsuario, selecionarAmbiente, criar as criarAmbiente, atualizar as atualizarAmbiente, excluir as excluirAmbiente, adicionarMembro, removerMembro } from '../api/ambiente'
import { alterarSenha, type AlterarSenhaRequest } from '../api/auth'
import { useAuth } from '../context/AuthContext'
import { useNavigate } from 'react-router-dom'
import type { AmbienteResponse } from '../types/api'
import Card from '../components/ui/Card'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Modal from '../components/ui/Modal'
import Alert from '../components/ui/Alert'
import ConfirmDialog from '../components/ui/ConfirmDialog'

export default function Configuracoes() {
  const { setAmbienteToken, nome, email, userId } = useAuth()

  function roleNoAmbiente(a: AmbienteResponse): string | null {
    return a.membros?.find((m) => m.usuario?.id === userId)?.role ?? null
  }

  function podeGerenciar(a: AmbienteResponse): boolean {
    const role = roleNoAmbiente(a)
    return role === 'Dono' || role === 'Admin'
  }
  const navigate = useNavigate()

  // Alterar Senha
  const SENHA_INICIAL: AlterarSenhaRequest = { senhaAtual: '', novaSenha: '', confirmacaoNovaSenha: '' }
  const [senhaForm, setSenhaForm] = useState<AlterarSenhaRequest>(SENHA_INICIAL)
  const [salvandoSenha, setSalvandoSenha] = useState(false)
  const [senhaError, setSenhaError] = useState('')
  const [senhaSuccess, setSenhaSuccess] = useState('')

  async function handleAlterarSenha() {
    setSenhaError('')
    setSenhaSuccess('')
    if (!senhaForm.senhaAtual || !senhaForm.novaSenha || !senhaForm.confirmacaoNovaSenha) {
      setSenhaError('Preencha todos os campos.')
      return
    }
    if (senhaForm.novaSenha !== senhaForm.confirmacaoNovaSenha) {
      setSenhaError('A nova senha e a confirmação não coincidem.')
      return
    }
    setSalvandoSenha(true)
    try {
      await alterarSenha(senhaForm)
      setSenhaSuccess('Senha alterada com sucesso!')
      setSenhaForm(SENHA_INICIAL)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errorMessage?: string } } })?.response?.data?.errorMessage
      setSenhaError(msg || 'Erro ao alterar a senha.')
    } finally {
      setSalvandoSenha(false)
    }
  }

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

  // Confirmações de exclusão
  const [ambToDelete, setAmbToDelete] = useState<AmbienteResponse | null>(null)
  const [membroToRemove, setMembroToRemove] = useState<{ ambienteId: string; membroId: string; nome: string } | null>(null)
  const [confirmLoading, setConfirmLoading] = useState(false)

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

  async function handleExcluirAmbiente() {
    if (!ambToDelete) return
    setConfirmLoading(true)
    try {
      await excluirAmbiente(ambToDelete.id)
      setAmbToDelete(null)
      await carregarAmbientes()
    } finally {
      setConfirmLoading(false)
    }
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

  async function handleRemoverMembro() {
    if (!membroToRemove) return
    const { ambienteId, membroId } = membroToRemove
    setConfirmLoading(true)
    try {
      await removerMembro(ambienteId, membroId)
      setMembroToRemove(null)
      await carregarAmbientes()
      const { data } = await listarDoUsuario()
      const atualizado = (data.dados ?? []).find((a) => a.id === ambienteId)
      setModalMembros(atualizado ?? null)
    } finally {
      setConfirmLoading(false)
    }
  }

  return (
    <div className="flex max-w-3xl flex-col gap-6">
      <h1 className="text-2xl font-bold text-fin-text-primary">Configurações</h1>

      {/* Perfil */}
      <Card>
        <div className="border-b border-fin-border px-6 py-4">
          <h2 className="font-semibold text-fin-text-primary">Perfil</h2>
        </div>
        <div className="px-6 py-5 flex items-center gap-4">
          <div className="flex h-14 w-14 items-center justify-center rounded-full bg-fin-brand-soft text-fin-brand text-xl font-bold">
            {nome?.[0]?.toUpperCase() ?? '?'}
          </div>
          <div>
            <p className="font-semibold text-fin-text-primary">{nome ?? 'Usuário'}</p>
            <p className="text-sm text-fin-text-secondary">{email ?? ''}</p>
          </div>
        </div>
      </Card>

      {/* Alterar Senha */}
      <Card>
        <div className="border-b border-fin-border px-6 py-4">
          <h2 className="font-semibold text-fin-text-primary">Alterar Senha</h2>
          <p className="text-xs text-fin-text-muted mt-0.5">Recomendado trocar a senha padrão no primeiro acesso.</p>
        </div>
        <div className="px-6 py-5 space-y-4 max-w-sm">
          {senhaError && <Alert type="error" message={senhaError} />}
          {senhaSuccess && <Alert type="success" message={senhaSuccess} />}
          <Input
            label="Senha atual"
            type="password"
            value={senhaForm.senhaAtual}
            onChange={(e) => setSenhaForm((f) => ({ ...f, senhaAtual: e.target.value }))}
            placeholder="••••••••"
          />
          <Input
            label="Nova senha"
            type="password"
            value={senhaForm.novaSenha}
            onChange={(e) => setSenhaForm((f) => ({ ...f, novaSenha: e.target.value }))}
            placeholder="••••••••"
          />
          <Input
            label="Confirmar nova senha"
            type="password"
            value={senhaForm.confirmacaoNovaSenha}
            onChange={(e) => setSenhaForm((f) => ({ ...f, confirmacaoNovaSenha: e.target.value }))}
            placeholder="••••••••"
            onKeyDown={(e) => { if (e.key === 'Enter') handleAlterarSenha() }}
          />
          <div className="flex justify-end">
            <Button onClick={handleAlterarSenha} loading={salvandoSenha}>Alterar senha</Button>
          </div>
        </div>
      </Card>

      {/* Logging */}
      <Card>
        <div className="border-b border-fin-border px-6 py-4">
          <h2 className="font-semibold text-fin-text-primary">Log Detalhado de Requisições</h2>
          <p className="text-xs text-fin-text-muted mt-0.5">Quando ativo, todas as requisições com corpo são registradas no banco.</p>
        </div>
        <div className="px-6 py-5 flex items-center justify-between">
          <div>
            {loadingLog ? (
              <p className="text-sm text-fin-text-muted">Verificando status...</p>
            ) : (
              <p className="flex items-center gap-1.5 text-sm text-fin-text-primary">
                Status atual:{' '}
                <span className={`inline-flex items-center gap-1.5 font-semibold ${logAtivo ? 'text-fin-positive' : 'text-fin-text-muted'}`}>
                  <span className="h-2 w-2 rounded-full" style={{ backgroundColor: logAtivo ? 'var(--fin-positive)' : 'var(--fin-text-muted)' }} />
                  {logAtivo ? 'Ativo' : 'Inativo'}
                </span>
              </p>
            )}
            <p className="text-xs text-fin-text-muted mt-1">
              {logAtivo ? 'Todas as requisições estão sendo logadas (incluindo bodies).' : 'Apenas erros 4xx/5xx são registrados.'}
            </p>
          </div>
          <button
            onClick={toggleLog}
            disabled={togglingLog || loadingLog}
            className={`relative inline-flex h-7 w-12 items-center rounded-full transition-colors focus:outline-none focus:shadow-fin-focus disabled:opacity-50 ${logAtivo ? 'bg-fin-brand' : 'bg-fin-surface-2'}`}
          >
            <span className={`inline-block h-5 w-5 rounded-full bg-white shadow transition-transform ${logAtivo ? 'translate-x-6' : 'translate-x-1'}`} />
          </button>
        </div>
      </Card>

      {/* Ambientes */}
      <Card>
        <div className="flex items-center justify-between border-b border-fin-border px-6 py-4">
          <div>
            <h2 className="font-semibold text-fin-text-primary">Ambientes</h2>
            <p className="text-xs text-fin-text-muted mt-0.5">Gerencie seus espaços financeiros.</p>
          </div>
          <Button size="sm" onClick={openNovoAmbiente}>+ Novo</Button>
        </div>
        <div className="divide-y divide-fin-border">
          {loadingAmbientes ? (
            <p className="px-6 py-5 text-sm text-fin-text-muted">Carregando...</p>
          ) : ambientes.length === 0 ? (
            <p className="px-6 py-5 text-sm text-fin-text-muted text-center">Nenhum ambiente.</p>
          ) : ambientes.map((a) => (
            <div key={a.id} className="flex items-center justify-between px-6 py-4">
              <div>
                <p className="font-medium text-fin-text-primary">{a.nome}</p>
                <p className="text-xs text-fin-text-muted">{a.membros?.length ?? 0} membro(s)</p>
              </div>
              <div className="flex gap-2">
                <Button size="sm" variant="ghost" onClick={() => handleSelecionarAmbiente(a.id)}>Entrar</Button>
                {podeGerenciar(a) && (
                  <>
                    <button onClick={() => setModalMembros(a)} title="Membros" className="flex h-8 w-8 items-center justify-center rounded-lg text-fin-text-muted transition hover:bg-fin-surface-2 hover:text-fin-text-primary">
                      <Icon icon="lucide:users" width={15} height={15} />
                    </button>
                    <button onClick={() => openEditAmbiente(a)} title="Editar" className="flex h-8 w-8 items-center justify-center rounded-lg text-fin-text-muted transition hover:bg-fin-surface-2 hover:text-fin-text-primary">
                      <Icon icon="lucide:pencil" width={15} height={15} />
                    </button>
                    <button onClick={() => setAmbToDelete(a)} title="Excluir" className="flex h-8 w-8 items-center justify-center rounded-lg text-fin-text-muted transition hover:bg-fin-negative-soft hover:text-fin-negative">
                      <Icon icon="lucide:trash-2" width={15} height={15} />
                    </button>
                  </>
                )}
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
          {modalMembros && podeGerenciar(modalMembros) && (
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
          )}
          <div className="divide-y divide-fin-border max-h-64 overflow-y-auto rounded-lg border border-fin-border">
            {modalMembros?.membros?.length === 0 && <p className="px-4 py-3 text-sm text-fin-text-muted">Nenhum membro.</p>}
            {modalMembros?.membros?.map((m) => (
              <div key={m.usuario?.id} className="flex items-center justify-between px-4 py-3">
                <div>
                  <p className="text-sm font-medium text-fin-text-primary">{m.usuario?.nome}</p>
                  <p className="text-xs text-fin-text-secondary">{m.usuario?.email}</p>
                </div>
                <div className="flex items-center gap-2">
                  <span className="text-xs text-fin-text-secondary bg-fin-surface-2 rounded px-2 py-0.5">{m.role}</span>
                  {modalMembros && podeGerenciar(modalMembros) && m.usuario?.id && m.usuario.id !== userId && (
                    <button onClick={() => setMembroToRemove({ ambienteId: modalMembros.id, membroId: m.usuario!.id!, nome: m.usuario?.nome ?? m.usuario?.email ?? 'membro' })} className="text-xs text-fin-negative hover:text-fin-negative-hover">Remover</button>
                  )}
                </div>
              </div>
            ))}
          </div>
        </div>
      </Modal>

      <ConfirmDialog
        open={!!ambToDelete}
        variant="delete"
        title="Excluir ambiente"
        message={<>Excluir o ambiente <strong className="text-fin-text-primary">{ambToDelete?.nome}</strong>? Todos os dados (contas, transações, categorias) serão perdidos.</>}
        confirmLabel="Excluir"
        loading={confirmLoading}
        onConfirm={handleExcluirAmbiente}
        onClose={() => setAmbToDelete(null)}
      />

      <ConfirmDialog
        open={!!membroToRemove}
        variant="warning"
        title="Remover membro"
        message={<>Remover <strong className="text-fin-text-primary">{membroToRemove?.nome}</strong> deste ambiente?</>}
        confirmLabel="Remover"
        loading={confirmLoading}
        onConfirm={handleRemoverMembro}
        onClose={() => setMembroToRemove(null)}
      />
    </div>
  )
}
