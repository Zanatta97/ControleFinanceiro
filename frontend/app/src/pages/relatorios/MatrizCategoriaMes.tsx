import { useState } from 'react'
import { matrizCategoriaMes } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency } from '../../utils/format'
import { BarraFiltros, SeletorAno } from './filtros'
import { CorCategoria, EstadoView, Secao } from './ui'
import { mesAtual } from './utils'

// Valores sem "R$" para caber 12 colunas; a legenda da seção avisa a moeda
const numero = new Intl.NumberFormat('pt-BR', { minimumFractionDigits: 2, maximumFractionDigits: 2 })

function Celula({ valor, maximo }: { valor: number; maximo: number }) {
  if (valor === 0) return <td className="px-3 py-2.5 text-right text-fin-text-muted">–</td>
  // Intensidade proporcional ao maior valor da matriz (mapa de calor discreto)
  const intensidade = maximo > 0 ? Math.round((valor / maximo) * 32) + 4 : 0
  return (
    <td
      className="whitespace-nowrap px-3 py-2.5 text-right font-fin-mono text-[12px] text-fin-text-primary"
      style={{ backgroundColor: `color-mix(in srgb, var(--fin-brand) ${intensidade}%, transparent)` }}
    >
      {numero.format(valor)}
    </td>
  )
}

export default function MatrizCategoriaMes() {
  const [ano, setAno] = useState(() => mesAtual().ano)
  const estado = useRelatorio(() => matrizCategoriaMes(ano), String(ano), {
    vazio: (d) => d.categorias.length === 0,
  })

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <SeletorAno ano={ano} onChange={setAno} />
      </BarraFiltros>

      <EstadoView estado={estado} vazio={`Nenhuma despesa encontrada em ${ano}.`}>
        {(dados) => {
          const maximo = Math.max(0, ...dados.categorias.flatMap((c) => c.meses.map((m) => m.valor)))
          return (
            <Secao
              titulo={`Categoria × mês — ${dados.ano}`}
              descricao={`Valores em R$, pela competência. Total do ano: ${formatCurrency(dados.totalAno)}.`}
            >
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead>
                    <tr className="border-b border-fin-border text-xs font-medium uppercase tracking-wide text-fin-text-muted">
                      <th className="sticky left-0 z-10 bg-fin-surface px-4 py-3 text-left">Categoria</th>
                      {dados.totaisPorMes.map((m) => (
                        <th key={m.mes} className="px-3 py-3 text-right capitalize">{m.nomeMes.slice(0, 3)}</th>
                      ))}
                      <th className="px-4 py-3 text-right">Total</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-fin-border">
                    {dados.categorias.map((c) => (
                      <tr key={c.categoriaId}>
                        <td className="sticky left-0 z-10 bg-fin-surface px-4 py-2.5">
                          <span className="flex items-center gap-2 whitespace-nowrap font-medium text-fin-text-primary">
                            <CorCategoria cor={c.cor} />
                            {c.nomeCategoria}
                          </span>
                        </td>
                        {c.meses.map((m) => <Celula key={m.mes} valor={m.valor} maximo={maximo} />)}
                        <td className="whitespace-nowrap px-4 py-2.5 text-right font-fin-mono text-[12px] font-semibold text-fin-text-primary">
                          {numero.format(c.totalAno)}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="border-t-2 border-fin-border bg-fin-surface-2">
                      <td className="sticky left-0 z-10 bg-fin-surface-2 px-4 py-3 text-xs font-semibold uppercase tracking-wide text-fin-text-muted">
                        Total
                      </td>
                      {dados.totaisPorMes.map((m) => (
                        <td key={m.mes} className="whitespace-nowrap px-3 py-3 text-right font-fin-mono text-[12px] font-semibold text-fin-text-primary">
                          {m.valor === 0 ? '–' : numero.format(m.valor)}
                        </td>
                      ))}
                      <td className="whitespace-nowrap px-4 py-3 text-right font-fin-mono text-[12px] font-semibold text-fin-text-primary">
                        {numero.format(dados.totalAno)}
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </Secao>
          )
        }}
      </EstadoView>
    </div>
  )
}
