import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { Icon } from '@iconify/react'
import { register as apiRegister } from '../api/auth'
import Button from '../components/ui/Button'
import Input from '../components/ui/Input'
import Alert from '../components/ui/Alert'

export default function Register() {
  const navigate = useNavigate()
  const [form, setForm] = useState({ nome: '', email: '', senha: '', confirmacaoSenha: '' })
  const [loading, setLoading] = useState(false)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')

  function set(field: string) {
    return (e: React.ChangeEvent<HTMLInputElement>) => setForm((f) => ({ ...f, [field]: e.target.value }))
  }

  async function handleSubmit(e: FormEvent) {
    e.preventDefault()
    setError('')
    if (form.senha !== form.confirmacaoSenha) {
      setError('As senhas não coincidem.')
      return
    }
    setLoading(true)
    try {
      const { data } = await apiRegister(form)
      if (data.success) {
        setSuccess('Conta criada! Redirecionando para o login...')
        setTimeout(() => navigate('/login'), 1500)
      } else {
        setError(data.errorMessage || 'Erro ao criar conta.')
      }
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { errorMessage?: string } } })?.response?.data?.errorMessage
      setError(msg || 'Erro ao criar conta.')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-fin-bg p-4">
      <div className="w-full max-w-sm">
        <div className="mb-8 text-center">
          <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-[12px] bg-fin-brand text-white">
            <Icon icon="lucide:dollar-sign" width={26} height={26} />
          </div>
          <h1 className="mt-3 text-2xl font-bold text-fin-text-primary">Criar Conta</h1>
          <p className="mt-1 text-sm text-fin-text-secondary">Preencha os dados abaixo</p>
        </div>

        <div className="rounded-2xl border border-fin-border bg-fin-surface p-8 shadow-sm">
          <form onSubmit={handleSubmit} className="space-y-4">
            {error && <Alert type="error" message={error} />}
            {success && <Alert type="success" message={success} />}
            <Input label="Nome" value={form.nome} onChange={set('nome')} placeholder="Seu nome" required autoFocus />
            <Input label="E-mail" type="email" value={form.email} onChange={set('email')} placeholder="seu@email.com" required />
            <Input label="Senha" type="password" value={form.senha} onChange={set('senha')} placeholder="Mínimo 6 caracteres" required />
            <Input label="Confirmar Senha" type="password" value={form.confirmacaoSenha} onChange={set('confirmacaoSenha')} placeholder="Repita a senha" required />
            <Button type="submit" loading={loading} className="w-full mt-2">
              Criar Conta
            </Button>
          </form>
          <p className="mt-5 text-center text-sm text-fin-text-secondary">
            Já tem conta?{' '}
            <Link to="/login" className="font-medium text-fin-brand hover:underline">
              Entrar
            </Link>
          </p>
        </div>
      </div>
    </div>
  )
}
