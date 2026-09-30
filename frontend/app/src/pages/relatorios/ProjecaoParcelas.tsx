import { useState } from 'react'
import { projecaoParcelas } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency, formatMesCompetencia } from '../../utils/format'
import { BarraFiltros, RotuloFiltro, SeletorCompetencia } from './filtros'
import { CorCategoria, EstadoView, GraficoBarras, Kpi, Secao, tbodyCls, td, th, theadRow, trCls } from './ui'
import { formatDataLocal, labelCompetencia, labelMesCurto, mesAtual } from './utils'

const opcoesMeses = [3, 6, 12, 24, 36]

function Reducao({ valor }: { valor: number }) {
  if (valor === 0) return <span className="text-fin-text-muted">—</span>
  // Positivo = o compromisso caiu (bom); negativo = subiu
  const caiu = valor > 0
  return (
    <span className={`font-fin-mono ${caiu ? 'text-fin-positive' : 'text-fin-negative'}`}>
      {caiu ? '▼' : '▲'} {formatCurrency(Math.abs(valor))}
    </span>
  )
}

export default function ProjecaoParcelas() {
  const [{ mes, ano }, setReferencia] = useState(mesAtual)
  const [meses, setMeses] = useState(12)
  const estado = useRelatorio(() => projecaoParcelas(mes, ano, meses), `${mes}-${ano}|${meses}`, {
    vazio: (d) => d.comprasAtivas.length === 0 && d.meses.every((m) => m.totalParcelas === 0) && d.totalParcelasMesReferencia === 0,
  })

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <SeletorCompetencia rotulo="Mês de referência" mes={mes} ano={ano} onChange={(m, a) => setReferencia({ mes: m, ano: a })} />
        <div>
          <RotuloFiltro>Horizonte</RotuloFiltro>
          <select
            aria-label="Horizonte da projeção"
            value={meses}
            onChange={(e) => setMeses(Number(e.target.value))}
            className="rounded-lg border border-fin-border bg-fin-surface px-3 py-1.5 text-sm text-fin-text-primary focus:border-fin-brand focus:shadow-fin-focus focus:outline-none"
          >
            {opcoesMeses.map((n) => (
              <option key={n} value={n}>{n} meses</option>
            ))}
          </select>
        </div>
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Nenhuma compra parcelada ativa depois do mês de referência.">
        {(p) => (
          <>
            <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
              <Kpi
                titulo="Parcelas no mês de referência"
                valor={formatCurrency(p.totalParcelasMesReferencia)}
                icone="lucide:calendar"
                tom="marca"
                detalhe={labelCompetencia(p.mesReferencia, p.anoReferencia)}
              />
              <Kpi
                titulo="Compras parceladas ativas"
                valor={String(p.comprasAtivas.length)}
                icone="lucide:shopping-bag"
                tom="invest"
              />
              <Kpi
                titulo="Total restante"
                valor={formatCurrency(p.totalRestante)}
                icone="lucide:hourglass"
                destaque
                detalhe="Inclui parcelas além do horizonte"
              />
            </div>

            <Secao titulo="Evolução do total em parcelas" descricao={`Próximos ${p.quantidadeMeses} meses a partir de ${labelCompetencia(p.mesReferencia, p.anoReferencia)}.`}>
              <GraficoBarras
                series={[{ nome: 'Total em parcelas', cor: 'var(--fin-brand)' }]}
                itens={p.meses.map((m) => ({
                  rotulo: labelMesCurto(m.mes, m.ano),
                  valores: [m.totalParcelas],
                  dica: `${labelCompetencia(m.mes, m.ano)}: ${formatCurrency(m.totalParcelas)}`,
                }))}
                formatar={formatCurrency}
              />
              <div className="overflow-x-auto border-t border-fin-border">
                <table className="w-full text-sm">
                  <thead>
                    <tr className={theadRow}>
                      <th className={th}>Mês</th>
                      <th className={`${th} text-right`}>Total em parcelas</th>
                      <th className={`${th} text-right`}>Parcelas</th>
                      <th className={`${th} text-right`}>Terminando</th>
                      <th className={`${th} text-right`}>Redução vs. mês anterior</th>
                    </tr>
                  </thead>
                  <tbody className={tbodyCls}>
                    {p.meses.map((m) => (
                      <tr key={`${m.ano}-${m.mes}`} className={trCls}>
                        <td className={`${td} capitalize text-fin-text-primary`}>{labelCompetencia(m.mes, m.ano)}</td>
                        <td className={`${td} text-right font-fin-mono text-fin-text-primary`}>{formatCurrency(m.totalParcelas)}</td>
                        <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>{m.quantidadeParcelas}</td>
                        <td className={`${td} text-right text-fin-text-secondary`}>
                          {m.quantidadeParcelasTerminando > 0 ? (
                            <span className="font-fin-mono">
                              {m.quantidadeParcelasTerminando} · {formatCurrency(m.totalParcelasTerminando)}
                            </span>
                          ) : (
                            <span className="text-fin-text-muted">—</span>
                          )}
                        </td>
                        <td className={`${td} text-right`}><Reducao valor={m.reducaoEmRelacaoAoMesAnterior} /></td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </Secao>

            <Secao titulo="Compras parceladas ativas" descricao="Com ao menos uma parcela depois do mês de referência.">
              {p.comprasAtivas.length === 0 ? (
                <p className="px-5 py-8 text-center text-sm text-fin-text-muted">Nenhuma compra parcelada ativa.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className={theadRow}>
                        <th className={th}>Descrição</th>
                        <th className={th}>Categoria</th>
                        <th className={th}>Conta</th>
                        <th className={th}>Compra</th>
                        <th className={`${th} text-right`}>Parcela</th>
                        <th className={`${th} text-right`}>Valor da parcela</th>
                        <th className={`${th} text-right`}>Restantes</th>
                        <th className={`${th} text-right`}>Valor restante</th>
                        <th className={th}>Última</th>
                      </tr>
                    </thead>
                    <tbody className={tbodyCls}>
                      {p.comprasAtivas.map((c) => (
                        <tr key={`${c.descricao}|${c.contaId}|${c.dataCompra}|${c.totalParcelas}`} className={trCls}>
                          <td className={`${td} font-medium text-fin-text-primary`}>{c.descricao || '—'}</td>
                          <td className={td}>
                            <span className="flex items-center gap-2 whitespace-nowrap text-fin-text-secondary">
                              <CorCategoria cor={c.cor} />
                              {c.nomeCategoria}
                            </span>
                          </td>
                          <td className={`${td} text-fin-text-secondary`}>{c.nomeConta}</td>
                          <td className={`${td} text-fin-text-muted`}>{formatDataLocal(c.dataCompra)}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>
                            {c.parcelaAtual === 0 ? 'não iniciada' : `${c.parcelaAtual}/${c.totalParcelas}`}
                          </td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-primary`}>{formatCurrency(c.valorParcela)}</td>
                          <td className={`${td} text-right font-fin-mono text-fin-text-secondary`}>{c.parcelasRestantes}</td>
                          <td className={`${td} text-right font-fin-mono font-semibold text-fin-text-primary`}>{formatCurrency(c.valorRestante)}</td>
                          <td className={`${td} whitespace-nowrap text-fin-text-muted`}>{formatMesCompetencia(c.ultimaCompetencia)}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </Secao>
          </>
        )}
      </EstadoView>
    </div>
  )
}
