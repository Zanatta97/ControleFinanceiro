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
  // Movimenta valor entre duas contas do ambiente (ex.: pagamento de fatura). Não é receita nem despesa.
  Transferencia = 3,
}

// Espelha backend/ControleFinanceiroAPI/Enums/TipoConta.cs — os valores precisam ser idênticos.
export enum TipoConta {
  Corrente = 1,
  Poupanca = 2,
  Investimento = 3,
  CartaoCredito = 4,
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
  observacao: string | null
  tipoTransacao: TipoTransacao
  categoriaId: string
  contaId: string
  contaDestinoId: string | null  // obrigatório só em Transferência
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
  contaDestinoId: string | null
  contaDestinoNome: string | null  // preenchido só nas listagens
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
  despesasCartao: number
  despesasOutras: number
  saldo: number
}

export interface EvolucaoMensalItemDTO {
  mes: number
  nomeMes: string
  totalReceitas: number
  totalDespesas: number
  despesasCartao: number
  despesasOutras: number
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

// Filtro dos relatórios que aceitam competência (mes/ano) OU período (dataInicio/dataFim) — nunca os dois
export interface FiltroCompetenciaOuPeriodo {
  mes?: number
  ano?: number
  dataInicio?: string  // YYYY-MM-DD
  dataFim?: string     // YYYY-MM-DD
}

export interface GastosPorContaResponse {
  contaId: string
  nomeConta: string
  tipoConta: TipoConta | null
  totalGasto: number
  percentual: number
}

export interface ComparativoCategoriaResponse {
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  gastoMes: number
  gastoMesAnterior: number
  mediaTresMesesAnteriores: number
  // Nulas quando a base de comparação é zero
  variacaoMesAnterior: number | null
  variacaoMedia: number | null
}

export interface MaiorDespesaResponse {
  transacaoId: string
  descricao: string | null
  valor: number
  data: string
  mesCompetencia: string  // YYYY-MM-DD
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  contaId: string
  nomeConta: string
}

export interface MatrizCategoriaValorMes {
  mes: number
  nomeMes: string
  valor: number
}

export interface MatrizCategoriaLinha {
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  meses: MatrizCategoriaValorMes[]  // sempre 12 itens
  totalAno: number
}

export interface MatrizCategoriaMesResponse {
  ano: number
  categorias: MatrizCategoriaLinha[]
  totaisPorMes: MatrizCategoriaValorMes[]
  totalAno: number
}

export interface FrequenciaCategoriaResponse {
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  quantidade: number
  totalGasto: number
  ticketMedio: number
}

export interface RitmoMesResponse {
  dataReferencia: string
  diasDecorridos: number
  diasNoMes: number
  gastoAteHoje: number
  diaComparadoMesAnterior: number
  gastoMesAnteriorAteMesmoDia: number
  // Nula quando o mês anterior não teve gasto até o dia comparado
  variacaoPercentual: number | null
  gastoTotalMesAnterior: number
  projecaoFimMes: number
}

export interface FaturaCompetenciaResponse {
  contaId: string
  nomeConta: string
  mes: number
  ano: number
  totalDespesas: number
  totalPago: number
  // Negativo indica pagamento acima do valor da fatura
  saldoEmAberto: number
}

export interface ProjecaoParcelasMes {
  mes: number
  ano: number
  nomeMes: string
  totalParcelas: number
  quantidadeParcelas: number
  totalParcelasTerminando: number
  quantidadeParcelasTerminando: number
  // Positivo = o compromisso caiu; negativo = subiu
  reducaoEmRelacaoAoMesAnterior: number
}

export interface CompraParceladaAtiva {
  descricao: string | null
  contaId: string
  nomeConta: string
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  dataCompra: string
  valorParcela: number
  parcelaAtual: number  // 0 quando a compra ainda não começou
  totalParcelas: number
  parcelasRestantes: number
  valorRestante: number
  ultimaCompetencia: string  // YYYY-MM-DD
}

export interface ProjecaoParcelasResponse {
  mesReferencia: number
  anoReferencia: number
  quantidadeMeses: number
  totalParcelasMesReferencia: number
  totalRestante: number
  meses: ProjecaoParcelasMes[]
  comprasAtivas: CompraParceladaAtiva[]
}

export interface ProjecaoConta {
  contaId: string
  nomeConta: string
  tipoConta: TipoConta
  saldoInicialPrevisto: number
  entradasLancadas: number
  saidasLancadas: number
  fixosEstimados: number
  recebimentosEstimados: number
  saldoFinalPrevisto: number
  // Só para cartão de crédito
  faturaPrevista: number | null
}

export interface ProjecaoProximoMesResponse {
  mes: number
  ano: number
  receitasPrevistas: number
  despesasPrevistas: number
  totalParcelas: number
  resultadoPrevisto: number
  saldoPrevisto: number
  contas: ProjecaoConta[]
}

export interface FixosRecebimentosCategoria {
  categoriaId: string
  nomeCategoria: string
  cor: string | null
  total: number
  grupo: string  // "Fixo" ou "Recebimento"
}

export interface FixosRecebimentosResponse {
  mes: number
  ano: number
  recebimentos: number
  outrasReceitas: number
  gastosFixos: number
  parcelas: number
  gastosVariaveis: number
  totalDespesas: number
  saldo: number
  // Nulos quando não houve recebimento no mês
  percentualComprometido: number | null
  percentualFixosSobreRecebimentos: number | null
  categorias: FixosRecebimentosCategoria[]
}

export interface ApiLogResponse {
  id: number
  timestamp: string
  method: string
  path: string
  queryString: string | null
  statusCode: number
  exceptionMessage: string | null
  isError: boolean
  elapsedMs: number
  userId: string | null
  requestBody: string | null
  responseBody: string | null
}
