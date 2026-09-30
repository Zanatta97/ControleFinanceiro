import { useState } from 'react'
import { evolucaoMensal } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency } from '../../utils/format'
import { BarraFiltros, SeletorAno } from './filtros'
import { EstadoView, GraficoBarras, Kpi, Secao, tbodyCls, td, th, theadRow, trCls } from './ui'
import { mesAtual } from './utils'

export default function EvolucaoMensal() {
  const [ano, setAno] = useState(() => mesAtual().ano)
  const estado = useRelatorio(() => evolucaoMensal(ano), String(ano), {
    vazio: (d) => d.meses.every((m) => m.totalReceitas === 0 && m.totalDespesas === 0),
  })

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <SeletorAno ano={ano} onChange={setAno} />
      </BarraFiltros>

      <EstadoView estado={estado} vazio={`Nenhuma movimentação encontrada em ${ano}.`}>
        {(e) => {
          const receitas = e.meses.reduce((acc, m) => acc + m.totalReceitas, 0)
          const despesas = e.meses.reduce((acc, m) => acc + m.totalDespesas, 0)
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
                <Kpi titulo={`Receitas em ${e.ano}`} valor={formatCurrency(receitas)} icone="lucide:arrow-down-circle" tom="positivo" />
                <Kpi titulo={`Despesas em ${e.ano}`} valor={formatCurrency(despesas)} icone="lucide:arrow-up-circle" tom="negativo" />
                <Kpi titulo="Saldo do ano" valor={formatCurrency(receitas - despesas)} icone="lucide:scale" destaque />
              </div>

              <Secao titulo="Receitas × despesas por mês" descricao="Pela competência.">
                <GraficoBarras
                  series={[
                    { nome: 'Receitas', cor: 'var(--fin-positive)' },
                    { nome: 'Despesas', cor: 'var(--fin-negative)' },
                  ]}
                  itens={e.meses.map((m) => ({ rotulo: m.nomeMes.slice(0, 3), valores: [m.totalReceitas, m.totalDespesas] }))}
                  formatar={formatCurrency}
                />
                <div className="overflow-x-auto border-t border-fin-border">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className={theadRow}>
                        <th className={th}>Mês</th>
                        <th className={`${th} text-right`}>Receitas</th>
                        <th className={`${th} text-right`}>Despesas</th>
                        <th className={`${th} text-right`}>Cartão</th>
                        <th className={`${th} text-right`}>Contas</th>
                        <th className={`${th} text-right`}>Saldo</th>
                      </tr>
                    </thead>
                    <tbody className={tbodyCls}>
                      {e.meses.map((m) => (
                        <tr key={m.mes} className={trCls}>
                          <td className={`${td} capitalize text-fin-text-primary`}>{m.nomeMes}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-positive`}>{formatCurrency(m.totalReceitas)}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-negative`}>{formatCurrency(m.totalDespesas)}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>{formatCurrency(m.despesasCartao)}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>{formatCurrency(m.despesasOutras)}</td>
                          <td className={`${td} text-right font-fin-mono font-semibold ${m.saldo < 0 ? 'text-fin-negative' : 'text-fin-text-primary'}`}>
                            {formatCurrency(m.saldo)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </Secao>
            </>
          )
        }}
      </EstadoView>
    </div>
  )
}
