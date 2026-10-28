import { beforeEach, describe, expect, it, vi } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { useState } from 'react'
import IconPicker from './IconPicker'
import { adiado } from '../../test/helpers'

const fetchMock = vi.fn()

function respostaBusca(icons: string[] | undefined) {
  return { json: () => Promise.resolve(icons === undefined ? {} : { icons }) } as Response
}

function Controlado({ inicial = '', onChange }: { inicial?: string; onChange?: (v: string) => void }) {
  const [valor, setValor] = useState(inicial)
  return (
    <div>
      <IconPicker
        value={valor}
        onChange={(v) => {
          setValor(v)
          onChange?.(v)
        }}
      />
      <p>Fora do seletor</p>
    </div>
  )
}

const gatilho = () => screen.getAllByRole('button')[0]
const busca = () => screen.getByPlaceholderText(/Pesquisar ícone/)

describe('IconPicker', () => {
  beforeEach(() => {
    fetchMock.mockReset()
    vi.stubGlobal('fetch', fetchMock)
  })

  it('sem valor, convida a selecionar', () => {
    render(<IconPicker value="" onChange={() => {}} />)
    expect(screen.getByText('Selecionar ícone...')).toBeInTheDocument()
    expect(screen.queryByTitle('Remover ícone')).not.toBeInTheDocument()
  })

  it('com ícone do Iconify, mostra o nome e a prévia', () => {
    const { container } = render(<IconPicker value="mdi:car" onChange={() => {}} />)
    expect(screen.getByText('mdi:car')).toBeInTheDocument()
    expect(container.querySelector('[data-icon="mdi:car"]')).toBeInTheDocument()
  })

  it('com URL, mostra "URL personalizada" e a imagem', () => {
    const { container } = render(<IconPicker value="https://exemplo.com/a.png" onChange={() => {}} />)
    expect(screen.getByText('URL personalizada')).toBeInTheDocument()
    expect(container.querySelector('img')).toHaveAttribute('src', 'https://exemplo.com/a.png')
  })

  it('abre o painel com os 20 ícones padrão, sem consultar a API', async () => {
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())

    expect(busca()).toHaveFocus()
    expect(screen.getByTitle('mdi:wallet')).toBeInTheDocument()
    expect(screen.getByTitle('mdi:tools')).toBeInTheDocument()
    expect(screen.getAllByRole('button').length).toBe(21) // gatilho + 20 ícones
    expect(fetchMock).not.toHaveBeenCalled()
  })

  it('destaca o ícone selecionado na grade', async () => {
    render(<IconPicker value="mdi:car" onChange={() => {}} />)
    await userEvent.click(gatilho())
    expect(screen.getByTitle('mdi:car')).toHaveClass('ring-1')
    expect(screen.getByTitle('mdi:home')).not.toHaveClass('ring-1')
  })

  it('selecionar um ícone chama onChange e fecha o painel', async () => {
    const onChange = vi.fn()
    render(<Controlado onChange={onChange} />)
    await userEvent.click(gatilho())
    await userEvent.click(screen.getByTitle('mdi:home'))

    expect(onChange).toHaveBeenCalledWith('mdi:home')
    expect(screen.queryByPlaceholderText(/Pesquisar ícone/)).not.toBeInTheDocument()
    expect(screen.getByText('mdi:home')).toBeInTheDocument()
  })

  it('o ✕ limpa o ícone sem abrir o painel', async () => {
    const onChange = vi.fn()
    render(<Controlado inicial="mdi:car" onChange={onChange} />)
    await userEvent.click(screen.getByTitle('Remover ícone'))

    expect(onChange).toHaveBeenCalledWith('')
    expect(screen.queryByPlaceholderText(/Pesquisar ícone/)).not.toBeInTheDocument()
    expect(screen.getByText('Selecionar ícone...')).toBeInTheDocument()
  })

  it('clicar fora fecha o painel', async () => {
    render(<Controlado />)
    await userEvent.click(gatilho())
    await userEvent.click(screen.getByText('Fora do seletor'))
    expect(screen.queryByPlaceholderText(/Pesquisar ícone/)).not.toBeInTheDocument()
  })

  it('clicar no gatilho de novo fecha o painel', async () => {
    render(<Controlado />)
    await userEvent.click(gatilho())
    await userEvent.click(gatilho())
    expect(screen.queryByPlaceholderText(/Pesquisar ícone/)).not.toBeInTheDocument()
  })

  it('busca na API do Iconify com espera de digitação e mostra os resultados', async () => {
    fetchMock.mockResolvedValue(respostaBusca(['mdi:home-city', 'lucide:house']))
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'casa é')

    await waitFor(() => expect(screen.getByTitle('lucide:house')).toBeInTheDocument())
    // A espera (debounce) faz uma consulta só, com o termo completo e codificado
    expect(fetchMock).toHaveBeenCalledTimes(1)
    expect(fetchMock).toHaveBeenCalledWith('https://api.iconify.design/search?query=casa%20%C3%A9&limit=60')
    expect(screen.queryByTitle('mdi:wallet')).not.toBeInTheDocument()
  })

  it('mostra "Buscando..." enquanto a busca não volta', async () => {
    const pendente = adiado<Response>()
    fetchMock.mockReturnValue(pendente.promise)
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'carro')

    expect(await screen.findByText('Buscando...')).toBeInTheDocument()
    pendente.resolver(respostaBusca(['mdi:car']))
    expect(await screen.findByTitle('mdi:car')).toBeInTheDocument()
  })

  it('sem resultados, avisa que nada foi encontrado', async () => {
    fetchMock.mockResolvedValue(respostaBusca([]))
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'xyzxyz')
    expect(await screen.findByText('Nenhum ícone encontrado.')).toBeInTheDocument()
  })

  it('resposta sem a lista de ícones conta como nenhum resultado', async () => {
    fetchMock.mockResolvedValue(respostaBusca(undefined))
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'abc')
    expect(await screen.findByText('Nenhum ícone encontrado.')).toBeInTheDocument()
  })

  it('erro de rede mantém a lista anterior', async () => {
    fetchMock.mockRejectedValue(new TypeError('Failed to fetch'))
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'carro')

    await waitFor(() => expect(fetchMock).toHaveBeenCalled())
    await waitFor(() => expect(screen.queryByText('Buscando...')).not.toBeInTheDocument())
    expect(screen.getByTitle('mdi:wallet')).toBeInTheDocument()
  })

  it('apagar a busca volta aos ícones padrão', async () => {
    fetchMock.mockResolvedValue(respostaBusca(['mdi:car']))
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'carro')
    await screen.findByTitle('mdi:car')

    await userEvent.clear(busca())
    await waitFor(() => expect(screen.getByTitle('mdi:wallet')).toBeInTheDocument())
  })

  it('selecionar limpa o termo buscado para a próxima abertura', async () => {
    fetchMock.mockResolvedValue(respostaBusca(['mdi:car']))
    render(<Controlado />)
    await userEvent.click(gatilho())
    await userEvent.type(busca(), 'carro')
    await userEvent.click(await screen.findByTitle('mdi:car'))

    await userEvent.click(gatilho())
    expect(busca()).toHaveValue('')
  })

  it.skip('Bug: resposta atrasada de uma busca antiga não sobrescreve a busca atual', async () => {
    // components/ui/IconPicker.tsx:30-47 — sem cancelar/ignorar a requisição anterior,
    // a última resposta a chegar vence, mesmo sendo de um termo que o usuário já trocou.
    const antiga = adiado<Response>()
    fetchMock.mockReturnValueOnce(antiga.promise).mockResolvedValueOnce(respostaBusca(['mdi:food']))
    render(<IconPicker value="" onChange={() => {}} />)
    await userEvent.click(gatilho())

    await userEvent.type(busca(), 'carro')
    await waitFor(() => expect(fetchMock).toHaveBeenCalledTimes(1))
    await userEvent.clear(busca())
    await userEvent.type(busca(), 'comida')
    await screen.findByTitle('mdi:food')

    antiga.resolver(respostaBusca(['mdi:car']))
    await new Promise((r) => setTimeout(r, 50))
    expect(screen.queryByTitle('mdi:car')).not.toBeInTheDocument()
    expect(screen.getByTitle('mdi:food')).toBeInTheDocument()
  })
})
