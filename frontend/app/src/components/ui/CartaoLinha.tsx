import type { ReactNode } from 'react'

// Versão em cartões das tabelas largas, usada abaixo de md (768px).
// No desktop a tabela continua; a lista fica escondida a partir de md.

export interface CampoCartao {
  rotulo: string
  valor: ReactNode
  /** Ocupa as duas colunas da grade — para conteúdo que não cabe em meia largura. */
  largo?: boolean
}

interface ListaCartoesProps {
  children: ReactNode
  className?: string
}

/** Lista de CartaoLinha, visível só abaixo de md (no desktop aparece a tabela). */
export function ListaCartoes({ children, className = '' }: ListaCartoesProps) {
  return <ul className={`divide-y divide-fin-border md:hidden ${className}`}>{children}</ul>
}

interface Props {
  titulo: ReactNode
  /** Elemento antes do título: cor da categoria, ícone. */
  marcador?: ReactNode
  subtitulo?: ReactNode
  /** Valor principal, alinhado à direita do título. Recebe o elemento já formatado (cor, fonte mono). */
  destaque?: ReactNode
  /** Demais colunas da linha, em pares rótulo/valor numa grade de 2 colunas. */
  campos?: CampoCartao[]
  /** Conteúdo livre entre o topo e os campos (barra de proporção, grade própria). */
  children?: ReactNode
  /** Botões de ação (editar, excluir), no rodapé do cartão. */
  acoes?: ReactNode
  /** 'total' reproduz o destaque da linha de totais da tabela. */
  variante?: 'padrao' | 'total'
}

/** Uma linha de tabela larga desenhada como cartão, para telas pequenas. */
export default function CartaoLinha({
  titulo,
  marcador,
  subtitulo,
  destaque,
  campos,
  children,
  acoes,
  variante = 'padrao',
}: Props) {
  const total = variante === 'total'

  return (
    <li className={`px-4 py-3.5 ${total ? 'bg-fin-surface-2' : ''}`}>
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0 flex-1">
          <div
            className={`flex items-start gap-2 ${
              total
                ? 'text-xs font-semibold uppercase leading-5 tracking-wide text-fin-text-muted'
                : 'text-sm font-medium text-fin-text-primary'
            }`}
          >
            {/* Altura de uma linha de texto: o marcador fica alinhado à primeira linha do título */}
            {marcador && <span className="flex h-5 flex-none items-center">{marcador}</span>}
            <span className="min-w-0 break-words">{titulo}</span>
          </div>
          {subtitulo && <div className="mt-0.5 break-words text-xs text-fin-text-muted">{subtitulo}</div>}
        </div>
        {destaque != null && <div className="flex-none whitespace-nowrap text-right text-sm leading-5">{destaque}</div>}
      </div>

      {children && <div className="mt-2.5">{children}</div>}

      {campos && campos.length > 0 && (
        <dl className="mt-3 grid grid-cols-2 gap-x-4 gap-y-2.5">
          {campos.map((c, i) => (
            <div key={i} className={`min-w-0 ${c.largo ? 'col-span-2' : ''}`}>
              <dt className="text-[10.5px] font-medium uppercase tracking-wide text-fin-text-muted">{c.rotulo}</dt>
              <dd className="mt-0.5 break-words text-[13px] text-fin-text-secondary">{c.valor}</dd>
            </div>
          ))}
        </dl>
      )}

      {acoes && <div className="mt-2.5 flex justify-end gap-1">{acoes}</div>}
    </li>
  )
}
