import { Outlet } from 'react-router-dom'
import Navbar from './Navbar'
import MobileTabBar from './MobileTabBar'

/** Estrutura das telas autenticadas: Navbar no topo, conteúdo rolável e MobileTabBar no celular. */
export default function Layout() {
  return (
    <div className="flex h-dvh flex-col overflow-hidden bg-fin-bg">
      <Navbar />
      <main className="flex-1 overflow-y-auto p-4 sm:p-6">
        <div className="mx-auto w-full max-w-[1060px]">
          <Outlet />
        </div>
      </main>
      <MobileTabBar />
    </div>
  )
}
