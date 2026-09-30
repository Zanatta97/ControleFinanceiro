import { useState } from 'react'
import { listarDoAmbiente } from '../../api/conta'
import { extratoConta } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { TipoTransacao, type ContaResponse, type TransacaoResponse } from '../../types/api'
import { formatCurrency } from '../../utils/format'
import { BarraFiltros, RotuloFiltro } from './filtros'
import { EstadoView, Kpi, Secao, tbodyCls, td, th, theadRow, trCls } from './ui'
import { filtroInicial, formatDataLocal, nomeTipoConta } from './utils'

const campo =
  'rounded-lg border border-fin-border bg-fin-surface px-3 py-1.5 text-sm text-fin-text-primary focus:border-fin-brand focus:shadow-fin-focus focus:outline-none'

// Entrada ou saída do ponto de vista da conta do extrato (transferência depende do lado)
function ehEntrada(t: TransacaoResponse, contaId: string) {
  if (t.tipoTransacao === TipoTransacao.Transferencia) return t.contaDestinoId === contaId
  return t.tipoTransacao === TipoTransacao.Receita
}

export default function Extrato() {
  const estadoContas = useRelatorio(() => listarDoAmbiente(), 'contas')
  return (
    <EstadoView estado={estadoContas} vazio="Nenhuma conta cadastrada neste ambiente.">
      {(contas) => <ExtratoConta contas={contas} />}
    </EstadoView>
  )
}

function ExtratoConta({ contas }: { contas: ContaResponse[] }) {
  const inicial = filtroInicial()
  const [contaId, setContaId] = useState(contas[0].id)
  const [dataInicio, setDataInicio] = useState(inicial.dataInicio)
  const [dataFim, setDataFim] = useState(inicial.dataFim)

  const erro = !dataInicio || !dataFim
    ? 'Informe a data inicial e a final.'
    : dataInicio > dataFim
      ? 'A data inicial deve ser anterior ou igual à final.'
      : null

  const estado = useRelatorio(() => extratoConta(contaId, dataInicio, dataFim), `${contaId}|${dataInicio}_${dataFim}`, {
    habilitado: !erro,
  })

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <div>
          <RotuloFiltro>Conta</RotuloFiltro>
          <select aria-label="Conta" value={contaId} onChange={(e) => setContaId(e.target.value)} className={`${campo} min-w-[200px]`}>
            {contas.map((c) => (
              <option key={c.id} value={c.id}>{c.nome} ({nomeTipoConta[c.tipoConta] ?? 'Conta'})</option>
            ))}
          </select>
        </div>
        <div>
          <RotuloFiltro>Período (data da transação)</RotuloFiltro>
          <div className="flex flex-wrap items-center gap-2">
            <input type="date" aria-label="Data inicial" value={dataInicio} onChange={(e) => setDataInicio(e.target.value)} className={campo} />
            <span className="text-sm text-fin-text-muted">até</span>
            <input type="date" aria-label="Data final" value={dataFim} onChange={(e) => setDataFim(e.target.value)} className={campo} />
          </div>
        </div>
        {erro && <p className="basis-full text-xs text-fin-negative">{erro}</p>}
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Extrato não encontrado.">
        {(x) => (
          <>
            <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
              <Kpi titulo="Entradas no período" valor={formatCurrency(x.totalEntradas)} icone="lucide:arrow-down-circle" tom="positivo" />
              <Kpi titulo="Saídas no período" valor={formatCurrency(x.totalSaidas)} icone="lucide:arrow-up-circle" tom="negativo" />
              <Kpi titulo="Saldo atual" valor={formatCurrency(x.saldoAtual)} icone="lucide:wallet" destaque detalhe={x.nomeConta} />
            </div>

            <Secao
              titulo={`Extrato — ${x.nomeConta}`}
              descricao={`De ${formatDataLocal(x.dataInicio)} a ${formatDataLocal(x.dataFim)} · ${x.transacoes.length} lançamento(s)`}
            >
              {x.transacoes.length === 0 ? (
                <p className="px-5 py-8 text-center text-sm text-fin-text-muted">Nenhuma transação no período.</p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className={theadRow}>
                        <th className={th}>Data</th>
                        <th className={th}>Descrição</th>
                        <th className={th}>Categoria</th>
                        <th className={`${th} text-right`}>Valor</th>
                      </tr>
                    </thead>
                    <tbody className={tbodyCls}>
                      {x.transacoes.map((t) => {
                        const entrada = ehEntrada(t, x.contaId)
                        const transferencia = t.tipoTransacao === TipoTransacao.Transferencia
                        return (
                          <tr key={t.id} className={trCls}>
                            <td className={`${td} whitespace-nowrap text-fin-text-muted`}>{formatDataLocal(t.data)}</td>
                            <td className={td}>
                              <p className="font-medium text-fin-text-primary">{t.descricao || '—'}</p>
                              {transferencia && (
                                <p className="text-[11.5px] text-fin-text-muted">
                                  Transferência {entrada ? `de ${t.contaNome ?? 'outra conta'}` : `para ${t.contaDestinoNome ?? 'outra conta'}`}
                                </p>
                              )}
                            </td>
                            <td className={`${td} text-fin-text-secondary`}>{t.categoriaNome || '—'}</td>
                            <td className={`${td} whitespace-nowrap text-right font-fin-mono ${entrada ? 'text-fin-positive' : 'text-fin-negative'}`}>
                              {entrada ? '+' : '−'}{formatCurrency(t.valor)}
                            </td>
                          </tr>
                        )
                      })}
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
