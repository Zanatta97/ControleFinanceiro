import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import Orcamentos from './Orcamentos'
import { atualizar, criar, excluir, listarDoAmbiente } from '../api/orcamento'
import { listarDoAmbiente as listarCategorias } from '../api/categoria'
import { statusOrcamentos } from '../api/relatorio'
import {
  StatusOrcamento,
  type CategoriaResponse,
  type OrcamentoResponse,
  type OrcamentoStatusResponse,
} from '../types/api'
import { adiado, erroHttp, respostaOk } from '../test/helpers'

vi.mock('../api/orcamento', () => ({
  listarDoAmbiente: vi.fn(),
  criar: vi.fn(),
  atualizar: vi.fn(),
  excluir: vi.fn(),
}))
vi.mock('../api/categoria', () => ({ listarDoAmbiente: vi.fn() }))
vi.mock('../api/relatorio', () => ({ statusOrcamentos: vi.fn() }))

const mListar = vi.mocked(listarDoAmbiente)
const mCriar = vi.mocked(criar)
const mAtualizar = vi.mocked(atualizar)
const mExcluir = vi.mocked(excluir)
const mCategorias = vi.mocked(listarCategorias)
const mStatus = vi.mocked(statusOrcamentos)

const categorias: CategoriaResponse[] = [
  { id: 'cat1', nome: 'Mercado', cor: null, urlIcone: null, usuarioId: null },
  { id: 'cat2', nome: 'Lazer', cor: null, urlIcone: null, usuarioId: null },
]

const alimentacao: OrcamentoResponse = {
  id: 'o1',
  nome: 'Alimentação',
  descricao: 'Mercado e feira',
  valorLimite: 1000,
  dataLimite: '2026-03-31T23:59:00',
  statusOrcamento: StatusOrcamento.Ativo,
  categoriaId: 'cat1',
  usuarioId: null,
}
const viagem: OrcamentoResponse = {
  id: 'o2',
  nome: 'Viagem',
  descricao: '',
  valorLimite: 5000,
  dataLimite: '2026-12-31T23:59:00',
  statusOrcamento: StatusOrcamento.Encerrado,
  categoriaId: 'cat2',
  usuarioId: null,
}
const lazer: OrcamentoResponse = { ...viagem, id: 'o3', nome: 'Lazer do mês', statusOrcamento: StatusOrcamento.Ativo }

function status(id: string, valorGasto: number, percentual: number): OrcamentoStatusResponse {
  return {
    id,
    nomeOrcamento: 'x',
    descricao: null,
    categoriaId: 'cat1',
    nomeCategoria: 'Mercado (status)',
    valorLimite: 1000,
    valorGasto,
    percentual,
    dataLimite: '2026-03-31T23:59:00',
    status: StatusOrcamento.Ativo,
  }
}

function carregar(orcamentos: OrcamentoResponse[] = [alimentacao, viagem, lazer], st: OrcamentoStatusResponse[] = [status('o1', 450, 45)]) {
  mListar.mockResolvedValue(respostaOk(orcamentos))
  mStatus.mockResolvedValue(respostaOk(st))
  mCategorias.mockResolvedValue(respostaOk(categorias))
}

async function renderizarCarregado() {
  render(<Orcamentos />)
  await waitFor(() => expect(screen.queryByText('Carregando...')).not.toBeInTheDocument())
}

const semNbsp = (t: string | null) => (t ?? '').replace(/\s/g, ' ')
const cartao = (nome: string) => screen.getByText(nome).closest('.p-\\[18px\\]') as HTMLElement
const barra = (nome: string) => cartao(nome).querySelector('.h-2 > .h-2') as HTMLElement

