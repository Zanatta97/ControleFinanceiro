import { useState } from 'react'
import { matrizCategoriaMes } from '../../api/relatorio'
import CartaoLinha, { ListaCartoes } from '../../components/ui/CartaoLinha'
import type { MatrizCategoriaValorMes } from '../../types/api'
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

// Grade de meses dos cartões do celular. Nas categorias repete o mapa de calor da tabela;
// no cartão de total (`total`), segue o destaque da linha de totais.
function GradeMeses({ meses, maximo = 0, total = false }: { meses: MatrizCategoriaValorMes[]; maximo?: number; total?: boolean }) {
  return (
    <div className="grid grid-cols-3 gap-1.5">
      {meses.map((m) => {
        const intensidade = maximo > 0 && m.valor > 0 ? Math.round((m.valor / maximo) * 32) + 4 : 0
        return (
          <div
            key={m.mes}
            className={`rounded-md px-2 py-1.5 ${total ? 'bg-fin-surface' : 'bg-fin-surface-2'}`}
            style={intensidade > 0 ? { backgroundColor: `color-mix(in srgb, var(--fin-brand) ${intensidade}%, transparent)` } : undefined}
          >
            <p className="text-[10.5px] font-medium uppercase tracking-wide text-fin-text-muted">{m.nomeMes.slice(0, 3)}</p>
            <p className={`font-fin-mono text-[12px] ${total ? 'font-semibold' : ''} ${m.valor === 0 ? 'text-fin-text-muted' : 'text-fin-text-primary'}`}>
              {m.valor === 0 ? '–' : numero.format(m.valor)}
            </p>
          </div>
        )
      })}
    </div>
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
              <div className="scrollbar-fino hidden overflow-x-auto md:block">
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
              <ListaCartoes>
                {dados.categorias.map((c) => (
                  <CartaoLinha
                    key={c.categoriaId}
                    marcador={<CorCategoria cor={c.cor} />}
                    titulo={c.nomeCategoria}
                    destaque={<span className="font-fin-mono font-semibold text-fin-text-primary">{numero.format(c.totalAno)}</span>}
                  >
                    <GradeMeses meses={c.meses} maximo={maximo} />
                  </CartaoLinha>
                ))}
                <CartaoLinha
                  variante="total"
                  titulo="Total"
                  destaque={<span className="font-fin-mono font-semibold text-fin-text-primary">{numero.format(dados.totalAno)}</span>}
                >
                  <GradeMeses meses={dados.totaisPorMes} total />
                </CartaoLinha>
              </ListaCartoes>
            </Secao>
          )
        }}
      </EstadoView>
    </div>
  )
}
