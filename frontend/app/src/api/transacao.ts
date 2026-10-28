import api from './client'
import type { ApiResponse, TransacaoRequest, TransacaoResponse, TipoTransacao } from '../types/api'

/** Transações de todos os ambientes (só Admin). */
export const listarTodas = () =>
  api.get<ApiResponse<TransacaoResponse[]>>('/Transacao')

/** Todas as transações do ambiente ativo, com nomes de categoria e contas. */
export const listarDoAmbiente = () =>
  api.get<ApiResponse<TransacaoResponse[]>>('/Transacao/ambiente')

/** Transações com origem na conta (não inclui transferências em que ela é só o destino). */
export const listarPorConta = (contaId: string) =>
  api.get<ApiResponse<TransacaoResponse[]>>(`/Transacao/conta/${contaId}`)

/** Transações com data entre `inicio` e `fim` (ISO); fim sem hora inclui o dia inteiro. */
export const listarPorPeriodo = (inicio: string, fim: string) =>
  api.get<ApiResponse<TransacaoResponse[]>>('/Transacao/periodo', { params: { inicio, fim } })

/** Transações de um tipo. */
export const listarPorTipo = (tipo: TipoTransacao) =>
  api.get<ApiResponse<TransacaoResponse[]>>(`/Transacao/tipo/${tipo}`)

/** Uma transação do ambiente ativo. */
export const buscarPorId = (id: string) =>
  api.get<ApiResponse<TransacaoResponse>>(`/Transacao/${id}`)

/**
 * Cria a transação; com `parcelas` > 1 o backend cria uma por parcela, em competências consecutivas.
 * Atenção: o backend responde sempre com uma LISTA em `dados`, embora o tipo declare um item só.
 */
export const criar = (data: TransacaoRequest) =>
  api.post<ApiResponse<TransacaoResponse>>('/Transacao', data)

/**
 * Edita uma única transação (não as outras parcelas). Envie `parcelas: 1`: com valor maior o backend
 * acrescenta o prefixo "Parcela 1/N" à observação.
 */
export const atualizar = (id: string, data: TransacaoRequest) =>
  api.put<ApiResponse<TransacaoResponse>>(`/Transacao/${id}`, data)

/** Exclui uma única transação. */
export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Transacao/${id}`)
