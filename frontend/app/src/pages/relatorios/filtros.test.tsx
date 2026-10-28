import { beforeEach, describe, expect, it, vi } from 'vitest'
import { fireEvent, render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import {
  BarraFiltros,
  CategoriasFixosRecebimentos,
  FiltroPeriodo,
  RotuloFiltro,
  SeletorAno,
  SeletorCategorias,
  SeletorCompetencia,
} from './filtros'
import type { FiltroPeriodoValor } from './utils'
import type { CategoriaResponse } from '../../types/api'
import { listarDoAmbiente } from '../../api/categoria'
import { adiado, erroHttp, respostaOk } from '../../test/helpers'

vi.mock('../../api/categoria', () => ({ listarDoAmbiente: vi.fn() }))

const categoria = (id: string, nome: string, cor: string | null = '#f00'): CategoriaResponse => ({
  id,
  nome,
  cor,
  urlIcone: null,
  usuarioId: null,
})

const categorias = [categoria('c1', 'Aluguel'), categoria('c2', 'Salário'), categoria('c3', 'Mercado', null)]

describe('BarraFiltros e RotuloFiltro', () => {
  it('renderizam o conteúdo', () => {
    render(<BarraFiltros><RotuloFiltro>Período</RotuloFiltro></BarraFiltros>)
    expect(screen.getByText('Período')).toBeInTheDocument()
  })
})

describe('SeletorCompetencia', () => {
  it('mostra o mês no campo com o rótulo padrão', () => {
    render(<SeletorCompetencia mes={9} ano={2026} onChange={() => {}} />)
    expect(screen.getByLabelText('Competência')).toHaveValue('2026-09')
  })

  it('aceita rótulo personalizado', () => {
    render(<SeletorCompetencia mes={1} ano={2026} onChange={() => {}} rotulo="Mês da fatura" />)
    expect(screen.getByLabelText('Mês da fatura')).toHaveValue('2026-01')
  })

  it('setas andam um mês, virando o ano', async () => {
    const onChange = vi.fn()
    const { rerender } = render(<SeletorCompetencia mes={12} ano={2026} onChange={onChange} />)
    await userEvent.click(screen.getByRole('button', { name: 'Próximo mês' }))
    expect(onChange).toHaveBeenLastCalledWith(1, 2027)

    rerender(<SeletorCompetencia mes={1} ano={2026} onChange={onChange} />)
    await userEvent.click(screen.getByRole('button', { name: 'Mês anterior' }))
    expect(onChange).toHaveBeenLastCalledWith(12, 2025)
  })

  it('digitar no campo informa mês e ano', () => {
    const onChange = vi.fn()
    render(<SeletorCompetencia mes={9} ano={2026} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Competência'), { target: { value: '2025-03' } })
    expect(onChange).toHaveBeenCalledWith(3, 2025)
  })

  it('campo apagado é ignorado', () => {
    const onChange = vi.fn()
    render(<SeletorCompetencia mes={9} ano={2026} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Competência'), { target: { value: '' } })
    expect(onChange).not.toHaveBeenCalled()
  })
})

describe('SeletorAno', () => {
  it('mostra o ano e as setas somam/subtraem um', async () => {
    const onChange = vi.fn()
    render(<SeletorAno ano={2026} onChange={onChange} />)
    expect(screen.getByText('2026')).toBeInTheDocument()

    await userEvent.click(screen.getByRole('button', { name: 'Ano anterior' }))
    await userEvent.click(screen.getByRole('button', { name: 'Próximo ano' }))
    expect(onChange.mock.calls).toEqual([[2025], [2027]])
  })
})

describe('FiltroPeriodo', () => {
  const competencia: FiltroPeriodoValor = {
    modo: 'competencia',
    mes: 9,
    ano: 2026,
    dataInicio: '2026-09-01',
    dataFim: '2026-09-30',
  }
  const periodo: FiltroPeriodoValor = { ...competencia, modo: 'periodo' }

  it('no modo competência, marca o rádio certo e mostra só o seletor de mês', () => {
    render(<FiltroPeriodo valor={competencia} onChange={() => {}} />)
    expect(screen.getByRole('radio', { name: 'Competência' })).toHaveAttribute('aria-checked', 'true')
    expect(screen.getByRole('radio', { name: 'Período' })).toHaveAttribute('aria-checked', 'false')
    expect(screen.getByLabelText('Competência')).toBeInTheDocument()
    expect(screen.queryByLabelText('Data inicial')).not.toBeInTheDocument()
  })

  it('trocar de modo preserva os valores do outro modo', async () => {
    const onChange = vi.fn()
    render(<FiltroPeriodo valor={competencia} onChange={onChange} />)
    await userEvent.click(screen.getByRole('radio', { name: 'Período' }))
    expect(onChange).toHaveBeenCalledWith(periodo)
  })

  it('mudar a competência mantém o modo e as datas', async () => {
    const onChange = vi.fn()
    render(<FiltroPeriodo valor={competencia} onChange={onChange} />)
    await userEvent.click(screen.getByRole('button', { name: 'Próximo mês' }))
    expect(onChange).toHaveBeenCalledWith({ ...competencia, mes: 10 })
  })

  it('no modo período, mostra as datas e o aviso sobre parcelados', () => {
    render(<FiltroPeriodo valor={periodo} onChange={() => {}} />)
    expect(screen.getByLabelText('Data inicial')).toHaveValue('2026-09-01')
    expect(screen.getByLabelText('Data final')).toHaveValue('2026-09-30')
    expect(screen.queryByLabelText('Competência')).not.toBeInTheDocument()
    expect(screen.getByText(/uma compra parcelada conta inteira/)).toBeInTheDocument()
  })

  it('editar as datas informa o novo valor', () => {
    const onChange = vi.fn()
    render(<FiltroPeriodo valor={periodo} onChange={onChange} />)
    fireEvent.change(screen.getByLabelText('Data inicial'), { target: { value: '2026-08-15' } })
    expect(onChange).toHaveBeenLastCalledWith({ ...periodo, dataInicio: '2026-08-15' })
    fireEvent.change(screen.getByLabelText('Data final'), { target: { value: '2026-10-15' } })
    expect(onChange).toHaveBeenLastCalledWith({ ...periodo, dataFim: '2026-10-15' })
  })

  it('período invertido mostra o erro no lugar do aviso', () => {
    render(<FiltroPeriodo valor={{ ...periodo, dataInicio: '2026-10-01' }} onChange={() => {}} />)
    expect(screen.getByText('A data inicial deve ser anterior ou igual à final.')).toBeInTheDocument()
    expect(screen.queryByText(/uma compra parcelada conta inteira/)).not.toBeInTheDocument()
  })

  it('período incompleto pede as duas datas', () => {
    render(<FiltroPeriodo valor={{ ...periodo, dataFim: '' }} onChange={() => {}} />)
    expect(screen.getByText('Informe a data inicial e a final.')).toBeInTheDocument()
  })
})

describe('SeletorCategorias', () => {
  it('lista vazia avisa que não há categorias', () => {
    render(<SeletorCategorias titulo="Fixas" categorias={[]} selecionadas={[]} onChange={() => {}} />)
    expect(screen.getByText('Nenhuma categoria cadastrada neste ambiente.')).toBeInTheDocument()
  })

  it('mostra título, descrição e marca as selecionadas', () => {
    render(
      <SeletorCategorias titulo="Fixas" descricao="Aluguel, contas..." categorias={categorias} selecionadas={['c1']} onChange={() => {}} />,
    )
    expect(screen.getByText('Aluguel, contas...')).toBeInTheDocument()
    expect(screen.getByText('(1)')).toBeInTheDocument()
    expect(screen.getByRole('button', { name: 'Aluguel' })).toHaveAttribute('aria-pressed', 'true')
    expect(screen.getByRole('button', { name: 'Salário' })).toHaveAttribute('aria-pressed', 'false')
  })

  it('clicar alterna a categoria', async () => {
    const onChange = vi.fn()
    render(<SeletorCategorias titulo="Fixas" categorias={categorias} selecionadas={['c1']} onChange={onChange} />)
    await userEvent.click(screen.getByRole('button', { name: 'Salário' }))
    expect(onChange).toHaveBeenLastCalledWith(['c1', 'c2'])
    await userEvent.click(screen.getByRole('button', { name: 'Aluguel' }))
    expect(onChange).toHaveBeenLastCalledWith([])
  })

  it('"Limpar" só aparece com seleção e desmarca tudo', async () => {
    const onChange = vi.fn()
    const { rerender } = render(<SeletorCategorias titulo="Fixas" categorias={categorias} selecionadas={[]} onChange={onChange} />)
    expect(screen.queryByRole('button', { name: 'Limpar' })).not.toBeInTheDocument()

    rerender(<SeletorCategorias titulo="Fixas" categorias={categorias} selecionadas={['c1', 'c3']} onChange={onChange} />)
    await userEvent.click(screen.getByRole('button', { name: 'Limpar' }))
    expect(onChange).toHaveBeenCalledWith([])
  })

  it('categoria bloqueada fica desabilitada com o motivo', async () => {
    const onChange = vi.fn()
    render(
      <SeletorCategorias
        titulo="Fixas"
        categorias={categorias}
        selecionadas={[]}
        bloqueadas={['c2']}
        motivoBloqueio="Já é recebimento"
        onChange={onChange}
      />,
    )
    const salario = screen.getByRole('button', { name: 'Salário' })
    expect(salario).toBeDisabled()
    expect(salario).toHaveAttribute('title', 'Já é recebimento')
    await userEvent.click(salario)
    expect(onChange).not.toHaveBeenCalled()
  })

  it('motivo de bloqueio padrão', () => {
    render(<SeletorCategorias titulo="Fixas" categorias={categorias} selecionadas={[]} bloqueadas={['c2']} onChange={() => {}} />)
    expect(screen.getByRole('button', { name: 'Salário' })).toHaveAttribute('title', 'Já selecionada na outra lista')
  })

  it('categoria selecionada nunca fica bloqueada', () => {
    render(<SeletorCategorias titulo="Fixas" categorias={categorias} selecionadas={['c2']} bloqueadas={['c2']} onChange={() => {}} />)
    expect(screen.getByRole('button', { name: 'Salário' })).toBeEnabled()
  })
})

describe('CategoriasFixosRecebimentos', () => {
  const listar = vi.mocked(listarDoAmbiente)

  beforeEach(() => {
    listar.mockReset()
  })

  it('mostra carregando enquanto busca as categorias', () => {
    listar.mockReturnValue(adiado<never>().promise)
    render(<CategoriasFixosRecebimentos selecao={{ fixas: [], recebimento: [] }} onSelecao={() => {}} />)
    expect(screen.getByText('Carregando categorias...')).toBeInTheDocument()
  })

  it('mostra o erro quando a busca falha', async () => {
    listar.mockRejectedValue(erroHttp(500, { errorMessage: 'Falha no servidor.' }))
    render(<CategoriasFixosRecebimentos selecao={{ fixas: [], recebimento: [] }} onSelecao={() => {}} />)
    expect(await screen.findByText('Falha no servidor.')).toBeInTheDocument()
  })

  it('sem categorias, as duas listas avisam', async () => {
    listar.mockResolvedValue(respostaOk([]))
    render(<CategoriasFixosRecebimentos selecao={{ fixas: [], recebimento: [] }} onSelecao={() => {}} />)
    expect(await screen.findAllByText('Nenhuma categoria cadastrada neste ambiente.')).toHaveLength(2)
  })

  it('mostra as duas listas e marca como opcional por padrão', async () => {
    listar.mockResolvedValue(respostaOk(categorias))
    render(<CategoriasFixosRecebimentos selecao={{ fixas: [], recebimento: [] }} onSelecao={() => {}} />)
    expect(await screen.findByText('Categorias de gasto fixo')).toBeInTheDocument()
    expect(screen.getByText('Categorias de recebimento')).toBeInTheDocument()
    expect(screen.getAllByText(/Opcional\./)).toHaveLength(2)
    expect(screen.getAllByRole('button', { name: 'Aluguel' })).toHaveLength(2)
  })

  it('obrigatório pede ao menos uma', async () => {
    listar.mockResolvedValue(respostaOk(categorias))
    render(<CategoriasFixosRecebimentos selecao={{ fixas: [], recebimento: [] }} onSelecao={() => {}} obrigatorio />)
    expect(await screen.findAllByText(/Selecione ao menos uma\./)).toHaveLength(2)
  })

  it('a mesma categoria não pode estar nas duas listas', async () => {
    listar.mockResolvedValue(respostaOk(categorias))
    render(<CategoriasFixosRecebimentos selecao={{ fixas: ['c1'], recebimento: ['c2'] }} onSelecao={() => {}} />)
    const [aluguelFixo, aluguelRecebimento] = await screen.findAllByRole('button', { name: 'Aluguel' })
    const [salarioFixo, salarioRecebimento] = screen.getAllByRole('button', { name: 'Salário' })

    expect(aluguelFixo).toHaveAttribute('aria-pressed', 'true')
    expect(aluguelRecebimento).toBeDisabled()
    expect(aluguelRecebimento).toHaveAttribute('title', 'Já selecionada como gasto fixo')
    expect(salarioFixo).toBeDisabled()
    expect(salarioFixo).toHaveAttribute('title', 'Já selecionada como recebimento')
    expect(salarioRecebimento).toHaveAttribute('aria-pressed', 'true')
  })

  it('selecionar em cada lista atualiza só aquela lista', async () => {
    listar.mockResolvedValue(respostaOk(categorias))
    const onSelecao = vi.fn()
    render(<CategoriasFixosRecebimentos selecao={{ fixas: ['c1'], recebimento: [] }} onSelecao={onSelecao} />)
    const [mercadoFixo, mercadoRecebimento] = await screen.findAllByRole('button', { name: 'Mercado' })

    await userEvent.click(mercadoFixo)
    expect(onSelecao).toHaveBeenLastCalledWith({ fixas: ['c1', 'c3'], recebimento: [] })

    await userEvent.click(mercadoRecebimento)
    expect(onSelecao).toHaveBeenLastCalledWith({ fixas: ['c1'], recebimento: ['c3'] })
  })
})
