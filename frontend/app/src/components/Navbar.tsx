import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { useTheme } from '../context/ThemeContext'
import { selecionarAmbiente, listarDoUsuario, criar, buscarPorId } from '../api/ambiente'
import type { AmbienteResponse } from '../types/api'

export default function Navbar() {
  const { nome, email, nomeAmbiente, ambienteId, isDemo, logout, setAmbienteToken } = useAuth()
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
    logout()
    navigate('/login')
  }

  return (
    <>
      <header className="flex h-16 items-center justify-between border-b border-fin-border bg-fin-surface px-6">
        {/* Ambiente ativo */}
        {nomeAmbiente && (
          <div className="flex items-center gap-2">
            <span className="rounded-full bg-fin-brand-soft px-3 py-1 text-xs font-semibold text-fin-brand">
              {nomeAmbiente}
            </span>
          </div>
        )}
        {!nomeAmbiente && <div />}

        <div className="flex items-center gap-2">
          {/* Dark mode toggle */}
          <button
            onClick={toggleTheme}
            className="rounded-lg p-2 hover:bg-fin-ghost-hover text-fin-text-secondary transition"
            title={isDark ? 'Ativar modo claro' : 'Ativar modo escuro'}
          >
            {isDark ? (
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364-6.364l-.707.707M6.343 17.657l-.707.707M17.657 17.657l-.707-.707M6.343 6.343l-.707-.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
              </svg>
            ) : (
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
              </svg>
            )}
          </button>

          <div className="relative">
            <button
              onClick={() => setShowDropdown((v) => !v)}
              className="flex items-center gap-2 rounded-lg px-3 py-2 hover:bg-fin-ghost-hover transition"
            >
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-fin-brand-soft text-fin-brand font-semibold text-sm">
                {nome?.[0]?.toUpperCase() ?? '?'}
              </div>
              <div className="text-left hidden sm:block">
                <p className="text-sm font-medium text-fin-text-primary leading-none">{nome ?? 'Usuário'}</p>
                <p className="text-xs text-fin-text-muted">{email ?? ''}</p>
              </div>
              <svg className="h-4 w-4 text-fin-text-muted" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>

            {showDropdown && (
              <div className="absolute right-0 mt-1 w-48 rounded-xl border border-fin-border bg-fin-surface shadow-lg z-20">
                {!isDemo && (
                  <>
                    <button
                      onClick={handleTrocarAmbiente}
                      className="flex w-full items-center gap-2 px-4 py-2.5 text-sm text-fin-text-primary hover:bg-fin-ghost-hover"
                    >
                      🔄 Gerenciar Ambientes
                    </button>
                    <hr className="border-fin-border" />
                  </>
                )}
                <button
                  onClick={handleLogout}
                  className="flex w-full items-center gap-2 px-4 py-2.5 text-sm text-fin-negative hover:bg-fin-negative-soft"
                >
                  🚪 Sair
                </button>
              </div>
            )}
          </div>
        </div>
      </header>

      {showAmbientes && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div
            className="absolute inset-0"
            style={{ backgroundColor: 'var(--fin-overlay)' }}
            onClick={() => setShowAmbientes(false)}
          />
          <div className="relative w-full max-w-sm rounded-xl bg-fin-surface border border-fin-border shadow-xl">
            <div className="flex items-center justify-between border-b border-fin-border px-6 py-4">
              <h2 className="text-lg font-semibold text-fin-text-primary">Ambientes</h2>
              <button
                onClick={() => setShowAmbientes(false)}
                className="rounded-lg p-1 hover:bg-fin-surface-2 text-fin-text-muted"
              >
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                </svg>
              </button>
            </div>

            <div className="px-6 py-4 space-y-2">
              {ambientes.length === 0 && (
                <p className="text-sm text-fin-text-secondary text-center py-2">Nenhum ambiente encontrado.</p>
              )}
              {ambientes.map((a) => (
                <button
                  key={a.id}
                  onClick={() => handleSelecionar(a)}
                  className={`w-full text-left rounded-lg border px-4 py-3 transition ${
                    a.nome === nomeAmbiente
                      ? 'border-fin-brand bg-fin-brand-soft'
                      : 'border-fin-border hover:bg-fin-highlight-row hover:border-fin-brand'
                  }`}
                >
                  <div className="flex items-center justify-between">
                    <p className="font-medium text-fin-text-primary">{a.nome}</p>
                    {a.nome === nomeAmbiente && (
                      <span className="text-xs text-fin-brand font-semibold">ativo</span>
                    )}
                  </div>
                  <p className="text-xs text-fin-text-muted">{a.membros?.length ?? 0} membro(s)</p>
                </button>
              ))}
            </div>

            {/* Criar novo ambiente */}
            <div className="border-t border-fin-border px-6 py-4 space-y-2">
              <p className="text-xs font-medium text-fin-text-muted uppercase tracking-wide">Criar novo ambiente</p>
              {erro && <p className="text-xs text-fin-negative">{erro}</p>}
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="Nome do ambiente..."
                  value={novoNome}
                  onChange={(e) => setNovoNome(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCriar()}
                  className="flex-1 rounded-lg border border-fin-border bg-fin-surface text-fin-text-primary placeholder:text-fin-text-muted px-3 py-2 text-sm focus:outline-none focus:shadow-fin-focus focus:border-fin-brand"
                />
                <button
                  onClick={handleCriar}
                  disabled={criando || !novoNome.trim()}
                  className="rounded-lg bg-fin-brand px-4 py-2 text-sm font-medium text-white hover:bg-fin-brand-hover disabled:opacity-50 transition"
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
