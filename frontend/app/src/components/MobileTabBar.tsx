import { NavLink } from 'react-router-dom'
import { Icon } from '@iconify/react'

const tabs = [
  { to: '/', label: 'Início', icon: 'lucide:layout-dashboard', end: true },
  { to: '/transacoes', label: 'Transações', icon: 'lucide:arrow-left-right', end: false },
  { to: '/contas', label: 'Contas', icon: 'lucide:credit-card', end: false },
  { to: '/categorias', label: 'Categorias', icon: 'lucide:tag', end: false },
  { to: '/orcamentos', label: 'Orçam.', icon: 'lucide:target', end: false },
  { to: '/relatorios', label: 'Relat.', icon: 'lucide:bar-chart-3', end: false },
]

/** Menu inferior de navegação, exibido abaixo de lg (no desktop o menu fica na Navbar). */
export default function MobileTabBar() {
  return (
    // Altura útil de 62px; a barra cresce para baixo só o tanto da barrinha do iPhone (safe-area)
    <nav className="flex h-[calc(62px+env(safe-area-inset-bottom))] flex-none items-center justify-around border-t border-fin-border bg-fin-surface pb-[calc(0.375rem+env(safe-area-inset-bottom))] lg:hidden">
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
