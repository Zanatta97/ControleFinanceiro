import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Login from './Login'
import { AuthProvider } from '../context/AuthContext'
import { demoLogin, login } from '../api/auth'
import type { LoginTokenDTO } from '../types/api'
import { adiado, criarToken, erroHttp, respostaOk } from '../test/helpers'

vi.mock('../api/auth', () => ({ login: vi.fn(), demoLogin: vi.fn() }))

const apiLogin = vi.mocked(login)
const apiDemo = vi.mocked(demoLogin)

const tokens = (): LoginTokenDTO => ({
  token: criarToken({ name: 'Ana' }),
  refreshToken: 'refresh-1',
  expiration: '2030-01-01T00:00:00Z',
})

function renderizar() {
  render(
    <AuthProvider>
      <MemoryRouter initialEntries={['/login']}>
        <Routes>
          <Route path="/login" element={<Login />} />
          <Route path="/selecionar-ambiente" element={<p>Tela de ambientes</p>} />
          <Route path="/" element={<p>Tela inicial</p>} />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  )
}

async function preencherEEntrar(email = 'ana@exemplo.com', senha = 'segredo1') {
  await userEvent.type(screen.getByLabelText('E-mail'), email)
  await userEvent.type(screen.getByLabelText('Senha'), senha)
  await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
}

describe('Login', () => {
  beforeEach(() => {
    apiLogin.mockReset()
    apiDemo.mockReset()
  })

  it('renderiza o formulário com e-mail, senha e demonstração', () => {
    renderizar()
    expect(screen.getByRole('heading', { name: 'Controle Financeiro' })).toBeInTheDocument()
    expect(screen.getByLabelText('E-mail')).toHaveAttribute('type', 'email')
    expect(screen.getByLabelText('E-mail')).toBeRequired()
    expect(screen.getByLabelText('E-mail')).toHaveFocus()
    expect(screen.getByLabelText('Senha')).toHaveAttribute('type', 'password')
    expect(screen.getByRole('button', { name: 'Acessar demonstração' })).toBeInTheDocument()
  })

  it('login certo salva a sessão e vai para a seleção de ambiente', async () => {
    const t = tokens()
    apiLogin.mockResolvedValue(respostaOk(t))
    renderizar()

    await preencherEEntrar()

    expect(apiLogin).toHaveBeenCalledWith({ email: 'ana@exemplo.com', senha: 'segredo1' })
    expect(await screen.findByText('Tela de ambientes')).toBeInTheDocument()
    expect(localStorage.getItem('accessToken')).toBe(t.token)
    expect(localStorage.getItem('refreshToken')).toBe('refresh-1')
  })

  it('desabilita o botão enquanto entra', async () => {
    const pendente = adiado<ReturnType<typeof respostaOk<LoginTokenDTO>>>()
    apiLogin.mockReturnValue(pendente.promise)
    renderizar()

    await preencherEEntrar()
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeDisabled()

    pendente.resolver(respostaOk(tokens()))
    expect(await screen.findByText('Tela de ambientes')).toBeInTheDocument()
  })

  it('resposta sem sucesso mostra a mensagem do backend', async () => {
    apiLogin.mockResolvedValue(respostaOk(null as unknown as LoginTokenDTO, { success: false, errorMessage: 'Usuário bloqueado.' }))
    renderizar()
    await preencherEEntrar()
    expect(await screen.findByText('Usuário bloqueado.')).toBeInTheDocument()
    expect(localStorage.getItem('accessToken')).toBeNull()
  })

  it('resposta sem tokens mostra "Credenciais inválidas."', async () => {
    apiLogin.mockResolvedValue(respostaOk({ token: '', refreshToken: '', expiration: '' }))
    renderizar()
    await preencherEEntrar()
    expect(await screen.findByText('Credenciais inválidas.')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Entrar' })).toBeEnabled()
  })

  it('erro HTTP mostra a mensagem do backend', async () => {
    apiLogin.mockRejectedValue(erroHttp(401, { errorMessage: 'E-mail ou senha incorretos.' }))
    renderizar()
    await preencherEEntrar()
    expect(await screen.findByText('E-mail ou senha incorretos.')).toBeInTheDocument()
  })

  it('erro de rede mostra a mensagem genérica', async () => {
    apiLogin.mockRejectedValue(new Error('Network Error'))
    renderizar()
    await preencherEEntrar()
    expect(await screen.findByText('Erro ao fazer login. Verifique sua conexão.')).toBeInTheDocument()
  })

  it('um novo envio apaga o erro anterior', async () => {
    apiLogin.mockRejectedValueOnce(new Error('Network Error')).mockResolvedValueOnce(respostaOk(tokens()))
    renderizar()
    await preencherEEntrar()
    await screen.findByText('Erro ao fazer login. Verifique sua conexão.')

    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
    expect(await screen.findByText('Tela de ambientes')).toBeInTheDocument()
  })

  it('campos obrigatórios vazios não chamam a API', async () => {
    renderizar()
    await userEvent.click(screen.getByRole('button', { name: 'Entrar' }))
    expect(apiLogin).not.toHaveBeenCalled()
  })

  describe('demonstração', () => {
    it('entra direto na tela inicial', async () => {
      const t = tokens()
      apiDemo.mockResolvedValue(respostaOk(t))
      renderizar()

      await userEvent.click(screen.getByRole('button', { name: 'Acessar demonstração' }))

      expect(await screen.findByText('Tela inicial')).toBeInTheDocument()
      expect(localStorage.getItem('accessToken')).toBe(t.token)
      expect(apiLogin).not.toHaveBeenCalled()
    })

    it('resposta sem sucesso mostra a mensagem do backend ou a padrão', async () => {
      apiDemo.mockResolvedValueOnce(respostaOk(null as unknown as LoginTokenDTO, { success: false, errorMessage: 'Demo desativada.' }))
      renderizar()
      await userEvent.click(screen.getByRole('button', { name: 'Acessar demonstração' }))
      expect(await screen.findByText('Demo desativada.')).toBeInTheDocument()

      apiDemo.mockResolvedValueOnce(respostaOk(null as unknown as LoginTokenDTO, { success: false, errorMessage: '' }))
      await userEvent.click(screen.getByRole('button', { name: 'Acessar demonstração' }))
      expect(await screen.findByText('Não foi possível acessar a demonstração.')).toBeInTheDocument()
    })

    it('erro HTTP mostra a mensagem do backend', async () => {
      apiDemo.mockRejectedValue(erroHttp(503, { errorMessage: 'Demonstração em manutenção.' }))
      renderizar()
      await userEvent.click(screen.getByRole('button', { name: 'Acessar demonstração' }))
      expect(await screen.findByText('Demonstração em manutenção.')).toBeInTheDocument()
    })

    it('erro de rede mostra a mensagem genérica e libera o botão', async () => {
      apiDemo.mockRejectedValue(new Error('Network Error'))
      renderizar()
      await userEvent.click(screen.getByRole('button', { name: 'Acessar demonstração' }))
      expect(await screen.findByText('Erro ao acessar a demonstração. Tente novamente.')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Acessar demonstração' })).toBeEnabled()
    })
  })
})
