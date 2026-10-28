import { describe, expect, it, vi } from 'vitest'
import { act, render, renderHook, screen } from '@testing-library/react'
import type { ReactNode } from 'react'
import { AuthProvider, useAuth } from './AuthContext'
import { criarToken } from '../test/helpers'

const CLAIM_ROLE_MS = 'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'

const wrapper = ({ children }: { children: ReactNode }) => <AuthProvider>{children}</AuthProvider>

function usarAuth() {
  return renderHook(() => useAuth(), { wrapper })
}

describe('AuthContext', () => {
  it('sem token salvo, começa deslogado', () => {
    const { result } = usarAuth()
    expect(result.current).toMatchObject({
      accessToken: null,
      refreshToken: null,
      ambienteId: null,
      nomeAmbiente: null,
      nome: null,
      email: null,
      userId: null,
      roles: [],
      isAuthenticated: false,
      hasAmbiente: false,
      isAdmin: false,
      isDemo: false,
    })
  })

  it('restaura a sessão do localStorage lendo as claims do token', () => {
    const token = criarToken({
      ambiente_id: 'amb-1',
      name: 'Ana',
      email: 'ana@exemplo.com',
      id: 'user-1',
      [CLAIM_ROLE_MS]: 'Admin',
    })
    localStorage.setItem('accessToken', token)
    localStorage.setItem('refreshToken', 'r1')
    localStorage.setItem('nomeAmbiente', 'Casa')

    const { result } = usarAuth()
    expect(result.current).toMatchObject({
      accessToken: token,
      refreshToken: 'r1',
      nomeAmbiente: 'Casa',
      ambienteId: 'amb-1',
      nome: 'Ana',
      email: 'ana@exemplo.com',
      userId: 'user-1',
      roles: ['Admin'],
      isAuthenticated: true,
      hasAmbiente: true,
      isAdmin: true,
      isDemo: false,
    })
  })

  it('aceita os nomes alternativos das claims', () => {
    localStorage.setItem(
      'accessToken',
      criarToken({ AmbienteId: 'amb-2', unique_name: 'bia', Email: 'bia@exemplo.com', role: ['Demo', 'User'] }),
    )
    const { result } = usarAuth()
    expect(result.current).toMatchObject({
      ambienteId: 'amb-2',
      nome: 'bia',
      email: 'bia@exemplo.com',
      roles: ['Demo', 'User'],
      isDemo: true,
      isAdmin: false,
    })
  })

  it('prefere "Nome" a "unique_name" e lê "ambienteId" e "roles"', () => {
    localStorage.setItem(
      'accessToken',
      criarToken({ ambienteId: 'amb-3', Nome: 'Carla', unique_name: 'carla123', roles: 'Admin' }),
    )
    const { result } = usarAuth()
    expect(result.current).toMatchObject({ ambienteId: 'amb-3', nome: 'Carla', roles: ['Admin'], isAdmin: true })
  })

  it('token sem ambiente: autenticado, mas sem ambiente selecionado', () => {
    localStorage.setItem('accessToken', criarToken({ name: 'Ana' }))
    const { result } = usarAuth()
    expect(result.current.isAuthenticated).toBe(true)
    expect(result.current.hasAmbiente).toBe(false)
  })

  it('token ilegível não derruba o app: autenticado e sem claims', () => {
    localStorage.setItem('accessToken', 'isso-nao-e-um-jwt')
    const { result } = usarAuth()
    expect(result.current.isAuthenticated).toBe(true)
    expect(result.current.nome).toBeNull()
    expect(result.current.roles).toEqual([])
  })

  it('login() salva os tokens e atualiza o estado', () => {
    const { result } = usarAuth()
    const token = criarToken({ name: 'Ana', email: 'ana@exemplo.com' })

    act(() => result.current.login(token, 'r1'))

    expect(localStorage.getItem('accessToken')).toBe(token)
    expect(localStorage.getItem('refreshToken')).toBe('r1')
    expect(result.current).toMatchObject({ isAuthenticated: true, nome: 'Ana', email: 'ana@exemplo.com' })
  })

  it('login() descarta o ambiente da sessão anterior', () => {
    localStorage.setItem('accessToken', criarToken({ ambiente_id: 'amb-1' }))
    localStorage.setItem('nomeAmbiente', 'Casa')
    const { result } = usarAuth()

    act(() => result.current.login(criarToken({ name: 'Outra' }), 'r2'))

    expect(result.current.ambienteId).toBeNull()
    expect(result.current.nomeAmbiente).toBeNull()
  })

  it('setAmbienteToken() troca o token e guarda o nome do ambiente', () => {
    const { result } = usarAuth()
    const token = criarToken({ ambiente_id: 'amb-9', name: 'Ana' })

    act(() => result.current.setAmbienteToken(token, 'r9', 'Empresa'))

    expect(localStorage.getItem('accessToken')).toBe(token)
    expect(localStorage.getItem('refreshToken')).toBe('r9')
    expect(localStorage.getItem('nomeAmbiente')).toBe('Empresa')
    expect(result.current).toMatchObject({ ambienteId: 'amb-9', nomeAmbiente: 'Empresa', hasAmbiente: true })
  })

  it('setAmbienteToken() sem nome remove o nome salvo', () => {
    localStorage.setItem('nomeAmbiente', 'Antigo')
    const { result } = usarAuth()

    act(() => result.current.setAmbienteToken(criarToken({ ambiente_id: 'amb-9' }), 'r9'))

    expect(localStorage.getItem('nomeAmbiente')).toBeNull()
    expect(result.current.nomeAmbiente).toBeNull()
  })

  it('logout() apaga só as chaves da sessão', () => {
    localStorage.setItem('accessToken', criarToken({ ambiente_id: 'amb-1' }))
    localStorage.setItem('refreshToken', 'r1')
    localStorage.setItem('nomeAmbiente', 'Casa')
    localStorage.setItem('theme', 'dark')
    const { result } = usarAuth()

    act(() => result.current.logout())

    expect(localStorage.getItem('accessToken')).toBeNull()
    expect(localStorage.getItem('refreshToken')).toBeNull()
    expect(localStorage.getItem('nomeAmbiente')).toBeNull()
    expect(localStorage.getItem('theme')).toBe('dark')
    expect(result.current).toMatchObject({ isAuthenticated: false, hasAmbiente: false, roles: [] })
  })

  it('expõe o estado para os componentes filhos', () => {
    localStorage.setItem('accessToken', criarToken({ name: 'Ana' }))
    function Saudacao() {
      const { nome } = useAuth()
      return <p>Olá, {nome}</p>
    }
    render(<AuthProvider><Saudacao /></AuthProvider>)
    expect(screen.getByText('Olá, Ana')).toBeInTheDocument()
  })

  it('useAuth fora do AuthProvider lança erro', () => {
    vi.spyOn(console, 'error').mockImplementation(() => {})
    expect(() => renderHook(() => useAuth())).toThrow('useAuth must be used within AuthProvider')
  })
})
