import { statusOrcamentos } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { StatusOrcamento } from '../../types/api'
import { formatCurrency, formatPercent } from '../../utils/format'
import Badge from '../../components/ui/Badge'
import { BarraProporcao, EstadoView, Kpi, Secao } from './ui'
import { formatDataLocal } from './utils'

// Mesmas faixas de cor da Dashboard
function corPercentual(pct: number) {
  return pct >= 90 ? 'var(--fin-negative)' : pct >= 70 ? 'var(--fin-warning)' : 'var(--fin-positive)'
}

export default function StatusOrcamentos() {
  const estado = useRelatorio(() => statusOrcamentos(), 'orcamentos')

  return (
    <EstadoView estado={estado} vazio="Nenhum orçamento encontrado.">
      {(dados) => {
        const ativos = dados.filter((o) => o.status === StatusOrcamento.Ativo)
        const estourados = dados.filter((o) => o.percentual >= 100)
        const limite = ativos.reduce((acc, o) => acc + o.valorLimite, 0)
        const gasto = ativos.reduce((acc, o) => acc + o.valorGasto, 0)
        return (
          <div className="space-y-4">
            <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
              <Kpi titulo="Orçamentos ativos" valor={String(ativos.length)} icone="lucide:target" tom="marca" />
              <Kpi
                titulo="Gasto × limite (ativos)"
                valor={formatCurrency(gasto)}
                icone="lucide:wallet"
                tom="invest"
                detalhe={`de ${formatCurrency(limite)} · ${limite > 0 ? formatPercent((gasto / limite) * 100) : '—'}`}
              />
              <Kpi titulo="Acima do limite" valor={String(estourados.length)} icone="lucide:alert-triangle" tom={estourados.length > 0 ? 'negativo' : 'neutro'} />
            </div>

            <Secao titulo="Status dos orçamentos">
              <div className="flex flex-col gap-5 px-5 py-[18px]">
                {dados.map((o) => {
                  const cor = corPercentual(o.percentual)
                  return (
                    <div key={o.id}>
                      <div className="mb-1.5 flex flex-wrap items-center justify-between gap-2">
                        <span className="flex items-center gap-2">
                          <span className="text-[13px] font-semibold text-fin-text-primary">{o.nomeOrcamento}</span>
                          {o.status === StatusOrcamento.Encerrado && <Badge>Encerrado</Badge>}
                        </span>
                        <span className="font-fin-mono text-[11.5px] text-fin-text-muted">
                          {formatCurrency(o.valorGasto)} / {formatCurrency(o.valorLimite)}
                        </span>
                      </div>
                      <BarraProporcao pct={o.percentual} cor={cor} />
                      <div className="mt-1.5 flex justify-between">
                        <span className="text-[11px] text-fin-text-muted">
                          {o.nomeCategoria} · até {formatDataLocal(o.dataLimite)}
                        </span>
                        <span className="text-[11px] font-semibold" style={{ color: cor }}>{formatPercent(o.percentual)} usado</span>
                      </div>
                    </div>
                  )
                })}
              </div>
            </Secao>
          </div>
        )
      }}
    </EstadoView>
  )
}
