export interface DonutSegment {
  /** Valor usado para calcular o ângulo da fatia (ex.: percentual 0-100). */
  value: number
  cor: string
}

interface Props {
  segments: DonutSegment[]
  /** Tamanho renderizado em px (o viewBox interno é fixo em 160). */
  size?: number
  stroke?: number
  /** Conteúdo centralizado (ex.: total). */
  children?: React.ReactNode
}

const R = 60
const CIRC = 2 * Math.PI * R

export default function Donut({ segments, size = 140, stroke = 20, children }: Props) {
  const total = segments.reduce((acc, s) => acc + (s.value > 0 ? s.value : 0), 0)
  let acc = 0

  return (
    <div className="relative flex-none" style={{ width: size, height: size }}>
      <svg width={size} height={size} viewBox="0 0 160 160" style={{ transform: 'rotate(-90deg)' }}>
        <circle cx="80" cy="80" r={R} fill="none" stroke="var(--fin-surface-2)" strokeWidth={stroke} />
        {total > 0 && segments.map((s, i) => {
          const frac = (s.value > 0 ? s.value : 0) / total
          const len = frac * CIRC
          const dashoffset = -acc * CIRC
          acc += frac
          return (
            <circle
              key={i}
              cx="80"
              cy="80"
              r={R}
              fill="none"
              stroke={s.cor}
              strokeWidth={stroke}
              strokeDasharray={`${len} ${CIRC - len}`}
              strokeDashoffset={dashoffset}
            />
          )
        })}
      </svg>
      {children && (
        <div className="absolute inset-0 flex flex-col items-center justify-center">{children}</div>
      )}
    </div>
  )
}
