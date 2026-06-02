import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { jwtDecode } from '../utils/jwt'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  ambienteId: string | null
  nomeAmbiente: string | null
  nome: string | null
  email: string | null
  userId: string | null
  roles: string[]
}

interface AuthContextValue extends AuthState {
  login: (accessToken: string, refreshToken: string) => void
  setAmbienteToken: (accessToken: string, refreshToken: string, nomeAmbiente?: string | null) => void
  logout: () => void
  isAuthenticated: boolean
  hasAmbiente: boolean
  isAdmin: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

function parseTokenState(accessToken: string): Partial<AuthState> {
  try {
    const decoded = jwtDecode(accessToken)
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

function loadInitialState(): AuthState {
  const accessToken = localStorage.getItem('accessToken')
  const refreshToken = localStorage.getItem('refreshToken')
  const nomeAmbiente = localStorage.getItem('nomeAmbiente')
  if (!accessToken) return { ...EMPTY_STATE }
  return { ...EMPTY_STATE, accessToken, refreshToken, nomeAmbiente, ...parseTokenState(accessToken) }
}

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
      }}
    >
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth must be used within AuthProvider')
  return ctx
}
