// Subabas da página de relatórios. O slug é o último segmento da URL: /relatorios/{slug}

export type GrupoAba = 'analise' | 'projecao' | 'gerais'

export interface AbaRelatorio {
  slug: string
  rotulo: string
  icone: string
  grupo: GrupoAba
  descricao: string
}

export const grupos: { id: GrupoAba; rotulo: string; icone: string }[] = [
  { id: 'analise', rotulo: 'Análise de gastos', icone: 'lucide:search' },
  { id: 'projecao', rotulo: 'Projeção e comprometimento', icone: 'lucide:trending-up' },
  { id: 'gerais', rotulo: 'Visão geral', icone: 'lucide:layout-grid' },
]

export const abas: AbaRelatorio[] = [
  { slug: 'por-categoria', rotulo: 'Por categoria', icone: 'lucide:pie-chart', grupo: 'analise', descricao: 'Total e percentual das despesas por categoria.' },
  { slug: 'por-conta', rotulo: 'Por conta', icone: 'lucide:landmark', grupo: 'analise', descricao: 'Total e percentual das despesas por conta.' },
  { slug: 'comparativo-categoria', rotulo: 'Comparativo', icone: 'lucide:git-compare', grupo: 'analise', descricao: 'Mês atual × mês anterior × média dos 3 meses anteriores, por categoria.' },
  { slug: 'maiores-despesas', rotulo: 'Maiores despesas', icone: 'lucide:arrow-up-wide-narrow', grupo: 'analise', descricao: 'As maiores despesas do período, da maior para a menor.' },
  { slug: 'matriz-categoria-mes', rotulo: 'Categoria × mês', icone: 'lucide:table', grupo: 'analise', descricao: 'Gasto de cada categoria em cada mês do ano.' },
  { slug: 'frequencia-categoria', rotulo: 'Frequência', icone: 'lucide:repeat', grupo: 'analise', descricao: 'Quantidade de despesas e ticket médio por categoria.' },
  { slug: 'ritmo-mes', rotulo: 'Ritmo do mês', icone: 'lucide:activity', grupo: 'analise', descricao: 'Gasto até hoje comparado ao mesmo ponto do mês anterior, com projeção para o fim do mês.' },
  { slug: 'fatura', rotulo: 'Fatura', icone: 'lucide:credit-card', grupo: 'analise', descricao: 'Total, pago e saldo em aberto da fatura do cartão por competência.' },
  { slug: 'projecao-parcelas', rotulo: 'Parcelas', icone: 'lucide:layers', grupo: 'projecao', descricao: 'Quanto está comprometido em parcelas nos próximos meses e quanto diminui a cada mês.' },
  { slug: 'projecao-proximo-mes', rotulo: 'Próximo mês', icone: 'lucide:calendar-clock', grupo: 'projecao', descricao: 'Saldo previsto por conta e fatura prevista dos cartões.' },
  { slug: 'fixos-x-recebimentos', rotulo: 'Fixos × recebimentos', icone: 'lucide:gauge', grupo: 'projecao', descricao: 'Quanto dos recebimentos já está comprometido com gastos fixos e parcelas.' },
  { slug: 'evolucao-mensal', rotulo: 'Evolução mensal', icone: 'lucide:bar-chart-3', grupo: 'gerais', descricao: 'Receitas, despesas e saldo mês a mês ao longo do ano.' },
  { slug: 'orcamentos', rotulo: 'Orçamentos', icone: 'lucide:target', grupo: 'gerais', descricao: 'Limite, valor gasto e percentual consumido de cada orçamento.' },
  { slug: 'extrato', rotulo: 'Extrato por conta', icone: 'lucide:scroll-text', grupo: 'gerais', descricao: 'Entradas, saídas e lançamentos de uma conta no período.' },
]

export const abaPadrao = abas[0].slug
