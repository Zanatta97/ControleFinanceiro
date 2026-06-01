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
