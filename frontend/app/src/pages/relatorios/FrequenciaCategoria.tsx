import { useState } from 'react'
import { frequenciaCategoria } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency } from '../../utils/format'
import { BarraFiltros, FiltroPeriodo } from './filtros'
import { BarraProporcao, CorCategoria, EstadoView, Kpi, Secao, tbodyCls, td, th, theadRow, trCls } from './ui'
import { chaveFiltro, corNeutra, descricaoFiltro, erroPeriodo, filtroInicial, filtroParaParams } from './utils'

export default function FrequenciaCategoria() {
  const [filtro, setFiltro] = useState(filtroInicial)
  const estado = useRelatorio(() => frequenciaCategoria(filtroParaParams(filtro)), chaveFiltro(filtro), {
    habilitado: !erroPeriodo(filtro),
  })

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <FiltroPeriodo valor={filtro} onChange={setFiltro} />
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Nenhuma despesa encontrada no período.">
        {(dados) => {
          const quantidadeTotal = dados.reduce((acc, c) => acc + c.quantidade, 0)
          const total = dados.reduce((acc, c) => acc + c.totalGasto, 0)
          const maiorQuantidade = Math.max(...dados.map((c) => c.quantidade))
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
                <Kpi titulo="Despesas lançadas" valor={String(quantidadeTotal)} icone="lucide:receipt" tom="marca" />
                <Kpi titulo="Total gasto" valor={formatCurrency(total)} icone="lucide:arrow-up-circle" tom="negativo" />
                <Kpi
                  titulo="Ticket médio geral"
                  valor={quantidadeTotal > 0 ? formatCurrency(total / quantidadeTotal) : '—'}
                  icone="lucide:divide"
                  tom="invest"
                />
              </div>

              <Secao titulo="Frequência e ticket médio por categoria" descricao={descricaoFiltro(filtro)}>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className={theadRow}>
                        <th className={th}>Categoria</th>
                        <th className={`${th} w-[30%]`}>Frequência</th>
                        <th className={`${th} text-right`}>Qtde.</th>
                        <th className={`${th} text-right`}>Total</th>
                        <th className={`${th} text-right`}>Ticket médio</th>
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
                          <td className={td}>
                            <BarraProporcao pct={maiorQuantidade > 0 ? (c.quantidade / maiorQuantidade) * 100 : 0} cor={c.cor ?? corNeutra} />
                          </td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-primary`}>{c.quantidade}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>{formatCurrency(c.totalGasto)}</td>
                          <td className={`${td} text-right font-fin-mono font-semibold text-fin-text-primary`}>{formatCurrency(c.ticketMedio)}</td>
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
