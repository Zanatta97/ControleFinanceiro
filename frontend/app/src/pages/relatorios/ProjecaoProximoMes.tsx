import { useState } from 'react'
import { projecaoProximoMes } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { TipoConta } from '../../types/api'
import { formatCurrency } from '../../utils/format'
import { BarraFiltros, CategoriasFixosRecebimentos, SeletorCompetencia } from './filtros'
import { EstadoView, Kpi, Secao, tbodyCls, td, th, theadRow, trCls } from './ui'
import { labelCompetencia, nomeTipoConta, proximoMes, type SelecaoCategoriasProps } from './utils'

function Valor({ v, tom }: { v: number; tom?: 'positivo' | 'negativo' }) {
  if (v === 0) return <span className="text-fin-text-muted">—</span>
  const cls = tom === 'positivo' ? 'text-fin-positive' : tom === 'negativo' ? 'text-fin-negative' : 'text-fin-text-primary'
  return <span className={`font-fin-mono ${cls}`}>{formatCurrency(v)}</span>
}

export default function ProjecaoProximoMes({ selecao, onSelecao }: SelecaoCategoriasProps) {
  const [{ mes, ano }, setAlvo] = useState(proximoMes)
  const estado = useRelatorio(
    () => projecaoProximoMes(mes, ano, selecao.fixas, selecao.recebimento),
    `${mes}-${ano}|${[...selecao.fixas].sort().join(',')}|${[...selecao.recebimento].sort().join(',')}`,
  )

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <SeletorCompetencia rotulo="Mês projetado" mes={mes} ano={ano} onChange={(m, a) => setAlvo({ mes: m, ano: a })} />
        <p className="basis-full text-xs text-fin-text-muted">
          Fixos e recebimentos são estimados pela média dos 3 meses anteriores nas categorias escolhidas, descontado o que já foi lançado.
        </p>
      </BarraFiltros>

      <CategoriasFixosRecebimentos selecao={selecao} onSelecao={onSelecao} />

      <EstadoView estado={estado} vazio="Nenhuma conta encontrada para projetar.">
        {(p) => {
          const cartoes = p.contas.filter((c) => c.tipoConta === TipoConta.CartaoCredito)
          const faturaTotal = cartoes.reduce((acc, c) => acc + (c.faturaPrevista ?? 0), 0)
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2 lg:grid-cols-4">
                <Kpi titulo="Receitas previstas" valor={formatCurrency(p.receitasPrevistas)} icone="lucide:arrow-down-circle" tom="positivo" />
                <Kpi
                  titulo="Despesas previstas"
                  valor={formatCurrency(p.despesasPrevistas)}
                  icone="lucide:arrow-up-circle"
                  tom="negativo"
                  detalhe={`Parcelas já lançadas: ${formatCurrency(p.totalParcelas)}`}
                />
                <Kpi
                  titulo="Resultado previsto"
                  valor={formatCurrency(p.resultadoPrevisto)}
                  icone="lucide:scale"
                  tom={p.resultadoPrevisto >= 0 ? 'positivo' : 'negativo'}
                />
                <Kpi
                  titulo="Saldo previsto"
                  valor={formatCurrency(p.saldoPrevisto)}
                  icone="lucide:piggy-bank"
                  destaque
                  detalhe={`Fim de ${labelCompetencia(p.mes, p.ano)}`}
                />
              </div>

              {cartoes.length > 0 && (
                <Secao titulo="Fatura prevista dos cartões" descricao="Despesas já lançadas no cartão + fixos estimados no cartão.">
                  <div className="grid grid-cols-1 gap-3 px-5 py-[18px] sm:grid-cols-2 lg:grid-cols-3">
                    {cartoes.map((c) => (
                      <div key={c.contaId} className="rounded-[11px] border border-fin-border bg-fin-surface-2 p-3.5">
                        <p className="text-[12.5px] font-semibold text-fin-text-secondary">{c.nomeConta}</p>
                        <p className="mt-1 font-fin-mono text-[18px] text-fin-text-primary">{formatCurrency(c.faturaPrevista ?? 0)}</p>
                      </div>
                    ))}
                    {cartoes.length > 1 && (
                      <div className="rounded-[11px] border border-fin-invest bg-fin-invest-soft p-3.5">
                        <p className="text-[12.5px] font-semibold text-fin-invest">Total dos cartões</p>
                        <p className="mt-1 font-fin-mono text-[18px] text-fin-text-primary">{formatCurrency(faturaTotal)}</p>
                      </div>
                    )}
                  </div>
                </Secao>
              )}

              <Secao titulo="Saldo previsto por conta" descricao={labelCompetencia(p.mes, p.ano)}>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className={theadRow}>
                        <th className={th}>Conta</th>
                        <th className={`${th} text-right`}>Saldo inicial</th>
                        <th className={`${th} text-right`}>Entradas lançadas</th>
                        <th className={`${th} text-right`}>Saídas lançadas</th>
                        <th className={`${th} text-right`}>Recebim. estimados</th>
                        <th className={`${th} text-right`}>Fixos estimados</th>
                        <th className={`${th} text-right`}>Saldo final</th>
                      </tr>
                    </thead>
                    <tbody className={tbodyCls}>
                      {p.contas.map((c) => (
                        <tr key={c.contaId} className={trCls}>
                          <td className={td}>
                            <p className="font-medium text-fin-text-primary">{c.nomeConta}</p>
                            <p className="text-[11.5px] text-fin-text-muted">{nomeTipoConta[c.tipoConta] ?? ''}</p>
                          </td>
                          <td className={`${td} text-right`}><Valor v={c.saldoInicialPrevisto} /></td>
                          <td className={`${td} text-right`}><Valor v={c.entradasLancadas} tom="positivo" /></td>
                          <td className={`${td} text-right`}><Valor v={c.saidasLancadas} tom="negativo" /></td>
                          <td className={`${td} text-right`}><Valor v={c.recebimentosEstimados} tom="positivo" /></td>
                          <td className={`${td} text-right`}><Valor v={c.fixosEstimados} tom="negativo" /></td>
                          <td className={`${td} text-right font-fin-mono font-semibold ${c.saldoFinalPrevisto < 0 ? 'text-fin-negative' : 'text-fin-text-primary'}`}>
                            {formatCurrency(c.saldoFinalPrevisto)}
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
