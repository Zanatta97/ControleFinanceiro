import { ritmoMes } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency, formatPercent } from '../../utils/format'
import { BarraProporcao, EstadoView, Kpi, Secao, VariacaoChip } from './ui'

function Linha({ rotulo, valor, maximo, cor }: { rotulo: string; valor: number; maximo: number; cor: string }) {
  return (
    <div>
      <div className="mb-1.5 flex items-center justify-between gap-3">
        <span className="text-[13px] text-fin-text-secondary">{rotulo}</span>
        <span className="font-fin-mono text-[12.5px] text-fin-text-primary">{formatCurrency(valor)}</span>
      </div>
      <BarraProporcao pct={maximo > 0 ? (valor / maximo) * 100 : 0} cor={cor} />
    </div>
  )
}

export default function RitmoMes() {
  const estado = useRelatorio(() => ritmoMes(), 'ritmo')

  return (
    <EstadoView estado={estado}>
      {(r) => {
        const referencia = new Date(r.dataReferencia)
        const nomeMes = referencia.toLocaleDateString('pt-BR', { month: 'long', year: 'numeric' })
        const pctDias = r.diasNoMes > 0 ? (r.diasDecorridos / r.diasNoMes) * 100 : 0
        const maximo = Math.max(r.gastoAteHoje, r.gastoMesAnteriorAteMesmoDia, r.projecaoFimMes, r.gastoTotalMesAnterior)
        const variacaoProjecao =
          r.gastoTotalMesAnterior > 0 ? ((r.projecaoFimMes - r.gastoTotalMesAnterior) / r.gastoTotalMesAnterior) * 100 : null
        return (
          <div className="space-y-4">
            <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
              <Kpi
                titulo="Gasto até hoje"
                valor={formatCurrency(r.gastoAteHoje)}
                icone="lucide:calendar-check"
                tom="negativo"
                detalhe={`Dia ${r.diasDecorridos} de ${r.diasNoMes}`}
              />
              <Kpi
                titulo={`Mês anterior até o dia ${r.diaComparadoMesAnterior}`}
                valor={formatCurrency(r.gastoMesAnteriorAteMesmoDia)}
                icone="lucide:calendar-minus"
                detalhe={
                  <span className="inline-flex items-center gap-1.5">
                    Ritmo atual <VariacaoChip valor={r.variacaoPercentual} />
                  </span>
                }
              />
              <Kpi
                titulo="Projeção para o fim do mês"
                valor={formatCurrency(r.projecaoFimMes)}
                icone="lucide:trending-up"
                destaque
                detalhe={`Mês anterior fechou em ${formatCurrency(r.gastoTotalMesAnterior)}`}
              />
            </div>

            <Secao
              titulo="Ritmo do mês"
              descricao={`${nomeMes} — pela data da transação. A projeção é linear: gasto até hoje ÷ dias decorridos × dias do mês.`}
            >
              <div className="space-y-5 px-5 py-[18px]">
                <div>
                  <div className="mb-1.5 flex items-center justify-between">
                    <span className="text-[12px] font-semibold uppercase tracking-[0.05em] text-fin-text-muted">Mês decorrido</span>
                    <span className="text-[12px] text-fin-text-secondary">{formatPercent(pctDias, 0)}</span>
                  </div>
                  <BarraProporcao pct={pctDias} cor="var(--fin-text-muted)" />
                </div>
                <Linha rotulo="Gasto até hoje" valor={r.gastoAteHoje} maximo={maximo} cor="var(--fin-negative)" />
                <Linha
                  rotulo={`Mês anterior até o dia ${r.diaComparadoMesAnterior}`}
                  valor={r.gastoMesAnteriorAteMesmoDia}
                  maximo={maximo}
                  cor="var(--fin-warning)"
                />
                <Linha rotulo="Projeção para o fim do mês" valor={r.projecaoFimMes} maximo={maximo} cor="var(--fin-brand)" />
                <Linha rotulo="Total do mês anterior" valor={r.gastoTotalMesAnterior} maximo={maximo} cor="var(--fin-invest)" />
                <p className="flex items-center gap-1.5 text-[12.5px] text-fin-text-secondary">
                  Projeção vs. total do mês anterior <VariacaoChip valor={variacaoProjecao} />
                </p>
              </div>
            </Secao>
          </div>
        )
      }}
    </EstadoView>
  )
}