describe('Orcamentos', () => {
  beforeEach(() => {
    for (const m of [mListar, mCriar, mAtualizar, mExcluir, mCategorias, mStatus]) m.mockReset()
  })

  describe('carregamento', () => {
    it('mostra "Carregando..." enquanto busca', () => {
      mListar.mockReturnValue(adiado<never>().promise)
      mStatus.mockReturnValue(adiado<never>().promise)
      mCategorias.mockReturnValue(adiado<never>().promise)
      render(<Orcamentos />)
      expect(screen.getByText('Carregando...')).toBeInTheDocument()
    })

    it('sem orçamentos, mostra o estado vazio com atalho para criar', async () => {
      carregar([], [])
      await renderizarCarregado()
      expect(screen.getByText('Nenhum orçamento encontrado.')).toBeInTheDocument()

      await userEvent.click(screen.getByRole('button', { name: 'Criar primeiro orçamento' }))
      expect(screen.getByRole('heading', { name: 'Novo Orçamento' })).toBeInTheDocument()
    })

    it('só encerrados, no filtro "Ativos", também mostra o estado vazio', async () => {
      carregar([viagem], [])
      await renderizarCarregado()
      expect(screen.getByText('Nenhum orçamento encontrado.')).toBeInTheDocument()
    })

    it('falha só no status ainda mostra os orçamentos, sem a barra de uso', async () => {
      mListar.mockResolvedValue(respostaOk([alimentacao]))
      mStatus.mockRejectedValue(erroHttp(500))
      mCategorias.mockResolvedValue(respostaOk(categorias))
      await renderizarCarregado()
      expect(screen.getByText('Alimentação')).toBeInTheDocument()
      expect(semNbsp(screen.getByText(/^Limite:/).textContent)).toBe('Limite: R$ 1.000,00')
    })

    it.skip('Bug: falha ao carregar mostra erro, e não "Nenhum orçamento encontrado."', async () => {
      // pages/Orcamentos.tsx:49-60 — load() ignora as rejeições do Promise.allSettled; sem estado
      // de erro, uma API fora do ar parece um ambiente sem orçamentos.
      mListar.mockRejectedValue(erroHttp(500))
      mStatus.mockResolvedValue(respostaOk([]))
      mCategorias.mockResolvedValue(respostaOk(categorias))
      await renderizarCarregado()
      expect(screen.queryByText('Nenhum orçamento encontrado.')).not.toBeInTheDocument()
      expect(screen.getByText(/erro|não foi possível/i)).toBeInTheDocument()
    })
  })

  describe('listagem', () => {
    it('mostra só os ativos por padrão e todos ao trocar o filtro', async () => {
      carregar()
      await renderizarCarregado()
      expect(screen.getByText('Alimentação')).toBeInTheDocument()
      expect(screen.getByText('Lazer do mês')).toBeInTheDocument()
      expect(screen.queryByText('Viagem')).not.toBeInTheDocument()

      await userEvent.click(screen.getByRole('button', { name: 'Todos' }))
      expect(screen.getByText('Viagem')).toBeInTheDocument()
      expect(cartao('Viagem')).toHaveTextContent('Encerrado')

      await userEvent.click(screen.getByRole('button', { name: 'Ativos' }))
      expect(screen.queryByText('Viagem')).not.toBeInTheDocument()
    })

    it('com status, mostra gasto, limite, percentual e vencimento', async () => {
      carregar()
      await renderizarCarregado()
      const c = cartao('Alimentação')

      expect(c).toHaveTextContent('Mercado (status)')
      expect(semNbsp(c.textContent)).toContain('R$ 450,00')
      expect(semNbsp(c.textContent)).toContain('de R$ 1.000,00')
      expect(c).toHaveTextContent('45,0% utilizado · vence 31/03/2026')
      expect(c).toHaveTextContent('Ativo')
      expect(barra('Alimentação').style.width).toBe('45%')
      expect(barra('Alimentação').style.backgroundColor).toBe('var(--fin-positive)')
    })

    it('sem status, mostra o limite e a categoria do cadastro', async () => {
      carregar()
      await renderizarCarregado()
      const c = cartao('Lazer do mês')
      expect(c).toHaveTextContent('Lazer')
      expect(semNbsp(c.textContent)).toContain('Limite: R$ 5.000,00')
      expect(c).toHaveTextContent('Vence em 31/12/2026')
    })

    it('categoria desconhecida aparece como "-"', async () => {
      carregar([{ ...alimentacao, categoriaId: 'apagada' }], [])
      await renderizarCarregado()
      expect(cartao('Alimentação')).toHaveTextContent('-')
    })

    it.each([
      [69.9, 'var(--fin-positive)'],
      [70, 'var(--fin-warning)'],
      [89.9, 'var(--fin-warning)'],
      [90, 'var(--fin-negative)'],
    ])('com %s%% de uso, a barra fica %s', async (percentual, cor) => {
      carregar([alimentacao], [status('o1', 1, percentual)])
      await renderizarCarregado()
      expect(barra('Alimentação').style.backgroundColor).toBe(cor)
    })

    it('uso acima de 100% é limitado a 100% na barra e no texto', async () => {
      carregar([alimentacao], [status('o1', 1500, 150)])
      await renderizarCarregado()
      expect(barra('Alimentação').style.width).toBe('100%')
      expect(cartao('Alimentação')).toHaveTextContent('100,0% utilizado')
    })
  })

  describe('novo orçamento', () => {
    it('valida o nome antes de enviar', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Novo orçamento' }))
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      expect(screen.getByText('Nome é obrigatório.')).toBeInTheDocument()
      expect(mCriar).not.toHaveBeenCalled()
    })

    it('sem categorias, pede uma categoria', async () => {
      mListar.mockResolvedValue(respostaOk([]))
      mStatus.mockResolvedValue(respostaOk([]))
      mCategorias.mockResolvedValue(respostaOk([]))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Novo orçamento' }))
      await userEvent.type(screen.getByLabelText('Nome'), 'Casa')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))
      expect(screen.getByText('Selecione uma categoria.')).toBeInTheDocument()
    })

    it('cria com os dados do formulário e recarrega', async () => {
      carregar()
      mCriar.mockResolvedValue(respostaOk(alimentacao))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Novo orçamento' }))

      expect(screen.getByLabelText('Categoria')).toHaveValue('cat1')
      expect(screen.getByLabelText('Status')).toHaveValue(String(StatusOrcamento.Ativo))

      await userEvent.type(screen.getByLabelText('Nome'), 'Farmácia')
      await userEvent.type(screen.getByLabelText('Descrição'), 'Remédios')
      await userEvent.clear(screen.getByLabelText('Valor Limite'))
      await userEvent.type(screen.getByLabelText('Valor Limite'), '300.5')
      await userEvent.selectOptions(screen.getByLabelText('Categoria'), 'cat2')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mCriar).toHaveBeenCalledTimes(1))
      const payload = mCriar.mock.calls[0][0]
      expect(payload).toMatchObject({
        nome: 'Farmácia',
        descricao: 'Remédios',
        valorLimite: 300.5,
        categoriaId: 'cat2',
        statusOrcamento: StatusOrcamento.Ativo,
      })
      expect(payload.dataLimite).toMatch(/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:00\.000Z$/)
      await waitFor(() => expect(mListar).toHaveBeenCalledTimes(2))
      expect(screen.queryByText('Nome é obrigatório.')).not.toBeInTheDocument()
    })

    it('valor limite inválido vira 0', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Novo orçamento' }))
      await userEvent.clear(screen.getByLabelText('Valor Limite'))
      expect(screen.getByLabelText('Valor Limite')).toHaveValue(0)
    })

    it('erro ao salvar mostra a mensagem e mantém o formulário aberto', async () => {
      carregar()
      mCriar.mockRejectedValue(erroHttp(400, { errorMessage: 'ignorada pela tela' }))
      await renderizarCarregado()
      await userEvent.click(screen.getByRole('button', { name: 'Novo orçamento' }))
      await userEvent.type(screen.getByLabelText('Nome'), 'Farmácia')
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      expect(await screen.findByText('Erro ao salvar orçamento.')).toBeInTheDocument()
      expect(screen.getByRole('button', { name: 'Salvar' })).toBeEnabled()
      expect(mListar).toHaveBeenCalledTimes(1)
    })
  })

  describe('edição', () => {
    it('abre preenchido e salva com atualizar', async () => {
      carregar()
      mAtualizar.mockResolvedValue(respostaOk(alimentacao))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Editar')[0])

      expect(screen.getByRole('heading', { name: 'Editar Orçamento' })).toBeInTheDocument()
      expect(screen.getByLabelText('Nome')).toHaveValue('Alimentação')
      expect(screen.getByLabelText('Descrição')).toHaveValue('Mercado e feira')
      expect(screen.getByLabelText('Valor Limite')).toHaveValue(1000)
      expect(screen.getByLabelText('Data Limite')).toHaveValue('2026-03-31T23:59')

      await userEvent.selectOptions(screen.getByLabelText('Status'), String(StatusOrcamento.Encerrado))
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mAtualizar).toHaveBeenCalled())
      expect(mAtualizar).toHaveBeenCalledWith(
        'o1',
        expect.objectContaining({ nome: 'Alimentação', statusOrcamento: StatusOrcamento.Encerrado, categoriaId: 'cat1' }),
      )
      expect(mCriar).not.toHaveBeenCalled()
    })

    it.skip('Bug: salvar sem mexer na data limite mantém o mesmo instante', async () => {
      // pages/Orcamentos.tsx:75 e :89 — a data UTC da API vai para o campo como local e volta com
      // toISOString(): cada edição adianta o vencimento em 3h (mesma causa do bug de Transacoes).
      carregar([{ ...alimentacao, dataLimite: '2026-03-31T20:00:00Z' }], [])
      mAtualizar.mockResolvedValue(respostaOk(alimentacao))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Editar')[0])
      await userEvent.click(screen.getByRole('button', { name: 'Salvar' }))

      await waitFor(() => expect(mAtualizar).toHaveBeenCalled())
      expect(mAtualizar.mock.calls[0][1].dataLimite).toBe('2026-03-31T20:00:00.000Z')
    })
  })

  describe('exclusão', () => {
    it('pede confirmação, exclui e recarrega', async () => {
      carregar()
      mExcluir.mockResolvedValue(respostaOk(null))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Excluir')[0])

      expect(screen.getByRole('heading', { name: 'Excluir orçamento' })).toBeInTheDocument()
      expect(screen.getByText('Alimentação', { selector: 'strong' })).toBeInTheDocument()

      await userEvent.click(screen.getByText('Excluir', { selector: 'button' }))
      await waitFor(() => expect(mExcluir).toHaveBeenCalledWith('o1'))
      await waitFor(() => expect(screen.queryByRole('heading', { name: 'Excluir orçamento' })).not.toBeInTheDocument())
      expect(mListar).toHaveBeenCalledTimes(2)
    })

    it('cancelar fecha sem excluir', async () => {
      carregar()
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Excluir')[0])
      const dialogo = screen.getByRole('heading', { name: 'Excluir orçamento' }).closest('.fixed') as HTMLElement
      await userEvent.click(within(dialogo).getByRole('button', { name: 'Cancelar' }))
      expect(screen.queryByRole('heading', { name: 'Excluir orçamento' })).not.toBeInTheDocument()
      expect(mExcluir).not.toHaveBeenCalled()
    })

    it.skip('Bug: falha ao excluir avisa o usuário', async () => {
      // pages/Orcamentos.tsx:104 — handleDelete sem catch: rejeição não tratada e nenhum aviso.
      carregar()
      mExcluir.mockRejectedValue(erroHttp(500, { errorMessage: 'Falha ao excluir.' }))
      await renderizarCarregado()
      await userEvent.click(screen.getAllByTitle('Excluir')[0])
      await userEvent.click(screen.getByText('Excluir', { selector: 'button' }))
      expect(await screen.findByText(/Falha ao excluir|Erro ao excluir/)).toBeInTheDocument()
    })
  })
})
