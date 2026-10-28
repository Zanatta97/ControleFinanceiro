import { describe, expect, it } from 'vitest'
import {
  formatCurrency,
  formatDate,
  formatDateTime,
  formatMesCompetencia,
  formatPercent,
  formatVariacao,
  monthName,
} from './format'

// O Intl usa espaço inquebrável entre "R$" e o número; normaliza para comparar
const semNbsp = (texto: string) => texto.replace(/\s/g, ' ')

describe('formatCurrency', () => {
  it('formata em reais no padrão pt-BR', () => {
    expect(semNbsp(formatCurrency(1234.56))).toBe('R$ 1.234,56')
  })

  it('formata zero com duas casas', () => {
    expect(semNbsp(formatCurrency(0))).toBe('R$ 0,00')
  })

  it('arredonda para centavos', () => {
    expect(semNbsp(formatCurrency(10.005))).toMatch(/^R\$ 10,0[01]$/)
    expect(semNbsp(formatCurrency(1.999))).toBe('R$ 2,00')
  })

  it('mantém o sinal de valores negativos', () => {
    expect(semNbsp(formatCurrency(-50))).toBe('-R$ 50,00')
  })

  it('separa milhões com ponto', () => {
    expect(semNbsp(formatCurrency(1234567.8))).toBe('R$ 1.234.567,80')
  })
})

describe('formatDate', () => {
  it('formata data e hora local como dd/mm/aaaa', () => {
    expect(formatDate('2026-03-15T10:30:00')).toBe('15/03/2026')
  })

  it('converte instante UTC para o fuso local', () => {
    // 01:00 UTC ainda é dia 14 em São Paulo (UTC-3)
    expect(formatDate('2026-03-15T01:00:00Z')).toBe('14/03/2026')
  })
})

describe('formatDateTime', () => {
  it('inclui a hora no padrão pt-BR', () => {
    expect(formatDateTime('2026-03-15T10:30:45')).toBe('15/03/2026, 10:30:45')
  })
})

describe('monthName', () => {
  it.each([
    [1, 'janeiro'],
    [3, 'março'],
    [12, 'dezembro'],
  ])('mês %i vira "%s"', (mes, nome) => {
    expect(monthName(mes)).toBe(nome)
  })
})

describe('formatMesCompetencia', () => {
  it('converte YYYY-MM-DD em mês abreviado/ano', () => {
    expect(formatMesCompetencia('2025-07-01')).toBe('jul/2025')
  })

  it('não depende do dia nem da hora', () => {
    expect(formatMesCompetencia('2025-12-31T23:59:59')).toBe('dez/2025')
  })

  it('maio, que não tem ponto na abreviação, fica correto', () => {
    expect(formatMesCompetencia('2026-05-01')).toBe('mai/2026')
  })

  it.each([null, undefined, ''])('valor vazio (%s) vira "-"', (valor) => {
    expect(formatMesCompetencia(valor)).toBe('-')
  })
})

describe('formatPercent', () => {
  it('usa vírgula decimal e uma casa por padrão', () => {
    expect(formatPercent(12.5)).toBe('12,5%')
    expect(formatPercent(3)).toBe('3,0%')
  })

  it('respeita o número de casas', () => {
    expect(formatPercent(12.3456, 2)).toBe('12,35%')
    expect(formatPercent(12.6, 0)).toBe('13%')
  })

  it('mantém o sinal negativo', () => {
    expect(formatPercent(-4.25, 1)).toBe('-4,3%')
  })

  it.each([null, undefined, Number.NaN, Number.POSITIVE_INFINITY, Number.NEGATIVE_INFINITY])(
    'valor inválido (%s) vira travessão, nunca "NaN"',
    (valor) => {
      expect(formatPercent(valor)).toBe('—')
    },
  )
})

describe('formatVariacao', () => {
  it('prefixa "+" em variação positiva', () => {
    expect(formatVariacao(12.5)).toBe('+12,5%')
  })

  it('mantém "-" em variação negativa', () => {
    expect(formatVariacao(-3)).toBe('-3,0%')
  })

  it('zero fica sem sinal', () => {
    expect(formatVariacao(0)).toBe('0,0%')
  })

  it('respeita o número de casas', () => {
    expect(formatVariacao(7.777, 2)).toBe('+7,78%')
  })

  it.each([null, undefined, Number.NaN, Number.POSITIVE_INFINITY])('valor inválido (%s) vira travessão', (valor) => {
    expect(formatVariacao(valor)).toBe('—')
  })
})
