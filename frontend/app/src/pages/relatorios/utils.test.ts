import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { TipoConta } from '../../types/api'
import {
  chaveFiltro,
  descricaoFiltro,
  erroPeriodo,
  filtroInicial,
  filtroParaParams,
  formatDataLocal,
  labelCompetencia,
  labelMesCurto,
  mesAtual,
  nomeTipoConta,
  proximoMes,
  somarMeses,
  toISODate,
  type FiltroPeriodoValor,
} from './utils'

const competencia: FiltroPeriodoValor = {
  modo: 'competencia',
  mes: 9,
  ano: 2026,
  dataInicio: '2026-09-01',
  dataFim: '2026-09-30',
}
const periodo: FiltroPeriodoValor = { ...competencia, modo: 'periodo' }

describe('toISODate', () => {
  it('formata a data local como YYYY-MM-DD com zero à esquerda', () => {
    expect(toISODate(new Date(2026, 0, 5))).toBe('2026-01-05')
  })

  it('usa o dia local, não o UTC (23h de 31/12 em São Paulo já é 01/01 em UTC)', () => {
    expect(toISODate(new Date(2025, 11, 31, 23, 30))).toBe('2025-12-31')
  })
})

describe('datas relativas a hoje', () => {
  beforeEach(() => {
    vi.useFakeTimers({ toFake: ['Date'] })
  })
  afterEach(() => {
    vi.useRealTimers()
  })

  it('mesAtual devolve mês (1–12) e ano de hoje', () => {
    vi.setSystemTime(new Date(2026, 2, 15))
    expect(mesAtual()).toEqual({ mes: 3, ano: 2026 })
  })

  it('proximoMes avança o mês', () => {
    vi.setSystemTime(new Date(2026, 2, 31))
    expect(proximoMes()).toEqual({ mes: 4, ano: 2026 })
  })

  it('proximoMes vira o ano em dezembro', () => {
    vi.setSystemTime(new Date(2026, 11, 10))
    expect(proximoMes()).toEqual({ mes: 1, ano: 2027 })
  })

  it('filtroInicial começa em competência do mês atual, com período do dia 1 até hoje', () => {
    vi.setSystemTime(new Date(2026, 8, 17, 10))
    expect(filtroInicial()).toEqual({
      modo: 'competencia',
      mes: 9,
      ano: 2026,
      dataInicio: '2026-09-01',
      dataFim: '2026-09-17',
    })
  })
})

describe('somarMeses', () => {
  it.each([
    [9, 2026, 1, { mes: 10, ano: 2026 }],
    [12, 2026, 1, { mes: 1, ano: 2027 }],
    [1, 2026, -1, { mes: 12, ano: 2025 }],
    [6, 2026, -18, { mes: 12, ano: 2024 }],
    [6, 2026, 0, { mes: 6, ano: 2026 }],
  ])('%i/%i + %i meses', (mes, ano, delta, esperado) => {
    expect(somarMeses(mes, ano, delta)).toEqual(esperado)
  })
})

describe('labelCompetencia', () => {
  it('escreve o mês por extenso com o ano', () => {
    expect(labelCompetencia(9, 2026)).toBe('setembro de 2026')
  })
})

describe('labelMesCurto', () => {
  it('abrevia mês e ano ("set/26")', () => {
    expect(labelMesCurto(9, 2026)).toBe('set/26')
  })

  it('maio fica sem ponto', () => {
    expect(labelMesCurto(5, 2030)).toBe('mai/30')
  })
})

describe('erroPeriodo', () => {
  it('competência nunca tem erro, mesmo com datas inválidas', () => {
    expect(erroPeriodo({ ...competencia, dataInicio: '', dataFim: '' })).toBeNull()
  })

  it('exige as duas datas no modo período', () => {
    expect(erroPeriodo({ ...periodo, dataInicio: '' })).toBe('Informe a data inicial e a final.')
    expect(erroPeriodo({ ...periodo, dataFim: '' })).toBe('Informe a data inicial e a final.')
  })

  it('recusa data inicial depois da final', () => {
    expect(erroPeriodo({ ...periodo, dataInicio: '2026-10-01', dataFim: '2026-09-30' })).toBe(
      'A data inicial deve ser anterior ou igual à final.',
    )
  })

  it('aceita datas iguais', () => {
    expect(erroPeriodo({ ...periodo, dataInicio: '2026-09-10', dataFim: '2026-09-10' })).toBeNull()
  })

  it('aceita período válido', () => {
    expect(erroPeriodo(periodo)).toBeNull()
  })
})

describe('filtroParaParams', () => {
  it('competência envia só mês e ano', () => {
    expect(filtroParaParams(competencia)).toEqual({ mes: 9, ano: 2026 })
  })

  it('período envia só as datas', () => {
    expect(filtroParaParams(periodo)).toEqual({ dataInicio: '2026-09-01', dataFim: '2026-09-30' })
  })
})

describe('chaveFiltro', () => {
  it('resume a competência', () => {
    expect(chaveFiltro(competencia)).toBe('c:9-2026')
  })

  it('resume o período', () => {
    expect(chaveFiltro(periodo)).toBe('p:2026-09-01_2026-09-30')
  })

  it('muda quando um valor do modo ativo muda e ignora o modo inativo', () => {
    expect(chaveFiltro({ ...competencia, mes: 10 })).not.toBe(chaveFiltro(competencia))
    expect(chaveFiltro({ ...competencia, dataFim: '2030-01-01' })).toBe(chaveFiltro(competencia))
  })
})

describe('descricaoFiltro', () => {
  it('descreve a competência', () => {
    expect(descricaoFiltro(competencia)).toBe('Competência de setembro de 2026')
  })

  it('descreve o período em datas locais, sem voltar um dia', () => {
    expect(descricaoFiltro(periodo)).toBe('De 01/09/2026 a 30/09/2026')
  })
})

describe('nomeTipoConta', () => {
  it('tem um nome para cada tipo de conta', () => {
    expect(nomeTipoConta[TipoConta.Corrente]).toBe('Corrente')
    expect(nomeTipoConta[TipoConta.Poupanca]).toBe('Poupança')
    expect(nomeTipoConta[TipoConta.Investimento]).toBe('Investimento')
    expect(nomeTipoConta[TipoConta.CartaoCredito]).toBe('Cartão de crédito')
  })
})

describe('formatDataLocal', () => {
  it('exibe data sem hora no fuso local, sem voltar um dia', () => {
    // new Date('2026-09-01') seria meia-noite UTC = 31/08 21h em São Paulo
    expect(formatDataLocal('2026-09-01')).toBe('01/09/2026')
  })

  it('ignora a parte de hora quando vier', () => {
    expect(formatDataLocal('2026-09-01T23:59:00Z')).toBe('01/09/2026')
  })
})
