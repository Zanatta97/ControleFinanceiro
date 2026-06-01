import { NavLink } from 'react-router-dom'

const links = [
  { to: '/', label: 'Dashboard', icon: '📊' },
  { to: '/contas', label: 'Contas', icon: '🏦' },
  { to: '/categorias', label: 'Categorias', icon: '🏷️' },
  { to: '/transacoes', label: 'Transações', icon: '💸' },
  { to: '/orcamentos', label: 'Orçamentos', icon: '📋' },
  { to: '/configuracoes', label: 'Configurações', icon: '⚙️' },
]

export default function Sidebar() {
  return (
    <aside className="flex h-full w-56 flex-col border-r border-gray-200 bg-white">
      <div className="flex h-16 items-center gap-2 border-b px-5">
        <span className="text-xl">💰</span>
        <span className="font-bold text-gray-800 text-sm">Controle Financeiro</span>
      </div>
      <nav className="flex-1 overflow-y-auto px-3 py-4 space-y-1">
        {links.map((l) => (
          <NavLink
            key={l.to}
            to={l.to}
            end={l.to === '/'}
            className={({ isActive }) =>
              `flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
                isActive
                  ? 'bg-green-50 text-green-700'
                  : 'text-gray-600 hover:bg-gray-100'
              }`
            }
          >
            <span className="text-base">{l.icon}</span>
            {l.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  )
}
