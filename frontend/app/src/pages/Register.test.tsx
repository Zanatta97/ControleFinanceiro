import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import Register from './Register'
import { register } from '../api/auth'
import { adiado, erroHttp, respostaOk } from '../test/helpers'

vi.mock('../api/auth', () => ({ register: vi.fn() }))

const apiRegister = vi.mocked(register)

function renderizar() {
  render(
    <MemoryRouter initialEntries={['/register']}>
      <Routes>
        <Route path="/register" element={<Register />} />
        <Route path="/login" element={<p>Tela de login</p>} />
      </Routes>
    </MemoryRouter>,
  )
}

async function preencher({ senha = 'segredo1', confirmacao = 'segredo1' } = {}) {
  await userEvent.type(screen.getByLabelText('Nome'), 'Ana')
  await userEvent.type(screen.getByLabelText('E-mail'), 'ana@exemplo.com')
  await userEvent.type(screen.getByLabelText('Senha'), senha)
  await userEvent.type(screen.getByLabelText('Confirmar Senha'), confirmacao)
}

const botaoCriar = () => screen.getByRole('button', { name: 'Criar Conta' })

describe('Register', () => {
  beforeEach(() => {
    apiRegister.mockReset()
  })

  it('renderiza os quatro campos obrigatórios e o link para entrar', () => {
    renderizar()
    for (const rotulo of ['Nome', 'E-mail', 'Senha', 'Confirmar Senha']) {
      expect(screen.getByLabelText(rotulo)).toBeRequired()
    }
    expect(screen.getByLabelText('Nome')).toHaveFocus()
    expect(screen.getByRole('link', { name: 'Entrar' })).toHaveAttribute('href', '/login')
  })

  it('senhas diferentes avisam sem chamar a API', async () => {
    renderizar()
    await preencher({ confirmacao: 'outra-senha' })
    await userEvent.click(botaoCriar())

    expect(screen.getByText('As senhas não coincidem.')).toBeInTheDocument()
    expect(apiRegister).not.toHaveBeenCalled()
  })

  it('cadastro certo envia o formulário, avisa e vai para o login', async () => {
    apiRegister.mockResolvedValue(respostaOk(null))
    renderizar()
    await preencher()
    await userEvent.click(botaoCriar())

    expect(apiRegister).toHaveBeenCalledWith({
      nome: 'Ana',
      email: 'ana@exemplo.com',
      senha: 'segredo1',
      confirmacaoSenha: 'segredo1',
    })
    expect(await screen.findByText('Conta criada! Redirecionando para o login...')).toBeInTheDocument()
    expect(await screen.findByText('Tela de login', {}, { timeout: 3000 })).toBeInTheDocument()
  })

  it('desabilita o botão enquanto envia', async () => {
    const pendente = adiado<ReturnType<typeof respostaOk<null>>>()
    apiRegister.mockReturnValue(pendente.promise)
    renderizar()
    await preencher()
    await userEvent.click(botaoCriar())

    expect(botaoCriar()).toBeDisabled()
    pendente.resolver(respostaOk(null, { success: false, errorMessage: 'x' }))
    expect(await screen.findByText('x')).toBeInTheDocument()
    expect(botaoCriar()).toBeEnabled()
  })

  it('resposta sem sucesso mostra a mensagem do backend', async () => {
    apiRegister.mockResolvedValue(respostaOk(null, { success: false, errorMessage: 'E-mail já cadastrado.' }))
    renderizar()
    await preencher()
    await userEvent.click(botaoCriar())
    expect(await screen.findByText('E-mail já cadastrado.')).toBeInTheDocument()
  })

  it('resposta sem sucesso e sem mensagem usa a padrão', async () => {
    apiRegister.mockResolvedValue(respostaOk(null, { success: false, errorMessage: '' }))
    renderizar()
    await preencher()
    await userEvent.click(botaoCriar())
    expect(await screen.findByText('Erro ao criar conta.')).toBeInTheDocument()
  })

  it('erro HTTP mostra a mensagem do backend', async () => {
    apiRegister.mockRejectedValue(erroHttp(400, { errorMessage: 'A senha deve ter ao menos 6 caracteres.' }))
    renderizar()
    await preencher()
    await userEvent.click(botaoCriar())
    expect(await screen.findByText('A senha deve ter ao menos 6 caracteres.')).toBeInTheDocument()
  })

  it('erro de rede mostra a mensagem padrão e não redireciona', async () => {
    apiRegister.mockRejectedValue(new Error('Network Error'))
    renderizar()
    await preencher()
    await userEvent.click(botaoCriar())
    expect(await screen.findByText('Erro ao criar conta.')).toBeInTheDocument()
    expect(screen.queryByText('Tela de login')).not.toBeInTheDocument()
  })

  it('corrigir a senha e reenviar apaga o erro anterior', async () => {
    apiRegister.mockResolvedValue(respostaOk(null))
    renderizar()
    await preencher({ confirmacao: 'errada' })
    await userEvent.click(botaoCriar())
    expect(screen.getByText('As senhas não coincidem.')).toBeInTheDocument()

    await userEvent.clear(screen.getByLabelText('Confirmar Senha'))
    await userEvent.type(screen.getByLabelText('Confirmar Senha'), 'segredo1')
    await userEvent.click(botaoCriar())
    expect(screen.queryByText('As senhas não coincidem.')).not.toBeInTheDocument()
    expect(await screen.findByText('Conta criada! Redirecionando para o login...')).toBeInTheDocument()
  })
})
