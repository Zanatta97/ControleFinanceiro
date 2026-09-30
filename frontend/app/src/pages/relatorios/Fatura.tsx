import { useState } from 'react'
import { listarDoAmbiente } from '../../api/conta'
import { faturaCompetencia } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { TipoConta } from '../../types/api'
import { formatCurrency, formatPercent } from '../../utils/format'
import { BarraFiltros, RotuloFiltro, SeletorCompetencia } from './filtros'
import { BarraProporcao, EstadoVazio, EstadoView, Kpi, Secao } from './ui'
import { labelCompetencia, mesAtual } from './utils'

const campo =
  'min-w-[200px] rounded-lg border border-fin-border bg-fin-surface px-3 py-1.5 text-sm text-fin-text-primary focus:border-fin-brand focus:shadow-fin-focus focus:outline-none'

export default function Fatura() {
  const estadoContas = useRelatorio(() => listarDoAmbiente(), 'contas')

  return (
    <EstadoView estado={estadoContas} vazio="Nenhuma conta cadastrada neste ambiente.">
      {(contas) => {
        const cartoes = contas.filter((c) => c.tipoConta === TipoConta.CartaoCredito)
        if (cartoes.length === 0) {
          return <EstadoVazio icone="lucide:credit-card" mensagem="Nenhum cartão de crédito cadastrado neste ambiente." />
        }
        return <FaturaCartao cartoes={cartoes.map((c) => ({ id: c.id, nome: c.nome }))} />
      }}
    </EstadoView>
  )
}

function FaturaCartao({ cartoes }: { cartoes: { id: string; nome: string }[] }) {
  const [contaId, setContaId] = useState(cartoes[0].id)
  const [{ mes, ano }, setCompetencia] = useState(mesAtual)
  const estado = useRelatorio(() => faturaCompetencia(contaId, mes, ano), `${contaId}|${mes}-${ano}`)

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <div>
          <RotuloFiltro>Cartão</RotuloFiltro>
          <select aria-label="Cartão" value={contaId} onChange={(e) => setContaId(e.target.value)} className={campo}>
            {cartoes.map((c) => (
              <option key={c.id} value={c.id}>{c.nome}</option>
            ))}
          </select>
        </div>
        <SeletorCompetencia mes={mes} ano={ano} onChange={(m, a) => setCompetencia({ mes: m, ano: a })} />
      </BarraFiltros>

      <EstadoView estado={estado} vazio="Fatura não encontrada.">
        {(f) => {
          const pctPago = f.totalDespesas > 0 ? (f.totalPago / f.totalDespesas) * 100 : null
          const quitada = f.totalDespesas > 0 && f.saldoEmAberto <= 0
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-3">
                <Kpi titulo="Total da fatura" valor={formatCurrency(f.totalDespesas)} icone="lucide:receipt" tom="negativo" />
                <Kpi titulo="Pago" valor={formatCurrency(f.totalPago)} icone="lucide:check-circle" tom="positivo" />
                <Kpi
                  titulo="Saldo em aberto"
                  valor={formatCurrency(f.saldoEmAberto)}
                  icone="lucide:wallet"
                  destaque
                  detalhe={f.saldoEmAberto < 0 ? 'Pagamento acima do valor da fatura' : quitada ? 'Fatura quitada' : undefined}
                />
              </div>
              <Secao titulo={f.nomeConta} descricao={`Fatura de ${labelCompetencia(f.mes, f.ano)}`}>
                <div className="px-5 py-[18px]">
                  {f.totalDespesas === 0 ? (
                    <p className="text-center text-sm text-fin-text-muted">Nenhuma despesa lançada neste cartão na competência.</p>
                  ) : (
                    <>
                      <div className="mb-1.5 flex items-center justify-between">
                        <span className="text-[13px] text-fin-text-secondary">Pago da fatura</span>
                        <span className="text-[12px] font-semibold text-fin-text-primary">{formatPercent(pctPago)}</span>
                      </div>
                      <BarraProporcao pct={pctPago ?? 0} cor="var(--fin-positive)" />
                    </>
                  )}
                </div>
              </Secao>
            </>
          )
        }}
      </EstadoView>
    </div>
  )
}
