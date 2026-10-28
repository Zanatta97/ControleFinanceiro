import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Transacoes from './Transacoes'
import { atualizar, criar, excluir, listarDoAmbiente } from '../api/transacao'
import { listarDoAmbiente as listarContas } from '../api/conta'
import { listarDoAmbiente as listarCategorias } from '../api/categoria'
import { TipoConta, TipoTransacao, type CategoriaResponse, type ContaResponse, type TransacaoResponse } from '../types/api'
import { adiado, erroHttp, respostaOk } from '../test/helpers'

vi.mock('../api/transacao', () => ({
  listarDoAmbiente: vi.fn(),
  criar: vi.fn(),
  atualizar: vi.fn(),
  excluir: vi.fn(),
}))
vi.mock('../api/conta', () => ({ listarDoAmbiente: vi.fn() }))
vi.mock('../api/categoria', () => ({ listarDoAmbiente: vi.fn() }))

const mListar = vi.mocked(listarDoAmbiente)
const mCriar = vi.mocked(criar)
const mAtualizar = vi.mocked(atualizar)
const mExcluir = vi.mocked(excluir)
const mContas = vi.mocked(listarContas)
const mCategorias = vi.mocked(listarCategorias)

const contas: ContaResponse[] = [
  { id: 'c1', nome: 'Nubank', tipoConta: TipoConta.CartaoCredito, saldo: 0, usuarioId: null },
  { id: 'c2', nome: 'Itaú', tipoConta: TipoConta.Corrente, saldo: 0, usuarioId: null },
]
const categorias: CategoriaResponse[] = [
  { id: 'cat1', nome: 'Mercado', cor: '#f00', urlIcone: null, usuarioId: null },
  { id: 'cat2', nome: 'Renda', cor: '#0f0', urlIcone: null, usuarioId: null },
]

function transacao(parcial: Partial<TransacaoResponse> & { id: string }): TransacaoResponse {
  return {
    descricao: 'Sem nome',
    valor: 10,
    data: '2026-03-01T12:00:00',
    observacao: null,
    tipoTransacao: TipoTransacao.Despesa,
    categoriaId: 'cat1',
    categoriaNome: 'Mercado',
    contaId: 'c1',
    contaNome: 'Nubank',
    contaDestinoId: null,
    contaDestinoNome: null,
    usuarioId: null,
    mesCompetencia: '2026-03-01',
    ...parcial,
  }
}

const supermercado = transacao({ id: 't1', descricao: 'Supermercado', valor: 250.5, data: '2026-03-10T15:00:00', observacao: 'Compra do mês' })
const salario = transacao({
  id: 't2',
  descricao: 'Salário março',
  valor: 5000,
  data: '2026-03-05T09:00:00',
  tipoTransacao: TipoTransacao.Receita,
  categoriaId: 'cat2',
  categoriaNome: 'Renda',
  contaId: 'c2',
  contaNome: 'Itaú',
  mesCompetencia: '2026-04-01',
})
const fatura = transacao({
  id: 't3',
  descricao: 'Pagamento fatura',
  valor: 1000,
  data: '2026-03-08T12:00:00',
  tipoTransacao: TipoTransacao.Transferencia,
  categoriaNome: null,
  contaId: 'c2',
  contaNome: 'Itaú',
  contaDestinoId: 'c1',
  contaDestinoNome: 'Nubank',
})

function carregar(transacoes: TransacaoResponse[] = [supermercado, salario, fatura]) {
  mListar.mockResolvedValue(respostaOk(transacoes))
  mContas.mockResolvedValue(respostaOk(contas))
  mCategorias.mockResolvedValue(respostaOk(categorias))
}

async function renderizarCarregado() {
  render(<Transacoes />)
  await waitFor(() => expect(screen.queryByText('Carregando...')).not.toBeInTheDocument())
}

/** Descrições das linhas da tabela (desktop), na ordem exibida. */
function linhasDaTabela() {
  const [, ...linhas] = within(screen.getByRole('table')).getAllByRole('row')
  return linhas.map((l) => within(l).getAllByRole('cell')[0].textContent)
}

