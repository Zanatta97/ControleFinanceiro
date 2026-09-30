import { useState } from 'react'
import { comparativoCategoria } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency } from '../../utils/format'
import { BarraFiltros, SeletorCompetencia } from './filtros'
import { CorCategoria, EstadoView, Kpi, Secao, VariacaoChip, tbodyCls, td, th, theadRow, trCls } from './ui'
import { labelCompetencia, mesAtual, somarMeses } from './utils'

// Variação dos totais, calculada aqui; nula quando a base é zero, como no backend
function variacao(atual: number, base: number): number | null {
  return base > 0 ? ((atual - base) / base) * 100 : null
}

export default function ComparativoCategoria() {
  const [{ mes, ano }, setCompetencia] = useState(mesAtual)
  const estado = useRelatorio(() => comparativoCategoria(mes, ano), `${mes}-${ano}`)
  const anterior = somarMeses(mes, ano, -1)

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <SeletorCompetencia mes={mes} ano={ano} onChange={(m, a) => setCompetencia({ mes: m, ano: a })} />
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Nenhuma despesa encontrada no período.">
        {(dados) => {
          const totalMes = dados.reduce((acc, c) => acc + c.gastoMes, 0)
          const totalAnterior = dados.reduce((acc, c) => acc + c.gastoMesAnterior, 0)
          const totalMedia = dados.reduce((acc, c) => acc + c.mediaTresMesesAnteriores, 0)
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
                <Kpi titulo="Gasto no mês" valor={formatCurrency(totalMes)} icone="lucide:calendar" tom="marca" />
                <Kpi
                  titulo="Mês anterior"
                  valor={formatCurrency(totalAnterior)}
                  icone="lucide:calendar-minus"
                  detalhe={
                    <span className="inline-flex items-center gap-1.5">
                      Mês atual vs. anterior <VariacaoChip valor={variacao(totalMes, totalAnterior)} />
                    </span>
                  }
                />
                <Kpi
                  titulo="Média de 3 meses"
                  valor={formatCurrency(totalMedia)}
                  icone="lucide:sigma"
                  detalhe={
                    <span className="inline-flex items-center gap-1.5">
                      Mês atual vs. média <VariacaoChip valor={variacao(totalMes, totalMedia)} />
                    </span>
                  }
                />
              </div>

              <Secao
                titulo="Comparativo por categoria"
                descricao={`${labelCompetencia(mes, ano)} × ${labelCompetencia(anterior.mes, anterior.ano)} × média dos 3 meses anteriores. "—" indica base zero.`}
              >
                <div className="scrollbar-fino overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className={theadRow}>
                        <th className={th}>Categoria</th>
                        <th className={`${th} text-right`}>Mês</th>
                        <th className={`${th} text-right`}>Mês anterior</th>
                        <th className={`${th} text-right`}>Variação</th>
                        <th className={`${th} text-right`}>Média 3 meses</th>
                        <th className={`${th} text-right`}>Variação</th>
                      </tr>
                    </thead>
                    <tbody className={tbodyCls}>
                      {dados.map((c) => (
                        <tr key={c.categoriaId} className={trCls}>
                          <td className={td}>
                            <span className="flex items-center gap-2.5 font-medium text-fin-text-primary">
                              <CorCategoria cor={c.cor} />
                              {c.nomeCategoria}
                            </span>
                          </td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-primary`}>{formatCurrency(c.gastoMes)}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>{formatCurrency(c.gastoMesAnterior)}</td>
                          <td className={`${td} text-right`}><VariacaoChip valor={c.variacaoMesAnterior} /></td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>
                            {formatCurrency(c.mediaTresMesesAnteriores)}
                          </td>
                          <td className={`${td} text-right`}><VariacaoChip valor={c.variacaoMedia} /></td>
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
