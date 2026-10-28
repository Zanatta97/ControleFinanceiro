import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { jwtDecode } from '../utils/jwt'

/**
 * Sessão do usuário. Tudo, exceto `nomeAmbiente`, sai do access token: ambiente, nome, e-mail, id e roles
 * são claims do JWT. `nomeAmbiente` não está no token e é guardado à parte no localStorage.
 */
interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  /** Claim `ambiente_id`; null até o usuário selecionar um ambiente. */
  ambienteId: string | null
  nomeAmbiente: string | null
  nome: string | null
  email: string | null
  userId: string | null
  /** Roles do Identity (Admin, User, Demo) — não o papel dentro do ambiente. */
  roles: string[]
}

interface AuthContextValue extends AuthState {
  /** Guarda os tokens do login. Limpa o nome do ambiente guardado no estado (o do localStorage fica). */
  login: (accessToken: string, refreshToken: string) => void
  /**
   * Troca os tokens pelos devolvidos na seleção de ambiente. Sem `nomeAmbiente`, o nome é apagado e a
   * Navbar o busca de novo na API.
   */
  setAmbienteToken: (accessToken: string, refreshToken: string, nomeAmbiente?: string | null) => void
  /** Apaga tokens e nome do ambiente. Não avisa o backend (o refresh token continua válido até expirar). */
  logout: () => void
  isAuthenticated: boolean
  /** Token já tem `ambiente_id`; sem ele o App só mostra a seleção de ambiente. */
  hasAmbiente: boolean
  isAdmin: boolean
  /** Sessão do modo demonstração: Configurações e Admin ficam fora das rotas. */
  isDemo: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

/**
 * Lê os claims do access token sem validar a assinatura (quem valida é o backend).
 * Token ilegível vira estado vazio, sem lançar erro.
 */
function parseTokenState(accessToken: string): Partial<AuthState> {
  try {
    const decoded = jwtDecode(accessToken)
    // Aceita o claim de role com o nome longo do .NET ou o curto ("role"/"roles"). Com várias roles
    // o JWT traz um array; com uma só, uma string.
    const roleRaw = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? decoded['role'] ?? decoded['roles']
    const roles: string[] = Array.isArray(roleRaw) ? roleRaw : roleRaw ? [roleRaw] : []
    return {
      ambienteId: decoded['ambiente_id'] ?? decoded['AmbienteId'] ?? decoded['ambienteId'] ?? null,
      nome: decoded['name'] ?? decoded['Nome'] ?? decoded['unique_name'] ?? null,
      email: decoded['email'] ?? decoded['Email'] ?? null,
      userId: decoded['id'] ?? null,
      roles,
    }
  } catch {
    return {}
  }
}

const EMPTY_STATE: AuthState = { accessToken: null, refreshToken: null, ambienteId: null, nomeAmbiente: null, nome: null, email: null, userId: null, roles: [] }

/** Restaura a sessão do localStorage ao abrir/recarregar o app; não confere se o token ainda é válido. */
function loadInitialState(): AuthState {
  const accessToken = localStorage.getItem('accessToken')
  const refreshToken = localStorage.getItem('refreshToken')
  const nomeAmbiente = localStorage.getItem('nomeAmbiente')
  if (!accessToken) return { ...EMPTY_STATE }
  return { ...EMPTY_STATE, accessToken, refreshToken, nomeAmbiente, ...parseTokenState(accessToken) }
}

/**
 * Provedor da sessão. Mantém o estado em React e espelha os tokens no localStorage, de onde o
 * cliente HTTP (api/client.ts) os lê. O refresh automático grava direto no localStorage e não passa
 * por aqui: o `accessToken` deste estado pode ficar defasado, mas os claims do token novo são os mesmos.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(loadInitialState)

  const login = useCallback((accessToken: string, refreshToken: string) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    setState({ ...EMPTY_STATE, accessToken, refreshToken, ...parseTokenState(accessToken) })
  }, [])

  const setAmbienteToken = useCallback((accessToken: string, refreshToken: string, nomeAmbiente?: string | null) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    if (nomeAmbiente != null) localStorage.setItem('nomeAmbiente', nomeAmbiente)
    else localStorage.removeItem('nomeAmbiente')
    setState({ ...EMPTY_STATE, accessToken, refreshToken, nomeAmbiente: nomeAmbiente ?? null, ...parseTokenState(accessToken) })
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('nomeAmbiente')
    setState({ ...EMPTY_STATE })
  }, [])

  return (
    <AuthContext.Provider
      value={{
        ...state,
        login,
        setAmbienteToken,
        logout,
        isAuthenticated: !!state.accessToken,
        hasAmbiente: !!state.ambienteId,
        isAdmin: state.roles.includes('Admin'),
        isDemo: state.roles.includes('Demo'),
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

/**
 * Acesso à sessão do usuário.
 * @throws se usado fora de `AuthProvider`.
 */
export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