const mesAtual = () => {
  const d = new Date()
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`
}

describe('Transacoes', () => {
  beforeEach(() => {
    for (const m of [mListar, mCriar, mAtualizar, mExcluir, mContas, mCategorias]) m.mockReset()
  })

  describe('carregamento', () => {
    it('mostra "Carregando..." enquanto busca', () => {
      mListar.mockReturnValue(adiado<never>().promise)
      mContas.mockReturnValue(adiado<never>().promise)
      mCategorias.mockReturnValue(adiado<never>().promise)
      render(<Transacoes />)
      expect(screen.getByText('Carregando...')).toBeInTheDocument()
    })

    it('busca transações, contas e categorias do ambiente', async () => {
      carregar()
      await renderizarCarregado()
      expect(mListar).toHaveBeenCalledTimes(1)
      expect(mContas).toHaveBeenCalledTimes(1)
      expect(mCategorias).toHaveBeenCalledTimes(1)
    })

    it('sem transações, mostra o estado vazio com atalho para criar', async () => {
      carregar([])
      await renderizarCarregado()
      expect(screen.getByText('Nenhuma transação encontrada.')).toBeInTheDocument()

      await userEvent.click(screen.getByRole('button', { name: 'Registrar primeira transação' }))
      expect(screen.getByRole('heading', { name: 'Nova Transação' })).toBeInTheDocument()
    })

    it('dados nulos da API contam como lista vazia', async () => {
      mListar.mockResolvedValue(respostaOk(null as unknown as TransacaoResponse[]))
      mContas.mockResolvedValue(respostaOk(contas))
      mCategorias.mockResolvedValue(respostaOk(categorias))
      await renderizarCarregado()
      expect(screen.getByText('Nenhuma transação encontrada.')).toBeInTheDocument()
    })

    it.skip('Bug: falha ao carregar mostra erro, e não "Nenhuma transação encontrada."', async () => {
      // pages/Transacoes.tsx:124-135 — load() descarta as rejeições do Promise.allSettled; sem estado
      // de erro, uma API fora do ar parece um ambiente sem transações.
      mListar.mockRejectedValue(erroHttp(500))
      mContas.mockResolvedValue(respostaOk(contas))
      mCategorias.mockResolvedValue(respostaOk(categorias))
      await renderizarCarregado()
      expect(screen.queryByText('Nenhuma transação encontrada.')).not.toBeInTheDocument()
      expect(screen.getByText(/erro|não foi possível/i)).toBeInTheDocument()
    })
  })

  describe('listagem', () => {
    it('mostra descrição, observação, valores com sinal e competência', async () => {
      carregar()
      await renderizarCarregado()
      const tabela = within(screen.getByRole('table'))

      expect(tabela.getByText('Supermercado')).toBeInTheDocument()
      expect(tabela.getByText('Compra do mês')).toBeInTheDocument()
      expect(tabela.getByText(/^-R\$\s250,50$/)).toBeInTheDocument()
      expect(tabela.getByText(/^\+R\$\s5\.000,00$/)).toBeInTheDocument()
      expect(tabela.getByText('abr/2026')).toBeInTheDocument()
      expect(tabela.getByText('10/03/2026')).toBeInTheDocument()
    })

    it('transferência aparece neutra (sem sinal) e com origem → destino', async () => {
      carregar()
      await renderizarCarregado()
      const linha = screen.getByRole('table').querySelector('tbody tr:nth-child(2)') as HTMLElement

      expect(within(linha).getByText('Pagamento fatura')).toBeInTheDocument()
      expect(within(linha).getByText(/^R\$\s1\.000,00$/)).toBeInTheDocument()
      expect(linha).toHaveTextContent('Itaú → Nubank')
    })

    it('também renderiza a versão em cartões para o celular', async () => {
      carregar()
      await renderizarCarregado()
      const cartoes = screen.getAllByRole('listitem')
      expect(cartoes).toHaveLength(3)
      expect(cartoes[0]).toHaveTextContent('Supermercado')
    })

    it('ordena por data, da mais recente para a mais antiga, por padrão', async () => {
      carregar()
      await renderizarCarregado()
      expect(linhasDaTabela()).toEqual([
        expect.stringContaining('Supermercado'),
        expect.stringContaining('Pagamento fatura'),
        expect.stringContaining('Salário março'),
      ])
    })

    it('clicar no cabeçalho ordena pela coluna e clicar de novo inverte', async () => {
      carregar()
      await renderizarCarregado()

      await userEvent.click(screen.getByRole('columnheader', { name: /Valor/ }))
      expect(linhasDaTabela()).toEqual([
        expect.stringContaining('Supermercado'),
        expect.stringContaining('Pagamento fatura'),
        expect.stringContaining('Salário março'),
      ])

      await userEvent.click(screen.getByRole('columnheader', { name: /Valor/ }))
      expect(linhasDaTabela()[0]).toContain('Salário março')
    })

    it('pagina de 20 em 20', async () => {
      carregar(Array.from({ length: 25 }, (_, i) => transacao({ id: `x${i}`, descricao: `Item ${i}` })))
      await renderizarCarregado()
      expect(screen.getByText('1–20 de 25')).toBeInTheDocument()
      expect(linhasDaTabela()).toHaveLength(20)

      await userEvent.click(screen.getByRole('button', { name: '›' }))
      expect(screen.getByText('21–25 de 25')).toBeInTheDocument()
      expect(linhasDaTabela()).toHaveLength(5)
    })
  })

  describe('filtros', () => {
    it('filtra por tipo e "Limpar" volta a mostrar tudo', async () => {
      carregar()
      await renderizarCarregado()
      expect(screen.queryByRole('button', { name: 'Limpar' })).not.toBeInTheDocument()

      await userEvent.click(screen.getByRole('button', { name: 'Receitas' }))
      expect(linhasDaTabela()).toEqual([expect.stringContaining('Salário março')])

      await userEvent.click(screen.getByRole('button', { name: 'Limpar' }))
      expect(linhasDaTabela()).toHaveLength(3)
    })

    it('filtra transferências', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Transferências' }))
      expect(linhasDaTabela()).toEqual([expect.stringContaining('Pagamento fatura')])
    })

    it('filtro por conta inclui transferências em que ela é o destino', async () => {
      carregar()
      await renderizarCarregado()
      const [, filtroConta] = screen.getAllByDisplayValue('Todas')
      await userEvent.selectOptions(filtroConta, 'c1')
      expect(linhasDaTabela()).toEqual([
        expect.stringContaining('Supermercado'),
        expect.stringContaining('Pagamento fatura'),
      ])
    })

    it('filtra por categoria', async () => {
      carregar()
      await renderizarCarregado()
      const [filtroCategoria] = screen.getAllByDisplayValue('Todas')
      await userEvent.selectOptions(filtroCategoria, 'cat2')
      expect(linhasDaTabela()).toEqual([expect.stringContaining('Salário março')])
    })

    it('filtro sem resultado mostra o estado vazio', async () => {
      carregar([supermercado])
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Receitas' }))
      expect(screen.getByText('Nenhuma transação encontrada.')).toBeInTheDocument()
    })
  })

  describe('nova transação', () => {
    it('valida a descrição antes de enviar', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      expect(screen.getByText('Descrição é obrigatória.')).toBeInTheDocument()
      expect(mCriar).not.toHaveBeenCalled()
    })

    it('sem contas cadastradas, pede uma conta', async () => {
      mListar.mockResolvedValue(respostaOk([]))
      mContas.mockResolvedValue(respostaOk([]))
      mCategorias.mockResolvedValue(respostaOk(categorias))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Padaria')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
      expect(screen.getByText('Selecione uma conta.')).toBeInTheDocument()
    })

    it('sem categorias cadastradas, pede uma categoria', async () => {
      mListar.mockResolvedValue(respostaOk([]))
      mContas.mockResolvedValue(respostaOk(contas))
      mCategorias.mockResolvedValue(respostaOk([]))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Padaria')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
      expect(screen.getByText('Selecione uma categoria.')).toBeInTheDocument()
    })

    it('cria a despesa com os padrões do formulário e recarrega a lista', async () => {
      carregar()
      mCriar.mockResolvedValue(respostaOk(supermercado))
      await renderizarCarregado()

      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Padaria')
      await userEvent.type(screen.getByLabelText('Valor (R$)'), '1590')
      await userEvent.type(screen.getByLabelText('Observação'), '  pão  ')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mCriar).toHaveBeenCalledTimes(1))
      const payload = mCriar.mock.calls[0][0]
      expect(payload).toMatchObject({
        descricao: 'Padaria',
        valor: 15.9,
        tipoTransacao: TipoTransacao.Despesa,
        contaId: 'c1',
        categoriaId: 'cat1',
        contaDestinoId: null,
        observacao: 'pão',
        parcelas: 1,
        mesCompetencia: `${mesAtual()}-01`,
      })
      expect(payload.data).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:00\.000Z$/)
      await waitFor(() => expect(mListar).toHaveBeenCalledTimes(2))
    })

    it('observação só com espaços vai como null', async () => {
      carregar()
      mCriar.mockResolvedValue(respostaOk(supermercado))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Padaria')
      await userEvent.type(screen.getByLabelText('Observação'), '   ')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
      await waitFor(() => expect(mCriar).toHaveBeenCalled())
      expect(mCriar.mock.calls[0][0].observacao).toBeNull()
    })

    it('parcelado mostra a prévia das competências e o rótulo do botão', async () => {
      carregar()
      mCriar.mockResolvedValue(respostaOk(supermercado))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))

      // O campo nunca fica vazio (volta a 1), então o valor é trocado de uma vez
      fireEvent.change(screen.getByRole('spinbutton'), { target: { value: '3' } })

      expect(screen.getByRole('button', { name: 'Criar 3 parcelas' })).toBeInTheDocument()
      expect(screen.getByText(/Serão criadas/)).toHaveTextContent('Serão criadas 3 transações')

      await userEvent.type(screen.getByLabelText('Descrição'), 'Geladeira')
      await userEvent.click(screen.getByRole('button', { name: 'Criar 3 parcelas' }))
      await waitFor(() => expect(mCriar).toHaveBeenCalled())
      expect(mCriar.mock.calls[0][0].parcelas).toBe(3)
    })

    it('parcelas inválidas viram 1', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      const parcelas = screen.getByRole('spinbutton')
      await userEvent.clear(parcelas)
      expect(parcelas).toHaveValue(1)
    })

    it('transferência exige a conta de destino', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Pagar fatura')
      await userEvent.selectOptions(screen.getByLabelText('Tipo'), String(TipoTransacao.Transferencia))

      expect(screen.getByLabelText('Conta de origem')).toBeInTheDocument()
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
      expect(screen.getByText('Selecione a conta de destino.')).toBeInTheDocument()
      expect(mCriar).not.toHaveBeenCalled()
    })

    it('o destino da transferência não oferece a própria conta de origem', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.selectOptions(screen.getByLabelText('Tipo'), String(TipoTransacao.Transferencia))

      const destino = screen.getByLabelText('Conta de destino')
      const opcoes = within(destino).getAllByRole('option').map((o) => o.textContent)
      expect(opcoes).toEqual(['Selecione a conta de destino', 'Itaú'])
    })

    it('trocar a origem para a conta de destino limpa o destino', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.selectOptions(screen.getByLabelText('Tipo'), String(TipoTransacao.Transferencia))
      await userEvent.selectOptions(screen.getByLabelText('Conta de destino'), 'c2')
      await userEvent.selectOptions(screen.getByLabelText('Conta de origem'), 'c2')

      expect(screen.getByLabelText('Conta de destino')).toHaveValue('')
    })

    it('cria a transferência com o destino', async () => {
      carregar()
      mCriar.mockResolvedValue(respostaOk(fatura))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Pagar fatura')
      await userEvent.selectOptions(screen.getByLabelText('Tipo'), String(TipoTransacao.Transferencia))
      await userEvent.selectOptions(screen.getByLabelText('Conta de destino'), 'c2')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mCriar).toHaveBeenCalled())
      expect(mCriar.mock.calls[0][0]).toMatchObject({
        tipoTransacao: TipoTransacao.Transferencia,
        contaId: 'c1',
        contaDestinoId: 'c2',
      })
    })

    it('voltar de transferência para despesa descarta o destino', async () => {
      carregar()
      mCriar.mockResolvedValue(respostaOk(supermercado))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Mercado')
      await userEvent.selectOptions(screen.getByLabelText('Tipo'), String(TipoTransacao.Transferencia))
      await userEvent.selectOptions(screen.getByLabelText('Conta de destino'), 'c2')
      await userEvent.selectOptions(screen.getByLabelText('Tipo'), String(TipoTransacao.Despesa))
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mCriar).toHaveBeenCalled())
      expect(mCriar.mock.calls[0][0].contaDestinoId).toBeNull()
    })

    it('erro da API mostra a mensagem do backend e mantém o formulário aberto', async () => {
      carregar()
      mCriar.mockRejectedValue(erroHttp(400, { errorMessage: 'Conta encerrada.' }))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Padaria')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      expect(await screen.findByText('Conta encerrada.')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Salvar' })).toBeEnabled()
      expect(mListar).toHaveBeenCalledTimes(1)
    })

    it('erro sem mensagem usa a padrão', async () => {
      carregar()
      mCriar.mockRejectedValue(new Error('Network Error'))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Padaria')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
      expect(await screen.findByText('Erro ao salvar transação.')).toBeInTheDocument()
    })

    describe('data padrão', () => {
      afterEach(() => {
        vi.useRealTimers()
      })

      it.skip('Bug: a data da compra começa na hora local atual, não na hora UTC', async () => {
        // pages/Transacoes.tsx:74 — new Date().toISOString().slice(0, 16) põe a hora UTC no campo local
        // (3h à frente em São Paulo); ao salvar ela é lida como local e a compra fica 3h no futuro.
        vi.useFakeTimers({ toFake: ['Date'] })
        vi.setSystemTime(new Date(2026, 2, 10, 12, 0))
        carregar()
        await renderizarCarregado()
        await userEvent.click(screen.getByRole('button', { name: 'Nova transação' }))
        expect(screen.getByLabelText('Data da compra')).toHaveValue('2026-03-10T12:00')
      })
    })
  })

  describe('edição', () => {
    it('abre o formulário preenchido, sem o campo de parcelas', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Editar')[0])

      expect(screen.getByRole('heading', { name: 'Editar Transação' })).toBeInTheDocument()
      expect(screen.getByLabelText('Descrição')).toHaveValue('Supermercado')
      expect(screen.getByLabelText('Valor (R$)')).toHaveValue('250,50')
      expect(screen.getByLabelText('Observação')).toHaveValue('Compra do mês')
      expect(screen.queryByRole('spinbutton')).not.toBeInTheDocument()
    })

    it('salvar envia atualizar com o id e uma parcela', async () => {
      carregar()
      mAtualizar.mockResolvedValue(respostaOk(supermercado))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Editar')[0])
      await userEvent.clear(screen.getByLabelText('Descrição'))
      await userEvent.type(screen.getByLabelText('Descrição'), 'Atacadão')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mAtualizar).toHaveBeenCalled())
      expect(mAtualizar).toHaveBeenCalledWith(
        't1',
        expect.objectContaining({ descricao: 'Atacadão', valor: 250.5, parcelas: 1, mesCompetencia: '2026-03-01' }),
      )
      expect(mCriar).not.toHaveBeenCalled()
    })

    it.skip('Bug: salvar sem mexer na data mantém o mesmo instante', async () => {
      // pages/Transacoes.tsx:158 e :190 — a data UTC da API vai para o campo como se fosse local
      // e volta com toISOString(): cada edição adianta a transação em 3h.
      carregar([transacao({ id: 't9', descricao: 'Almoço', data: '2026-03-10T15:00:00Z' })])
      mAtualizar.mockResolvedValue(respostaOk(supermercado))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Editar')[0])
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mAtualizar).toHaveBeenCalled())
      expect(mAtualizar.mock.calls[0][1].data).toBe('2026-03-10T15:00:00.000Z')
    })
  })

  describe('exclusão', () => {
    it('pede confirmação, exclui e recarrega', async () => {
      carregar()
      mExcluir.mockResolvedValue(respostaOk(null))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Excluir')[0])

      expect(screen.getByRole('heading', { name: 'Excluir transação' })).toBeInTheDocument()
      expect(screen.getByText('Supermercado', { selector: 'strong' })).toBeInTheDocument()

      await userEvent.click(screen.getByText('Excluir', { selector: 'button' }))
      await waitFor(() => expect(mExcluir).toHaveBeenCalledWith('t1'))
      await waitFor(() => expect(screen.queryByRole('heading', { name: 'Excluir transação' })).not.toBeInTheDocument())
      expect(mListar).toHaveBeenCalledTimes(2)
    })

    it('cancelar fecha a confirmação sem excluir', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Excluir')[0])
      const dialogo = screen.getByRole('heading', { name: 'Excluir transação' }).closest('.fixed') as HTMLElement
      await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }))

      expect(screen.queryByRole('heading', { name: 'Excluir transação' })).not.toBeInTheDocument()
      expect(mExcluir).not.toHaveBeenCalled()
    })

    it.skip('Bug: falha ao excluir avisa o usuário', async () => {
      // pages/Transacoes.tsx:208-218 — handleDelete não tem catch: o erro vira rejeição não tratada
      // e o diálogo fica aberto sem nenhuma mensagem.
      carregar()
      mExcluir.mockRejectedValue(erroHttp(500, { errorMessage: 'Falha ao excluir.' }))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Excluir')[0])
      await userEvent.click(screen.getByText('Excluir', { selector: 'button' }))
      expect(await screen.findByText(/Falha ao excluir|Erro ao excluir/)).toBeInTheDocument()
    })
  })
})
