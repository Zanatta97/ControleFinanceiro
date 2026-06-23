import { NavLink } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const links = [
  { to: '/', label: 'Dashboard', icon: '📊' },
  { to: '/contas', label: 'Contas', icon: '🏦' },
  { to: '/categorias', label: 'Categorias', icon: '🏷️' },
  { to: '/transacoes', label: 'Transações', icon: '💸' },
  { to: '/orcamentos', label: 'Orçamentos', icon: '📋' },
  { to: '/configuracoes', label: 'Configurações', icon: '⚙️' },
]

export default function Sidebar() {
  const { isAdmin, isDemo } = useAuth()

  // No modo demonstração, Configurações fica indisponível (só os cadastros)
  const visibleLinks = isDemo ? links.filter((l) => l.to !== '/configuracoes') : links

  return (
    <aside className="flex h-full w-56 flex-col border-r border-fin-border bg-fin-surface">
      <div className="flex h-16 items-center gap-2 border-b border-fin-border px-5">
        <span className="text-xl">💰</span>
        <span className="font-bold text-fin-text-primary text-sm">Controle Financeiro</span>
      </div>
      <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
        {visibleLinks.map((l) => (
          <NavLink
            key={l.to}
            to={l.to}
            end={l.to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-fin-brand-soft text-fin-brand'
                  : 'text-fin-text-secondary hover:bg-fin-ghost-hover hover:text-fin-brand'
              }`
            }
          >
            <span className="text-base">{l.icon}</span>
            {l.label}
          </NavLink>
        ))}

        {isAdmin && (
          <>
            <div className="my-2 border-t border-fin-border" />
            <NavLink
              to="/admin"
              className={({ isActive }) =>
                `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                  isActive
                    ? 'bg-fin-invest-soft text-fin-invest'
                    : 'text-fin-text-secondary hover:bg-fin-ghost-hover hover:text-fin-brand'
                }`
              }
            >
              <span className="text-base">🛡️</span>
              Administração
            </NavLink>
          </>
        )}
      </nav>
    </aside>
  )
}
