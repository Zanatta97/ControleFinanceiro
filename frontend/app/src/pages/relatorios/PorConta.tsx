import { useState } from 'react'
import { gastosPorConta } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { TipoConta } from '../../types/api'
import { formatCurrency, formatPercent } from '../../utils/format'
import { BarraFiltros, FiltroPeriodo } from './filtros'
import { BarraProporcao, EstadoView, Kpi, Secao } from './ui'
import { chaveFiltro, descricaoFiltro, erroPeriodo, filtroInicial, filtroParaParams, nomeTipoConta } from './utils'

export default function PorConta() {
  const [filtro, setFiltro] = useState(filtroInicial)
  const estado = useRelatorio(() => gastosPorConta(filtroParaParams(filtro)), chaveFiltro(filtro), {
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
          const cartao = dados
            .filter((c) => c.tipoConta === TipoConta.CartaoCredito)
            .reduce((acc, c) => acc + c.totalGasto, 0)
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
                <Kpi titulo="Total de despesas" valor={formatCurrency(total)} icone="lucide:arrow-up-circle" tom="negativo" />
                <Kpi titulo="Em cartão de crédito" valor={formatCurrency(cartao)} icone="lucide:credit-card" tom="invest" />
                <Kpi titulo="Nas demais contas" valor={formatCurrency(total - cartao)} icone="lucide:landmark" tom="marca" />
              </div>
              <Secao titulo="Gastos por conta" descricao={descricaoFiltro(filtro)}>
                <div className="flex flex-col gap-4 px-5 py-[18px]">
                  {dados.map((c) => {
                    const ehCartao = c.tipoConta === TipoConta.CartaoCredito
                    return (
                      <div key={c.contaId}>
                        <div className="mb-1.5 flex items-center gap-2.5">
                          <span className="flex-1 truncate text-[13px] font-medium text-fin-text-primary">{c.nomeConta}</span>
                          {c.tipoConta != null && (
                            <span className="hidden text-[11.5px] text-fin-text-muted sm:inline">{nomeTipoConta[c.tipoConta]}</span>
                          )}
                          <span className="min-w-[48px] text-right text-[11.5px] text-fin-text-muted">{formatPercent(c.percentual)}</span>
                          <span className="min-w-[96px] text-right font-fin-mono text-[12.5px] text-fin-text-primary">
                            {formatCurrency(c.totalGasto)}
                          </span>
                        </div>
                        <BarraProporcao pct={c.percentual} cor={ehCartao ? 'var(--fin-invest)' : 'var(--fin-brand)'} />
                      </div>
                    )
                  })}
                </div>
              </Secao>
            </>
          )
        }}
      </EstadoView>
    </div>
  )
}
