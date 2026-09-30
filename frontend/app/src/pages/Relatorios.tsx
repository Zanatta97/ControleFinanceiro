import { useState, type ReactNode } from 'react'
import { Link, NavLink, Navigate, useParams } from 'react-router-dom'
import { Icon } from '@iconify/react'
import { abaPadrao, abas, grupos } from './relatorios/abas'
import type { SelecaoCategorias } from './relatorios/utils'
import PorCategoria from './relatorios/PorCategoria'
import PorConta from './relatorios/PorConta'
import ComparativoCategoria from './relatorios/ComparativoCategoria'
import MaioresDespesas from './relatorios/MaioresDespesas'
import MatrizCategoriaMes from './relatorios/MatrizCategoriaMes'
import FrequenciaCategoria from './relatorios/FrequenciaCategoria'
import RitmoMes from './relatorios/RitmoMes'
import Fatura from './relatorios/Fatura'
import ProjecaoParcelas from './relatorios/ProjecaoParcelas'
import ProjecaoProximoMes from './relatorios/ProjecaoProximoMes'
import FixosRecebimentos from './relatorios/FixosRecebimentos'
import EvolucaoMensal from './relatorios/EvolucaoMensal'
import StatusOrcamentos from './relatorios/StatusOrcamentos'
import Extrato from './relatorios/Extrato'

export default function Relatorios() {
  const { aba: slug } = useParams()
  // Seleção de categorias compartilhada entre "Próximo mês" e "Fixos × recebimentos";
  // fica aqui para sobreviver à troca de subaba
  const [selecao, setSelecao] = useState<SelecaoCategorias>({ fixas: [], recebimento: [] })

  const aba = abas.find((a) => a.slug === slug)
  // /relatorios sem subaba, ou com uma subaba que não existe, cai na primeira
  if (!aba) return <Navigate to={`/relatorios/${abaPadrao}`} replace />

  const conteudo: Record<string, ReactNode> = {
    'por-categoria': <PorCategoria />,
    'por-conta': <PorConta />,
    'comparativo-categoria': <ComparativoCategoria />,
    'maiores-despesas': <MaioresDespesas />,
    'matriz-categoria-mes': <MatrizCategoriaMes />,
    'frequencia-categoria': <FrequenciaCategoria />,
    'ritmo-mes': <RitmoMes />,
    fatura: <Fatura />,
    'projecao-parcelas': <ProjecaoParcelas />,
    'projecao-proximo-mes': <ProjecaoProximoMes selecao={selecao} onSelecao={setSelecao} />,
    'fixos-x-recebimentos': <FixosRecebimentos selecao={selecao} onSelecao={setSelecao} />,
    'evolucao-mensal': <EvolucaoMensal />,
    orcamentos: <StatusOrcamentos />,
    extrato: <Extrato />,
  }

  const abasDoGrupo = abas.filter((a) => a.grupo === aba.grupo)

  return (
    <div className="space-y-5">
      <div>
        <h1 className="text-[22px] font-bold text-fin-text-primary">Relatórios</h1>
        <p className="text-sm text-fin-text-secondary">{aba.descricao}</p>
      </div>

      {/* Grupos: levam à primeira subaba do grupo */}
      <div className="flex gap-1 overflow-x-auto rounded-[11px] border border-fin-border bg-fin-surface p-1">
        {grupos.map((g) => {
          const ativo = g.id === aba.grupo
          const primeira = abas.find((a) => a.grupo === g.id)!
          return (
            <Link
              key={g.id}
              to={`/relatorios/${primeira.slug}`}
              aria-current={ativo ? 'page' : undefined}
              className={`flex flex-none items-center gap-2 whitespace-nowrap rounded-[8px] px-3 py-2 text-[13px] font-semibold transition-colors ${
                ativo ? 'bg-fin-brand-soft text-fin-brand' : 'text-fin-text-secondary hover:bg-fin-surface-2'
              }`}
            >
              <Icon icon={g.icone} width={16} height={16} />
              {g.rotulo}
            </Link>
          )
        })}
      </div>

      {/* Subabas do grupo ativo */}
      <nav aria-label="Relatórios" className="-mt-1 flex gap-1 overflow-x-auto border-b border-fin-border">
        {abasDoGrupo.map((a) => (
          <NavLink
            key={a.slug}
            to={`/relatorios/${a.slug}`}
            className={({ isActive }) =>
              `-mb-px flex flex-none items-center gap-1.5 whitespace-nowrap border-b-2 px-3 py-2.5 text-[13px] font-semibold transition-colors ${
                isActive
                  ? 'border-fin-brand text-fin-brand'
                  : 'border-transparent text-fin-text-muted hover:text-fin-text-primary'
              }`
            }
          >
            <Icon icon={a.icone} width={15} height={15} />
            {a.rotulo}
          </NavLink>
        ))}
      </nav>

      {/* key: cada subaba começa com seus próprios filtros */}
      <div key={aba.slug}>{conteudo[aba.slug]}</div>
    </div>
  )
}
