interface Props {
  color?: string
  children: string
}

export default function Badge({ color, children }: Props) {
  const style = color ? { backgroundColor: color + '22', color } : undefined
  return (
    <span
      className="inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium"
      style={style ?? { backgroundColor: '#e5e7eb', color: '#374151' }}
    >
      {children}
    </span>
  )
}
