import { NavLink } from 'react-router-dom'
import { Icon } from '@iconify/react'

const tabs = [
  { to: '/', label: 'Início', icon: 'lucide:layout-dashboard', end: true },
  { to: '/transacoes', label: 'Transações', icon: 'lucide:arrow-left-right', end: false },
  { to: '/contas', label: 'Contas', icon: 'lucide:credit-card', end: false },
  { to: '/categorias', label: 'Categorias', icon: 'lucide:tag', end: false },
  { to: '/orcamentos', label: 'Orçam.', icon: 'lucide:target', end: false },
]

export default function MobileTabBar() {
  return (
    <nav className="flex h-[62px] flex-none items-center justify-around border-t border-fin-border bg-fin-surface pb-1.5 lg:hidden">
      {tabs.map((t) => (
        <NavLink
          key={t.to}
          to={t.to}
          end={t.end}
          className={({ isActive }) =>
            `flex flex-1 flex-col items-center justify-center gap-1 text-[10.5px] font-semibold transition-colors ${
              isActive ? 'text-fin-brand' : 'text-fin-text-muted'
            }`
          }
        >
          <Icon icon={t.icon} width={21} height={21} />
          {t.label}
        </NavLink>
      ))}
    </nav>
  )
}
