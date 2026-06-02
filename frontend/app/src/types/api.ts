export interface ApiResponse<T> {
  statusCode: number
  errorMessage: string
  success: boolean
  statusMessage: string | null
  timestamp: string
  dados: T | null
}

// Enums
export enum TipoTransacao {
  Receita = 1,
  Despesa = 2,
}

export enum TipoConta {
  Corrente = 0,
  Poupanca = 1,
  Carteira = 2,
  Cartao = 3,
}

export enum StatusOrcamento {
  Ativo = 1,
  Encerrado = 2,
}

// Auth
export interface UsuarioLoginRequest {
  email: string
  senha: string
}

export interface UsuarioRegisterRequest {
  nome: string
  email: string
  senha: string
  confirmacaoSenha: string
}

export interface TokenDTO {
  accessToken: string | null
  refreshToken: string | null
}

// Resposta do /api/Auth/login — .NET serializa o objeto anônimo em camelCase
export interface LoginTokenDTO {
  token: string
  refreshToken: string
  expiration: string
}

// Usuario
export interface UsuarioResumoDTO {
  id: string | null
  nome: string | null
  email: string | null
}

// Ambiente
export interface AmbienteRequest {
  nome: string
}

export interface AmbienteMembroResponse {
  usuario: UsuarioResumoDTO | null
  role: string | null
}

export interface AmbienteResponse {
  id: string
  nome: string | null
  dataCriacao: string | null
  membros: AmbienteMembroResponse[]
}

// Categoria
export interface CategoriaRequest {
  nome: string
  cor: string
  urlIcone?: string
}

export interface CategoriaResponse {
  id: string
  nome: string | null
  cor: string | null
  urlIcone: string | null
  usuarioId: string | null
}

// Conta
export interface ContaRequest {
  nome: string
  tipoConta: TipoConta
  saldo?: number
}

export interface ContaResponse {
  id: string
  nome: string
  tipoConta: TipoConta
  saldo: number
  usuarioId: string | null
}

// Transacao
export interface TransacaoRequest {
  descricao: string
  valor: number
  data: string
  observacao: string
  tipoTransacao: TipoTransacao
  categoriaId: string
  contaId: string
  mesCompetencia: string  // YYYY-MM-DD (dia 1 do mês)
  parcelas: number
}

export interface TransacaoResponse {
  id: string
  descricao: string | null
  valor: number
  data: string
  observacao: string | null
  tipoTransacao: TipoTransacao
  categoriaId: string
  categoriaNome: string | null
  contaId: string
  contaNome: string | null
  usuarioId: string | null
  mesCompetencia: string | null  // YYYY-MM-DD
}

// Orcamento
export interface OrcamentoRequest {
  nome: string
  descricao: string
  valorLimite: number
  dataLimite: string
  statusOrcamento: StatusOrcamento
  categoriaId: string
}

export interface OrcamentoResponse {
  id: string
  nome: string | null
  descricao: string | null
  valorLimite: number
  dataLimite: string
  statusOrcamento: StatusOrcamento
  categoriaId: string
  usuarioId: string | null
}

export interface OrcamentoStatusResponse {
  id: string
  nomeOrcamento: string
  descricao: string | null
  categoriaId: string
  nomeCategoria: string
  valorLimite: number
  valorGasto: number
  percentual: number
  dataLimite: string
  status: StatusOrcamento
}

// Relatórios
export interface ResumoFinanceiroResponse {
  mes: number
  ano: number
  totalReceitas: number
  totalDespesas: number
  saldo: number
}

export interface EvolucaoMensalItemDTO {
  mes: number
  nomeMes: string
  totalReceitas: number
  totalDespesas: number
  saldo: number
}

export interface EvolucaoMensalResponse {
  ano: number
  meses: EvolucaoMensalItemDTO[]
}

export interface GastoPorCategoriaResponse {
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  totalGasto: number
  percentual: number
}

export interface ExtratoContaResponse {
  contaId: string
  nomeConta: string
  tipoConta: TipoConta
  saldoAtual: number
  dataInicio: string
  dataFim: string
  totalEntradas: number
  totalSaidas: number
  transacoes: TransacaoResponse[]
}
