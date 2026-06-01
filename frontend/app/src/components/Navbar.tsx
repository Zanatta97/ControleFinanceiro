import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { selecionarAmbiente, listarDoUsuario } from '../api/ambiente'
import type { AmbienteResponse } from '../types/api'

export default function Navbar() {
  const { nome, email, logout, setAmbienteToken } = useAuth()
  const navigate = useNavigate()
  const [ambientes, setAmbientes] = useState<AmbienteResponse[]>([])
  const [showDropdown, setShowDropdown] = useState(false)
  const [showAmbientes, setShowAmbientes] = useState(false)

  async function handleTrocarAmbiente() {
    const { data } = await listarDoUsuario()
    setAmbientes(data.dados ?? [])
    setShowAmbientes(true)
    setShowDropdown(false)
  }

  async function handleSelecionar(id: string) {
    const { data } = await selecionarAmbiente(id)
    if (data.dados?.accessToken && data.dados.refreshToken) {
      setAmbienteToken(data.dados.accessToken, data.dados.refreshToken)
    }
    setShowAmbientes(false)
    navigate('/')
  }

  function handleLogout() {
    logout()
    navigate('/login')
  }

  return (
    <>
      <header className="flex h-16 items-center justify-between border-b border-gray-200 bg-white px-6">
        <div />
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
                🔄 Trocar Ambiente
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
              <h2 className="text-lg font-semibold text-gray-800">Trocar Ambiente</h2>
              <button onClick={() => setShowAmbientes(false)} className="rounded-lg p-1 hover:bg-gray-100 text-gray-500">
                <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
              </button>
            </div>
            <div className="px-6 py-4 space-y-2">
              {ambientes.length === 0 && <p className="text-sm text-gray-500 text-center py-4">Nenhum ambiente encontrado.</p>}
              {ambientes.map((a) => (
                <button
                  key={a.id}
                  onClick={() => handleSelecionar(a.id)}
                  className="w-full text-left rounded-lg border border-gray-200 px-4 py-3 hover:bg-green-50 hover:border-green-200 transition"
                >
                  <p className="font-medium text-gray-800">{a.nome}</p>
                  <p className="text-xs text-gray-500">{a.membros?.length ?? 0} membro(s)</p>
                </button>
              ))}
            </div>
          </div>
        </div>
      )}
    </>
  )
}
