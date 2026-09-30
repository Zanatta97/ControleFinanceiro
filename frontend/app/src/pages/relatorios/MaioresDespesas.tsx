import { useState } from 'react'
import { maioresDespesas } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency, formatMesCompetencia } from '../../utils/format'
import { BarraFiltros, FiltroPeriodo, RotuloFiltro } from './filtros'
import { CorCategoria, EstadoView, Secao, tbodyCls, td, th, theadRow, trCls } from './ui'
import { chaveFiltro, descricaoFiltro, erroPeriodo, filtroInicial, filtroParaParams, formatDataLocal } from './utils'

const QUANTIDADE_MIN = 1
const QUANTIDADE_MAX = 100

export default function MaioresDespesas() {
  const [filtro, setFiltro] = useState(filtroInicial)
  // Rascunho do campo; a quantidade só vale (e dispara a busca) quando está entre 1 e 100
  const [rascunho, setRascunho] = useState('10')
  const [quantidade, setQuantidade] = useState(10)

  const numero = Number(rascunho)
  const quantidadeValida = Number.isInteger(numero) && numero >= QUANTIDADE_MIN && numero <= QUANTIDADE_MAX

  function aplicarQuantidade() {
    if (quantidadeValida) setQuantidade(numero)
  }

  const estado = useRelatorio(
    () => maioresDespesas(filtroParaParams(filtro), quantidade),
    `${chaveFiltro(filtro)}|${quantidade}`,
    { habilitado: !erroPeriodo(filtro) },
  )

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <FiltroPeriodo valor={filtro} onChange={setFiltro} />
        <div>
          <RotuloFiltro>Quantidade</RotuloFiltro>
          <input
            type="number"
            min={QUANTIDADE_MIN}
            max={QUANTIDADE_MAX}
            step={1}
            aria-label="Quantidade de despesas"
            value={rascunho}
            onChange={(e) => setRascunho(e.target.value)}
            onBlur={aplicarQuantidade}
            onKeyDown={(e) => e.key === 'Enter' && aplicarQuantidade()}
            className={`w-[96px] rounded-lg border bg-fin-surface px-3 py-1.5 text-sm text-fin-text-primary focus:shadow-fin-focus focus:outline-none ${
              quantidadeValida ? 'border-fin-border focus:border-fin-brand' : 'border-fin-negative'
            }`}
          />
        </div>
        {!quantidadeValida && (
          <p className="basis-full text-xs text-fin-negative">Informe uma quantidade entre {QUANTIDADE_MIN} e {QUANTIDADE_MAX}.</p>
        )}
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Nenhuma despesa encontrada no período.">
        {(dados) => (
          <Secao titulo={`As ${dados.length} maiores despesas`} descricao={descricaoFiltro(filtro)}>
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className={theadRow}>
                    <th className={`${th} w-10`}>#</th>
                    <th className={th}>Descrição</th>
                    <th className={th}>Categoria</th>
                    <th className={th}>Conta</th>
                    <th className={th}>Data</th>
                    <th className={th}>Competência</th>
                    <th className={`${th} text-right`}>Valor</th>
                  </tr>
                </thead>
                <tbody className={tbodyCls}>
                  {dados.map((d, i) => (
                    <tr key={d.transacaoId} className={trCls}>
                      <td className={`${td} font-fin-mono text-fin-text-muted`}>{i + 1}</td>
                      <td className={`${td} font-medium text-fin-text-primary`}>{d.descricao || '—'}</td>
                      <td className={td}>
                        <span className="flex items-center gap-2 text-fin-text-secondary">
                          <CorCategoria cor={d.cor} />
                          {d.nomeCategoria}
                        </span>
                      </td>
                      <td className={`${td} text-fin-text-secondary`}>{d.nomeConta}</td>
                      <td className={`${td} text-fin-text-muted`}>{formatDataLocal(d.data)}</td>
                      <td className={`${td} text-fin-text-muted`}>{formatMesCompetencia(d.mesCompetencia)}</td>
                      <td className={`${td} text-right font-fin-mono text-fin-negative`}>{formatCurrency(d.valor)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </Secao>
        )}
      </EstadoView>
    </div>
  )
}
