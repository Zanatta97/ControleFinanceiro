import { useState } from 'react'
import { gastoPorCategoriaFiltrado } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency, formatPercent } from '../../utils/format'
import Donut from '../../components/ui/Donut'
import { BarraFiltros, FiltroPeriodo } from './filtros'
import { BarraProporcao, CorCategoria, EstadoView, Secao } from './ui'
import { chaveFiltro, corNeutra, descricaoFiltro, erroPeriodo, filtroInicial, filtroParaParams } from './utils'

export default function PorCategoria() {
  const [filtro, setFiltro] = useState(filtroInicial)
  const estado = useRelatorio(() => gastoPorCategoriaFiltrado(filtroParaParams(filtro)), chaveFiltro(filtro), {
    habilitado: !erroPeriodo(filtro),
  })

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <FiltroPeriodo valor={filtro} onChange={setFiltro} />
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Nenhuma despesa encontrada no período.">
        {(dados) => {
          const total = dados.reduce((acc, c) => acc + c.totalGasto, 0)
          return (
            <Secao titulo="Gastos por categoria" descricao={descricaoFiltro(filtro)}>
              <div className="flex flex-col items-center gap-6 px-5 py-[18px] md:flex-row md:items-start">
                <Donut size={170} segments={dados.map((c) => ({ value: c.totalGasto, cor: c.cor ?? corNeutra }))}>
                  <span className="text-[9.5px] font-semibold uppercase text-fin-text-muted">Total</span>
                  <span className="font-fin-mono text-[14px] text-fin-text-primary">{formatCurrency(total)}</span>
                </Donut>
                <div className="flex w-full flex-1 flex-col gap-3.5">
                  {dados.map((c) => (
                    <div key={c.categoriaId}>
                      <div className="mb-1.5 flex items-center gap-2.5">
                        <CorCategoria cor={c.cor} />
                        <span className="flex-1 truncate text-[13px] font-medium text-fin-text-primary">{c.nomeCategoria}</span>
                        <span className="text-[11.5px] text-fin-text-muted">{formatPercent(c.percentual)}</span>
                        <span className="min-w-[96px] text-right font-fin-mono text-[12.5px] text-fin-text-primary">
                          {formatCurrency(c.totalGasto)}
                        </span>
                      </div>
                      <BarraProporcao pct={c.percentual} cor={c.cor ?? corNeutra} />
                    </div>
                  ))}
                </div>
              </div>
            </Secao>
          )
        }}
      </EstadoView>
    </div>
  )
}
