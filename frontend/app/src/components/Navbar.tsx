import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { selecionarAmbiente, listarDoUsuario, criar } from '../api/ambiente'
import type { AmbienteResponse } from '../types/api'

export default function Navbar() {
  const { nome, email, nomeAmbiente, logout, setAmbienteToken } = useAuth()
  const navigate = useNavigate()
  const [ambientes, setAmbientes] = useState<AmbienteResponse[]>([])
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
      <header className="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-6">
        {/* Ambiente ativo */}
        {nomeAmbiente && (
          <div className="flex items-center gap-2">
            <span className="rounded-full bg-green-100 px-3 py-1 text-xs font-semibold text-green-700">
              {nomeAmbiente}
            </span>
          </div>
        )}
        {!nomeAmbiente && <div />}

        <div className="relative">
          <button
            onClick={() => setShowDropdown((v) => !v)}
            className="flex items-center gap-2 rounded-lg px-3 py-2 hover:bg-gray-100 transition"
          >
            <div className="flex h-8 w-8 items-center justify-center rounded-full bg-green-100 text-green-700 font-semibold text-sm">
              {nome?.[0]?.toUpperCase() ?? '?'}
            </div>
            <div className="text-left hidden sm:block">
              <p className="text-sm font-medium text-gray-800 leading-none">{nome ?? 'Usuário'}</p>
              <p className="text-xs text-gray-500">{email ?? ''}</p>
            </div>
            <svg className="h-4 w-4 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>

          {showDropdown && (
            <div className="absolute right-0 mt-1 w-48 rounded-xl border border-gray-200 bg-white shadow-lg z-20">
              <button
                onClick={handleTrocarAmbiente}
                className="flex w-full items-center gap-2 px-4 py-2.5 text-sm text-gray-700 hover:bg-gray-50"
              >
                🔄 Gerenciar Ambientes
              </button>
              <hr className="border-gray-100" />
              <button
                onClick={handleLogout}
                className="flex w-full items-center gap-2 px-4 py-2.5 text-sm text-red-600 hover:bg-red-50"
              >
                🚪 Sair
              </button>
            </div>
          )}
        </div>
      </header>

      {showAmbientes && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
          <div className="absolute inset-0 bg-black/40" onClick={() => setShowAmbientes(false)} />
          <div className="relative w-full max-w-sm rounded-xl bg-white shadow-xl">
            <div className="flex items-center justify-between border-b px-6 py-4">
              <h2 className="text-lg font-semibold text-gray-800">Ambientes</h2>
              <button onClick={() => setShowAmbientes(false)} className="rounded-lg p-1 hover:bg-gray-100 text-gray-500">
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>

            <div className="px-6 py-4 space-y-2">
              {ambientes.length === 0 && (
                <p className="text-sm text-gray-500 text-center py-2">Nenhum ambiente encontrado.</p>
              )}
              {ambientes.map((a) => (
                <button
                  key={a.id}
                  onClick={() => handleSelecionar(a)}
                  className={`w-full text-left rounded-lg border px-4 py-3 hover:bg-green-50 hover:border-green-200 transition ${a.nome === nomeAmbiente ? 'border-green-400 bg-green-50' : 'border-gray-200'}`}
                >
                  <div className="flex items-center justify-between">
                    <p className="font-medium text-gray-800">{a.nome}</p>
                    {a.nome === nomeAmbiente && (
                      <span className="text-xs text-green-600 font-semibold">ativo</span>
                    )}
                  </div>
                  <p className="text-xs text-gray-500">{a.membros?.length ?? 0} membro(s)</p>
                </button>
              ))}
            </div>

            {/* Criar novo ambiente */}
            <div className="border-t px-6 py-4 space-y-2">
              <p className="text-xs font-medium text-gray-500 uppercase tracking-wide">Criar novo ambiente</p>
              {erro && <p className="text-xs text-red-600">{erro}</p>}
              <div className="flex gap-2">
                <input
                  type="text"
                  placeholder="Nome do ambiente..."
                  value={novoNome}
                  onChange={(e) => setNovoNome(e.target.value)}
                  onKeyDown={(e) => e.key === 'Enter' && handleCriar()}
                  className="flex-1 rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-500"
                />
                <button
                  onClick={handleCriar}
                  disabled={criando || !novoNome.trim()}
                  className="rounded-lg bg-green-600 px-4 py-2 text-sm font-medium text-white hover:bg-green-700 disabled:opacity-50 transition"
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
