import { useState, useEffect } from 'react'
import { NavLink, useNavigate } from 'react-router-dom'
import { Icon } from '@iconify/react'
import { useAuth } from '../context/AuthContext'
import { useTheme } from '../context/ThemeContext'
import { selecionarAmbiente, listarDoUsuario, criar, buscarPorId } from '../api/ambiente'
import type { AmbienteResponse } from '../types/api'

export const navLinks = [
  { to: '/', label: 'Dashboard', icon: 'lucide:layout-dashboard', end: true },
  { to: '/transacoes', label: 'Transações', icon: 'lucide:arrow-left-right', end: false },
  { to: '/contas', label: 'Contas', icon: 'lucide:credit-card', end: false },
  { to: '/categorias', label: 'Categorias', icon: 'lucide:tag', end: false },
  { to: '/orcamentos', label: 'Orçamentos', icon: 'lucide:target', end: false },
]

function iniciais(nome?: string | null) {
  if (!nome) return '?'
  const partes = nome.trim().split(/\s+/)
  if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase()
  return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase()
}

export default function Navbar() {
  const { nome, email, nomeAmbiente, ambienteId, isDemo, isAdmin, logout, setAmbienteToken } = useAuth()
  const { isDark, toggleTheme } = useTheme()
  const navigate = useNavigate()
  const [ambientes, setAmbientes] = useState<AmbienteResponse[]>([])

  useEffect(() => {
    if (ambienteId && !nomeAmbiente) {
      buscarPorId(ambienteId).then(({ data }) => {
        if (data.dados?.nome) {
          localStorage.setItem('nomeAmbiente', data.dados.nome)
          const token = localStorage.getItem('accessToken')!
          const refresh = localStorage.getItem('refreshToken')!
          setAmbienteToken(token, refresh, data.dados.nome)
        }
      }).catch(() => {})
    }
  }, [ambienteId, nomeAmbiente, setAmbienteToken])

  const [showDropdown, setShowDropdown] = useState(false)
  const [showAmbientes, setShowAmbientes] = useState(false)
  const [novoNome, setNovoNome] = useState('')
  const [criando, setCriando] = useState(false)
  const [erro, setErro] = useState('')

  async function handleTrocarAmbiente() {
    const { data } = await listarDoUsuario()
    setAmbientes(data.dados ?? [])
    setNovoNome('')
    setErro('')
    setShowAmbientes(true)
    setShowDropdown(false)
  }

  async function handleSelecionar(a: AmbienteResponse) {
    const { data } = await selecionarAmbiente(a.id)
    if (data.dados?.accessToken && data.dados.refreshToken) {
      setAmbienteToken(data.dados.accessToken, data.dados.refreshToken, a.nome ?? null)
    }
    setShowAmbientes(false)
    navigate('/')
  }

  async function handleCriar() {
    if (!novoNome.trim()) return
    setCriando(true)
    setErro('')
    try {
      const { data } = await criar({ nome: novoNome.trim() })
      const novo = data.dados
      if (!novo) { setErro('Erro ao criar ambiente.'); return }
      const sel = await selecionarAmbiente(novo.id)
      if (sel.data.dados?.accessToken && sel.data.dados.refreshToken) {
        setAmbienteToken(sel.data.dados.accessToken, sel.data.dados.refreshToken, novo.nome ?? null)
      }
      setShowAmbientes(false)
      navigate('/')
    } catch {
      setErro('Erro ao criar ambiente.')
    } finally {
      setCriando(false)
    }
  }

  function handleLogout() {
    setShowDropdown(false)
    logout()
    navigate('/login')
  }

  return (
    <>
      <header className="relative z-30 flex h-[60px] flex-none items-center gap-4 border-b border-fin-border bg-fin-surface px-4 sm:px-6">
        {/* Logo */}
        <div className="flex items-center gap-2.5">
          <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-fin-brand text-white">
            <Icon icon="lucide:dollar-sign" width={16} height={16} />
          </div>
          <span className="hidden text-sm font-bold text-fin-text-primary sm:inline">Controle Financeiro</span>
        </div>

        {/* Nav desktop */}
        <nav className="ml-1.5 hidden items-center gap-0.5 lg:flex">
          {navLinks.map((l) => (
            <NavLink
              key={l.to}
              to={l.to}
              end={l.end}
              className={({ isActive }) =>
                `flex items-center gap-2 rounded-[9px] px-3 py-2 text-[13.5px] font-semibold transition-colors ${
                  isActive
                    ? 'bg-fin-brand-soft text-fin-brand'
                    : 'text-fin-text-secondary hover:bg-fin-surface-2'
                }`
              }
            >
              <Icon icon={l.icon} width={17} height={17} />
              {l.label}
            </NavLink>
          ))}
        </nav>

        {/* Lado direito */}
        <div className="ml-auto flex flex-none items-center gap-2">
          {/* Badge do ambiente ativo */}
          {nomeAmbiente && (
            isDemo ? (
              <span className="flex h-8 items-center gap-1.5 rounded-full bg-fin-brand-soft px-3 text-[12.5px] font-semibold text-fin-brand">
                <Icon icon="lucide:layers" width={14} height={14} />
                <span className="hidden sm:inline">{nomeAmbiente}</span>
              </span>
            ) : (
              <button
                onClick={handleTrocarAmbiente}
                title="Trocar ambiente"
                className="flex h-8 items-center gap-1.5 rounded-full bg-fin-brand-soft px-3 text-[12.5px] font-semibold text-fin-brand transition hover:brightness-95"
              >
                <Icon icon="lucide:layers" width={14} height={14} />
                <span className="hidden sm:inline">{nomeAmbiente}</span>
              </button>
            )
          )}

          {/* Dark mode */}
          <button
            onClick={toggleTheme}
            title={isDark ? 'Ativar modo claro' : 'Ativar modo escuro'}
            className="flex h-[38px] w-[38px] items-center justify-center rounded-[9px] border border-fin-border bg-fin-surface text-fin-text-secondary transition hover:bg-fin-surface-2"
          >
            <Icon icon={isDark ? 'lucide:sun' : 'lucide:moon'} width={18} height={18} />
          </button>

          {/* Menu do usuário */}
          <div className="relative">
            <button
              onClick={() => setShowDropdown((v) => !v)}
              className="flex h-[38px] items-center gap-1.5 rounded-[9px] py-[3px] pl-[3px] pr-2 transition hover:bg-fin-surface-2"
            >
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-fin-invest-soft text-[13px] font-semibold text-fin-invest">
                {iniciais(nome)}
              </div>
              <Icon icon="lucide:chevron-down" width={14} height={14} className="text-fin-text-muted" />
            </button>

            {showDropdown && (
              <div className="absolute right-0 top-[46px] z-40 w-[228px] overflow-hidden rounded-xl border border-fin-border bg-fin-surface shadow-[0_14px_34px_rgba(0,0,0,0.16)]">
                <div className="border-b border-fin-border px-3.5 py-3">
                  <p className="text-[13.5px] font-semibold text-fin-text-primary">{nome ?? 'Usuário'}</p>
                  <p className="mt-0.5 text-[11.5px] text-fin-text-muted">{email ?? ''}</p>
                </div>
                <div className="p-1.5">
                  {!isDemo && (
                    <>
                      <button
                        onClick={() => { setShowDropdown(false); navigate('/configuracoes') }}
                        className="flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-[13px] font-medium text-fin-text-primary hover:bg-fin-surface-2"
                      >
                        <Icon icon="lucide:settings" width={16} height={16} />
                        Configurações
                      </button>
                      {isAdmin && (
                        <button
                          onClick={() => { setShowDropdown(false); navigate('/admin') }}
                          className="flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-[13px] font-medium text-fin-text-primary hover:bg-fin-surface-2"
                        >
                          <Icon icon="lucide:shield" width={16} height={16} />
                          Administração
                        </button>
                      )}
                      <button
                        onClick={handleTrocarAmbiente}
                        className="flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-[13px] font-medium text-fin-text-primary hover:bg-fin-surface-2"
                      >
                        <Icon icon="lucide:refresh-cw" width={16} height={16} />
                        Alterar ambiente
                      </button>
                      <div className="mx-1.5 my-1.5 h-px bg-fin-border" />
                    </>
                  )}
                  <button
                    onClick={handleLogout}
                    className="flex w-full items-center gap-2.5 rounded-lg px-2.5 py-2.5 text-[13px] font-medium text-fin-negative hover:bg-fin-negative-soft"
                  >
                    <Icon icon="lucide:log-out" width={16} height={16} />
                    Sair
                  </button>
                </div>
              </div>
            )}
          </div>
        </div>
      </header>

      {/* Overlay click-away para fechar o menu do usuário */}
      {showDropdown && (
        <div className="fixed inset-0 z-20" onClick={() => setShowDropdown(false)} />
      )}

      {/* Modal de ambientes — tela cheia no mobile, modal centralizado no desktop */}
      {showAmbientes && (
        <div className="fixed inset-0 z-50 flex items-stretch justify-center animate-fin-fade sm:items-center sm:p-4">
          <div
            className="absolute inset-0 hidden sm:block"
            style={{ backgroundColor: 'var(--fin-overlay)' }}
            onClick={() => setShowAmbientes(false)}
          />
          <div className="relative flex w-full flex-col overflow-hidden bg-fin-surface shadow-[0_20px_50px_rgba(0,0,0,0.3)] animate-fin-pop sm:h-auto sm:max-w-[400px] sm:rounded-[15px]">
            <div className="flex items-center gap-2 border-b border-fin-border px-4 py-4 sm:px-5">
              <button
                onClick={() => setShowAmbientes(false)}
                aria-label="Voltar"
                className="flex h-9 w-9 items-center justify-center rounded-lg text-fin-text-muted hover:bg-fin-surface-2 sm:hidden"
              >
                <Icon icon="lucide:chevron-left" width={22} height={22} />
              </button>
              <div className="flex-1">
                <h2 className="text-base font-bold text-fin-text-primary">Ambientes</h2>
                <p className="mt-0.5 text-[11.5px] text-fin-text-muted">Escolha o espaço financeiro ativo.</p>
              </div>
              <button
                onClick={() => setShowAmbientes(false)}
                className="hidden h-[30px] w-[30px] items-center justify-center rounded-lg text-fin-text-muted hover:bg-fin-surface-2 sm:flex"
              >
                <Icon icon="lucide:x" width={16} height={16} />
              </button>
            </div>

            <div className="flex flex-1 flex-col gap-2 overflow-y-auto px-4 py-3.5">
              {ambientes.length === 0 && (
                <p className="py-2 text-center text-sm text-fin-text-secondary">Nenhum ambiente encontrado.</p>
              )}
              {ambientes.map((a) => {
                const ativo = a.nome === nomeAmbiente
                return (
                  <button
                    key={a.id}
                    onClick={() => handleSelecionar(a)}
                    className={`flex w-full items-center gap-3 rounded-[11px] border-[1.5px] px-3 py-3 text-left transition hover:border-fin-brand ${
                      ativo ? 'border-fin-brand bg-fin-brand-soft' : 'border-fin-border bg-fin-surface'
                    }`}
                  >
                    <span className="flex h-9 w-9 flex-none items-center justify-center rounded-[10px] bg-fin-brand text-[15px] font-bold text-white">
                      {(a.nome?.[0] ?? '?').toUpperCase()}
                    </span>
                    <span className="min-w-0 flex-1">
                      <span className="block text-sm font-semibold text-fin-text-primary">{a.nome}</span>
                      <span className="block text-[11.5px] text-fin-text-muted">{a.membros?.length ?? 0} membro(s)</span>
                    </span>
                    {ativo && (
                      <span className="flex items-center gap-1 text-[11px] font-bold text-fin-brand">
                        <Icon icon="lucide:check" width={14} height={14} />
                        ativo
                      </span>
                    )}
                  </button>
                )
              })}
            </div>

            <div className="border-t border-fin-border px-4 py-3.5">
              <p className="mb-2 text-[10.5px] font-semibold uppercase tracking-wide text-fin-text-muted">Criar novo ambiente</p>
              {erro && <p className="mb-2 text-xs text-fin-negative">{erro}</p>}
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="Nome do ambiente…"
                  value={novoNome}
                  onChange={(e) => setNovoNome(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCriar()}
                  className="h-10 flex-1 rounded-[9px] border border-fin-border bg-fin-surface px-3 text-[13.5px] text-fin-text-primary placeholder:text-fin-text-muted focus:border-fin-brand focus:shadow-fin-focus focus:outline-none"
                />
                <button
                  onClick={handleCriar}
                  disabled={criando || !novoNome.trim()}
                  className="h-10 rounded-[9px] bg-fin-brand px-4 text-[13px] font-semibold text-white transition hover:bg-fin-brand-hover disabled:opacity-50"
                >
                  {criando ? '...' : 'Criar'}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  )
}
