import { useState, useEffect } from 'react'
import { useNavigate } from 'react-router-dom'
import { listarDoUsuario, selecionarAmbiente, criar as criarAmbiente } from '../api/ambiente'
import { useAuth } from '../context/AuthContext'
import type { AmbienteResponse } from '../types/api'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Alert from '../components/ui/Alert'

export default function SelecionarAmbiente() {
  const navigate = useNavigate()
  const { setAmbienteToken, logout } = useAuth()
  const [ambientes, setAmbientes] = useState<AmbienteResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [selecting, setSelecting] = useState<string | null>(null)
  const [error, setError] = useState('')
  const [novoNome, setNovoNome] = useState('')
  const [criando, setCriando] = useState(false)
  const [showNovo, setShowNovo] = useState(false)

  useEffect(() => {
    carregarAmbientes()
  }, [])

  async function carregarAmbientes() {
    setLoading(true)
    try {
      const { data } = await listarDoUsuario()
      setAmbientes(data.dados ?? [])
    } catch {
      setError('Erro ao carregar ambientes.')
    } finally {
      setLoading(false)
    }
  }

  async function handleSelecionar(id: string) {
    setSelecting(id)
    setError('')
    try {
      const { data } = await selecionarAmbiente(id)
      if (data.dados?.accessToken && data.dados.refreshToken) {
        setAmbienteToken(data.dados.accessToken, data.dados.refreshToken)
        navigate('/')
      } else {
        setError('Falha ao selecionar ambiente.')
      }
    } catch {
      setError('Erro ao selecionar ambiente.')
    } finally {
      setSelecting(null)
    }
  }

  async function handleCriar() {
    if (!novoNome.trim()) return
    setCriando(true)
    setError('')
    try {
      await criarAmbiente({ nome: novoNome.trim() })
      setNovoNome('')
      setShowNovo(false)
      await carregarAmbientes()
    } catch {
      setError('Erro ao criar ambiente.')
    } finally {
      setCriando(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gray-50 p-4">
      <div className="w-full max-w-md">
        <div className="mb-8 text-center">
          <span className="text-5xl">🏠</span>
          <h1 className="mt-3 text-2xl font-bold text-gray-900">Selecionar Ambiente</h1>
          <p className="mt-1 text-sm text-gray-500">Escolha o ambiente financeiro para acessar</p>
        </div>

        <div className="rounded-2xl border border-gray-200 bg-white p-6 shadow-sm">
          {error && <div className="mb-4"><Alert type="error" message={error} /></div>}

          {loading ? (
            <div className="py-8 text-center text-gray-400">Carregando...</div>
          ) : (
            <div className="space-y-2">
              {ambientes.length === 0 && !showNovo && (
                <p className="py-4 text-center text-sm text-gray-500">Você não tem ambientes. Crie um abaixo.</p>
              )}
              {ambientes.map((a) => (
                <button
                  key={a.id}
                  onClick={() => handleSelecionar(a.id)}
                  disabled={!!selecting}
                  className="w-full rounded-xl border border-gray-200 px-4 py-3.5 text-left hover:border-green-300 hover:bg-green-50 transition disabled:opacity-60"
                >
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="font-semibold text-gray-800">{a.nome}</p>
                      <p className="text-xs text-gray-500 mt-0.5">{a.membros?.length ?? 0} membro(s)</p>
                    </div>
                    {selecting === a.id ? (
                      <svg className="h-5 w-5 animate-spin text-green-600" viewBox="0 0 24 24" fill="none">
                        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
                        <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
                      </svg>
                    ) : (
                      <svg className="h-5 w-5 text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                      </svg>
                    )}
                  </div>
                </button>
              ))}

              {showNovo && (
                <div className="rounded-xl border border-green-200 bg-green-50 p-4 space-y-3">
                  <Input
                    label="Nome do Ambiente"
                    value={novoNome}
                    onChange={(e) => setNovoNome(e.target.value)}
                    placeholder="Ex: Pessoal, Família, Empresa..."
                    autoFocus
                    onKeyDown={(e) => { if (e.key === 'Enter') handleCriar() }}
                  />
                  <div className="flex gap-2">
                    <Button onClick={handleCriar} loading={criando} size="sm">Criar</Button>
                    <Button variant="secondary" onClick={() => setShowNovo(false)} size="sm">Cancelar</Button>
                  </div>
                </div>
              )}

              {!showNovo && (
                <button
                  onClick={() => setShowNovo(true)}
                  className="w-full rounded-xl border-2 border-dashed border-gray-200 px-4 py-3 text-sm text-gray-500 hover:border-green-300 hover:text-green-600 transition"
                >
                  + Criar novo ambiente
                </button>
              )}
            </div>
          )}

          <div className="mt-5 border-t pt-4">
            <button onClick={() => { logout(); navigate('/login') }} className="text-sm text-gray-500 hover:text-red-600 transition">
              Sair da conta
            </button>
          </div>
        </div>
      </div>
    </div>
  )
}
