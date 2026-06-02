import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { AuthProvider, useAuth } from './context/AuthContext'
import { ThemeProvider } from './context/ThemeContext'
import Layout from './components/Layout'
import Login from './pages/Login'
import Register from './pages/Register'
import SelecionarAmbiente from './pages/SelecionarAmbiente'
import Dashboard from './pages/Dashboard'
import Contas from './pages/Contas'
import Categorias from './pages/Categorias'
import Transacoes from './pages/Transacoes'
import Orcamentos from './pages/Orcamentos'
import Configuracoes from './pages/Configuracoes'
import Admin from './pages/Admin'

const qc = new QueryClient()

function AppRoutes() {
  const { isAuthenticated, hasAmbiente, ambienteId } = useAuth()

  if (!isAuthenticated) {
    return (
      <Routes>
        <Route path="/login" element={<Login />} />
        <Route path="/register" element={<Register />} />
        <Route path="*" element={<Navigate to="/login" replace />} />
      </Routes>
    )
  }

  if (!hasAmbiente) {
    return (
      <Routes>
        <Route path="/selecionar-ambiente" element={<SelecionarAmbiente />} />
        <Route path="*" element={<Navigate to="/selecionar-ambiente" replace />} />
      </Routes>
    )
  }

  return (
    <Routes>
      <Route element={<Layout key={ambienteId} />}>
        <Route path="/" element={<Dashboard />} />
        <Route path="/contas" element={<Contas />} />
        <Route path="/categorias" element={<Categorias />} />
        <Route path="/transacoes" element={<Transacoes />} />
        <Route path="/orcamentos" element={<Orcamentos />} />
        <Route path="/configuracoes" element={<Configuracoes />} />
        <Route path="/admin" element={<Admin />} />
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}

export default function App() {
  return (
    <ThemeProvider>
      <QueryClientProvider client={qc}>
        <BrowserRouter>
          <AuthProvider>
            <AppRoutes />
          </AuthProvider>
        </BrowserRouter>
      </QueryClientProvider>
    </ThemeProvider>
  )
}
