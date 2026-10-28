import { describe, expect, it } from 'vitest'
import { jwtDecode } from './jwt'
import { criarToken } from '../test/helpers'

describe('jwtDecode', () => {
  it('devolve as claims do payload', () => {
    const token = criarToken({ email: 'ana@exemplo.com', ambiente_id: 'abc-123', exp: 1900000000 })
    expect(jwtDecode(token)).toEqual({ email: 'ana@exemplo.com', ambiente_id: 'abc-123', exp: 1900000000 })
  })

  it('aceita base64url com "-" e "_" e sem padding', () => {
    // Payload escolhido para gerar "-" / "_" no base64url e comprimento não múltiplo de 4
    const payload = { sub: '>>>???>>>???', n: 2 }
    const token = criarToken(payload)
    expect(token.split('.')[1]).toMatch(/[-_]/)
    expect(jwtDecode(token)).toEqual(payload)
  })

  it('mantém arrays de roles', () => {
    const token = criarToken({ role: ['Admin', 'Demo'] })
    expect(jwtDecode(token).role).toEqual(['Admin', 'Demo'])
  })

  it('lança erro com token sem payload', () => {
    expect(() => jwtDecode('somente-um-pedaco')).toThrow()
  })

  it('lança erro com payload que não é JSON', () => {
    expect(() => jwtDecode(`x.${btoa('não é json')}.y`)).toThrow()
  })

  it.skip('Bug: decodifica nomes acentuados (payload UTF-8) sem corromper os caracteres', () => {
    // atob devolve bytes como caracteres Latin-1; "João" chega como "JoÃ£o" no menu do usuário
    const token = criarToken({ name: 'João Conceição' })
    expect(jwtDecode(token).name).toBe('João Conceição')
  })
})
