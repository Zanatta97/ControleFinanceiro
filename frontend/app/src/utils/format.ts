export function formatCurrency(value: number): string {
  return new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' }).format(value)
}

export function formatDate(iso: string): string {
  return new Date(iso).toLocaleDateString('pt-BR')
}

export function formatDateTime(iso: string): string {
  return new Date(iso).toLocaleString('pt-BR')
}

export function monthName(mes: number): string {
  return new Date(2000, mes - 1, 1).toLocaleDateString('pt-BR', { month: 'long' })
}

// "2025-07-01" → "jul/2025"
export function formatMesCompetencia(iso: string | null | undefined): string {
  if (!iso) return '-'
  const [year, month] = iso.split('-')
  return new Date(Number(year), Number(month) - 1, 1)
    .toLocaleDateString('pt-BR', { month: 'short', year: 'numeric' })
    .replace('. de ', '/')
    .replace('.', '')
}
