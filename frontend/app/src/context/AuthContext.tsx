import { createContext, useContext, useState, useCallback, type ReactNode } from 'react'
import { jwtDecode } from '../utils/jwt'

interface AuthState {
  accessToken: string | null
  refreshToken: string | null
  ambienteId: string | null
  nome: string | null
  email: string | null
}

interface AuthContextValue extends AuthState {
  login: (accessToken: string, refreshToken: string) => void
  setAmbienteToken: (accessToken: string, refreshToken: string) => void
  logout: () => void
  isAuthenticated: boolean
  hasAmbiente: boolean
}

const AuthContext = createContext<AuthContextValue | null>(null)

function parseTokenState(accessToken: string): Partial<AuthState> {
  try {
    const decoded = jwtDecode(accessToken)
    return {
      ambienteId: decoded['AmbienteId'] ?? decoded['ambienteId'] ?? null,
      nome: decoded['name'] ?? decoded['Nome'] ?? decoded['unique_name'] ?? null,
      email: decoded['email'] ?? decoded['Email'] ?? null,
    }
  } catch {
    return {}
  }
}

function loadInitialState(): AuthState {
  const accessToken = localStorage.getItem('accessToken')
  const refreshToken = localStorage.getItem('refreshToken')
  if (!accessToken) return { accessToken: null, refreshToken: null, ambienteId: null, nome: null, email: null }
  return { accessToken, refreshToken, ...parseTokenState(accessToken) }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(loadInitialState)

  const login = useCallback((accessToken: string, refreshToken: string) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    setState({ accessToken, refreshToken, ...parseTokenState(accessToken) })
  }, [])

  const setAmbienteToken = useCallback((accessToken: string, refreshToken: string) => {
    localStorage.setItem('accessToken', accessToken)
    localStorage.setItem('refreshToken', refreshToken)
    setState({ accessToken, refreshToken, ...parseTokenState(accessToken) })
  }, [])

  const logout = useCallback(() => {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    setState({ accessToken: null, refreshToken: null, ambienteId: null, nome: null, email: null })
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
