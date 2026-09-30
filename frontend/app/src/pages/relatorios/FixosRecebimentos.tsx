import { useState } from 'react'
import { fixosRecebimentos } from '../../api/relatorio'
import { useRelatorio } from '../../hooks/useRelatorio'
import { formatCurrency, formatPercent } from '../../utils/format'
import { BarraFiltros, CategoriasFixosRecebimentos, SeletorCompetencia } from './filtros'
import { CorCategoria, EstadoVazio, EstadoView, Kpi, Secao } from './ui'
import { labelCompetencia, mesAtual, type SelecaoCategoriasProps } from './utils'

const cores = {
  fixos: 'var(--fin-negative)',
  parcelas: 'var(--fin-warning)',
  variaveis: 'var(--fin-invest)',
  livre: 'var(--fin-positive)',
}

export default function FixosRecebimentos({ selecao, onSelecao }: SelecaoCategoriasProps) {
  const [{ mes, ano }, setCompetencia] = useState(mesAtual)
  // As duas listas são obrigatórias na API: sem elas não há chamada
  const completo = selecao.fixas.length > 0 && selecao.recebimento.length > 0
  const estado = useRelatorio(
    () => fixosRecebimentos(mes, ano, selecao.fixas, selecao.recebimento),
    `${mes}-${ano}|${[...selecao.fixas].sort().join(',')}|${[...selecao.recebimento].sort().join(',')}`,
    { habilitado: completo },
  )

  return (
    <div className="space-y-4">
      <BarraFiltros>
        <SeletorCompetencia mes={mes} ano={ano} onChange={(m, a) => setCompetencia({ mes: m, ano: a })} />
      </BarraFiltros>

      <CategoriasFixosRecebimentos selecao={selecao} onSelecao={onSelecao} obrigatorio />

      <EstadoView
        estado={estado}
        ocioso={
          <EstadoVazio
            icone="lucide:list-checks"
            mensagem="Selecione ao menos uma categoria de gasto fixo e uma de recebimento para ver quanto da renda já está comprometido."
          />
        }
      >
        {(r) => {
          // Base da barra: o maior entre recebimentos e despesas, para a barra nunca estourar
          const base = Math.max(r.recebimentos, r.gastosFixos + r.parcelas + r.gastosVariaveis)
          const pct = (v: number) => (base > 0 ? (v / base) * 100 : 0)
          const livre = Math.max(r.recebimentos - r.totalDespesas, 0)
          const fixas = r.categorias.filter((c) => c.grupo === 'Fixo')
          const recebimentos = r.categorias.filter((c) => c.grupo === 'Recebimento')
          return (
            <>
              <div className="grid grid-cols-1 gap-3.5 sm:grid-cols-2 lg:grid-cols-4">
                <Kpi
                  titulo="Recebimentos"
                  valor={formatCurrency(r.recebimentos)}
                  icone="lucide:arrow-down-circle"
                  tom="positivo"
                  detalhe={`Outras receitas: ${formatCurrency(r.outrasReceitas)}`}
                />
                <Kpi titulo="Total de despesas" valor={formatCurrency(r.totalDespesas)} icone="lucide:arrow-up-circle" tom="negativo" />
                <Kpi titulo="Saldo" valor={formatCurrency(r.saldo)} icone="lucide:scale" tom={r.saldo >= 0 ? 'positivo' : 'negativo'} />
                <Kpi
                  titulo="Renda comprometida"
                  valor={formatPercent(r.percentualComprometido)}
                  icone="lucide:gauge"
                  destaque
                  detalhe={
                    r.percentualComprometido == null
                      ? 'Sem recebimento no mês'
                      : `Fixos: ${formatPercent(r.percentualFixosSobreRecebimentos)} dos recebimentos`
                  }
                />
              </div>

              <Secao
                titulo="Recebimentos × fixos × parcelas × variáveis"
                descricao={`${labelCompetencia(r.mes, r.ano)}. Comprometido = fixos + parcelas sobre os recebimentos.`}
              >
                <div className="space-y-4 px-5 py-[18px]">
                  <div className="flex h-3.5 overflow-hidden rounded-[6px] bg-fin-surface-2">
                    <div style={{ width: `${pct(r.gastosFixos)}%`, backgroundColor: cores.fixos }} />
                    <div style={{ width: `${pct(r.parcelas)}%`, backgroundColor: cores.parcelas }} />
                    <div style={{ width: `${pct(r.gastosVariaveis)}%`, backgroundColor: cores.variaveis }} />
                    <div style={{ width: `${pct(livre)}%`, backgroundColor: cores.livre }} />
                  </div>
                  <div className="grid grid-cols-1 gap-2.5 sm:grid-cols-2">
                    {[
                      { nome: 'Gastos fixos', valor: r.gastosFixos, cor: cores.fixos },
                      { nome: 'Parcelas (fora das fixas)', valor: r.parcelas, cor: cores.parcelas },
                      { nome: 'Gastos variáveis', valor: r.gastosVariaveis, cor: cores.variaveis },
                      { nome: 'Sobra dos recebimentos', valor: livre, cor: cores.livre },
                    ].map((i) => (
                      <div key={i.nome} className="flex items-center gap-2.5">
                        <span className="h-2.5 w-2.5 flex-none rounded-[3px]" style={{ backgroundColor: i.cor }} />
                        <span className="flex-1 text-[12.5px] text-fin-text-secondary">{i.nome}</span>
                        <span className="text-[11.5px] text-fin-text-muted">
                          {r.recebimentos > 0 ? formatPercent((i.valor / r.recebimentos) * 100) : '—'}
                        </span>
                        <span className="min-w-[96px] text-right font-fin-mono text-[12.5px] text-fin-text-primary">{formatCurrency(i.valor)}</span>
                      </div>
                    ))}
                  </div>
                </div>
              </Secao>

              <div className="grid grid-cols-1 gap-3.5 lg:grid-cols-2">
                <ListaCategorias titulo="Recebimentos por categoria" itens={recebimentos} tom="text-fin-positive" />
                <ListaCategorias titulo="Gastos fixos por categoria" itens={fixas} tom="text-fin-negative" />
              </div>
            </>
          )
        }}
      </EstadoView>
    </div>
  )
}

interface ListaCategoriasProps {
  titulo: string
  itens: { categoriaId: string; nomeCategoria: string; cor: string | null; total: number }[]
  tom: string
}

function ListaCategorias({ titulo, itens, tom }: ListaCategoriasProps) {
  return (
    <Secao titulo={titulo}>
      {itens.length === 0 ? (
        <p className="px-5 py-6 text-center text-sm text-fin-text-muted">Nada lançado nestas categorias no mês.</p>
      ) : (
        <div className="flex flex-col gap-2.5 px-5 py-[18px]">
          {itens.map((c) => (
            <div key={c.categoriaId} className="flex items-center gap-2.5">
              <CorCategoria cor={c.cor} />
              <span className="flex-1 truncate text-[13px] text-fin-text-secondary">{c.nomeCategoria}</span>
              <span className={`font-fin-mono text-[12.5px] ${tom}`}>{formatCurrency(c.total)}</span>
            </div>
          ))}
        </div>
      )}
    </Secao>
  )
}
