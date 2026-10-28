import api from './client'
import type { ApiResponse, OrcamentoRequest, OrcamentoResponse, StatusOrcamento } from '../types/api'

/** Orçamentos de todos os ambientes (só Admin). */
export const listarTodos = () =>
  api.get<ApiResponse<OrcamentoResponse[]>>('/Orcamento')

/** Orçamentos do ambiente ativo, de qualquer status. O consumo vem de `statusOrcamentos` (api/relatorio). */
export const listarDoAmbiente = () =>
  api.get<ApiResponse<OrcamentoResponse[]>>('/Orcamento/ambiente')

/** Orçamentos do ambiente com o status informado. */
export const listarPorStatus = (status: StatusOrcamento) =>
  api.get<ApiResponse<OrcamentoResponse[]>>(`/Orcamento/status/${status}`)

/** Um orçamento do ambiente ativo. */
export const buscarPorId = (id: string) =>
  api.get<ApiResponse<OrcamentoResponse>>(`/Orcamento/${id}`)

/** Cria o orçamento no ambiente ativo. */
export const criar = (data: OrcamentoRequest) =>
  api.post<ApiResponse<OrcamentoResponse>>('/Orcamento', data)

/** Atualiza o orçamento. */
export const atualizar = (id: string, data: OrcamentoRequest) =>
  api.put<ApiResponse<OrcamentoResponse>>(`/Orcamento/${id}`, data)

/** Exclui o orçamento. */
export const excluir = (id: string) =>
  api.delete<ApiResponse<null>>(`/Orcamento/${id}`)
