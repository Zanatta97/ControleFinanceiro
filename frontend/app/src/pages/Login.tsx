import { useState, type FormEvent } from 'react'
import { useNavigate } from 'react-router-dom'
import { login as apiLogin, demoLogin as apiDemoLogin } from '../api/auth'
import { useAuth } from '../context/AuthContext'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Alert from '../components/ui/Alert'

export default function Login() {
  const navigate = useNavigate()
  const { login } = useAuth()
  const [email, setEmail] = useState('')
  const [senha, setSenha] = useState('')
  const [loading, setLoading] = useState(false)
  const [demoLoading, setDemoLoading] = useState(false)
  const [error, setError] = useState('')

  async function handleDemo() {
    setError('')
    setDemoLoading(true)
    try {
      const { data } = await apiDemoLogin()
      if (data.success && data.dados?.token && data.dados.refreshToken) {
        login(data.dados.token, data.dados.refreshToken)
        navigate('/')
      } else {
        setError(data.errorMessage || 'Não foi possível acessar a demonstração.')
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errorMessage?: string } } })?.response?.data?.errorMessage
      setError(msg || 'Erro ao acessar a demonstração. Tente novamente.')
    } finally {
      setDemoLoading(false)
    }
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    setLoading(true)
    try {
      const { data } = await apiLogin({ email, senha })
      if (data.success && data.dados?.token && data.dados.refreshToken) {
        login(data.dados.token, data.dados.refreshToken)
        navigate('/selecionar-ambiente')
      } else {
        setError(data.errorMessage || 'Credenciais inválidas.')
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errorMessage?: string } } })?.response?.data?.errorMessage
      setError(msg || 'Erro ao fazer login. Verifique sua conexão.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-fin-bg p-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <span className="text-5xl">💰</span>
          <h1 className="mt-3 text-2xl font-bold text-fin-text-primary">Controle Financeiro</h1>
          <p className="mt-1 text-sm text-fin-text-secondary">Entre com sua conta</p>
        </div>

        <div className="rounded-2xl border border-fin-border bg-fin-surface p-8 shadow-sm">
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && <Alert type="error" message={error} />}
            <Input
              label="E-mail"
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="seu@email.com"
              required
              autoFocus
            />
            <Input
              label="Senha"
              type="password"
              value={senha}
              onChange={(e) => setSenha(e.target.value)}
              placeholder="••••••••"
              required
            />
            <Button type="submit" loading={loading} className="w-full mt-2">
              Entrar
            </Button>
          </form>

          <div className="mt-6 flex items-center gap-3">
            <div className="h-px flex-1 bg-fin-border" />
            <span className="text-xs text-fin-text-muted">ou</span>
            <div className="h-px flex-1 bg-fin-border" />
          </div>

          <Button
            type="button"
            variant="secondary"
            onClick={handleDemo}
            loading={demoLoading}
            className="w-full mt-4"
          >
            🚀 Acessar Demonstração
          </Button>
          <p className="mt-2 text-center text-xs text-fin-text-muted">
            Explore o sistema com dados de exemplo, sem precisar de cadastro.
          </p>
        </div>
      </div>
    </div>
  )
}
