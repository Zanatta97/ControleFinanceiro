import { useState, useEffect, useRef, useCallback } from 'react'
import { Icon } from '@iconify/react'

interface IconPickerProps {
  value: string
  onChange: (icon: string) => void
  previewColor?: string
}

// Conjunto inicial de ícones financeiros para exibir antes de qualquer busca
const DEFAULT_ICONS = [
  'mdi:wallet', 'mdi:cash', 'mdi:credit-card', 'mdi:bank', 'mdi:shopping',
  'mdi:food', 'mdi:car', 'mdi:home', 'mdi:heart', 'mdi:school',
  'mdi:airplane', 'mdi:dumbbell', 'mdi:music', 'mdi:movie', 'mdi:gift',
  'mdi:pill', 'mdi:dog', 'mdi:coffee', 'mdi:chart-line', 'mdi:tools',
]

function isIconifyName(value: string) {
  return value.includes(':') && !value.startsWith('http')
}

export default function IconPicker({ value, onChange, previewColor = '#22c55e' }: IconPickerProps) {
  const [open, setOpen] = useState(false)
  const [query, setQuery] = useState('')
  const [icons, setIcons] = useState<string[]>(DEFAULT_ICONS)
  const [loading, setLoading] = useState(false)
  const containerRef = useRef<HTMLDivElement>(null)
  const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null)

  const search = useCallback(async (q: string) => {
    if (!q.trim()) {
      setIcons(DEFAULT_ICONS)
      return
    }
    setLoading(true)
    try {
      const res = await fetch(
        `https://api.iconify.design/search?query=${encodeURIComponent(q)}&limit=60`
      )
      const json = await res.json() as { icons: string[] }
      setIcons(json.icons ?? [])
    } catch {
      // mantém lista anterior em caso de erro de rede
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    if (debounceRef.current) clearTimeout(debounceRef.current)
    debounceRef.current = setTimeout(() => search(query), 300)
    return () => { if (debounceRef.current) clearTimeout(debounceRef.current) }
  }, [query, search])

  // Fecha ao clicar fora
  useEffect(() => {
    function onClickOutside(e: MouseEvent) {
      if (containerRef.current && !containerRef.current.contains(e.target as Node)) {
        setOpen(false)
      }
    }
    if (open) document.addEventListener('mousedown', onClickOutside)
    return () => document.removeEventListener('mousedown', onClickOutside)
  }, [open])

  function handleSelect(icon: string) {
    onChange(icon)
    setOpen(false)
    setQuery('')
  }

  function handleClear(e: React.MouseEvent) {
    e.stopPropagation()
    onChange('')
  }

  return (
    <div ref={containerRef} className="relative">
      <label className="text-sm font-medium text-gray-700 mb-1 block">Ícone (opcional)</label>

      {/* Botão de preview / trigger */}
      <button
        type="button"
        onClick={() => setOpen((o) => !o)}
        className="flex items-center gap-3 w-full rounded-lg border border-gray-300 px-3 py-2 text-sm hover:border-green-400 focus:outline-none focus:ring-2 focus:ring-green-400 transition"
      >
        <span
          className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full text-white"
          style={{ backgroundColor: previewColor }}
        >
          {value && isIconifyName(value) ? (
            <Icon icon={value} width={18} height={18} color="white" />
          ) : value && value.startsWith('http') ? (
            <img src={value} alt="" className="h-4 w-4" />
          ) : (
            <Icon icon="mdi:image-search-outline" width={18} height={18} color="white" />
          )}
        </span>
        <span className="flex-1 text-left text-gray-500">
          {value ? (isIconifyName(value) ? value : 'URL personalizada') : 'Selecionar ícone...'}
        </span>
        {value && (
          <span
            onClick={handleClear}
            className="text-gray-400 hover:text-red-500 transition cursor-pointer px-1"
            title="Remover ícone"
          >
            ✕
          </span>
        )}
      </button>

      {/* Painel do picker */}
      {open && (
        <div className="absolute z-50 mt-1 w-full rounded-xl border border-gray-200 bg-white shadow-lg">
          <div className="p-3 border-b border-gray-100">
            <input
              autoFocus
              type="text"
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              placeholder="Pesquisar ícone (ex: casa, comida, carro...)"
              className="w-full rounded-lg border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-green-400"
            />
          </div>

          <div className="p-2 h-52 overflow-y-auto">
            {loading ? (
              <div className="flex h-full items-center justify-center text-sm text-gray-400">
                Buscando...
              </div>
            ) : icons.length === 0 ? (
              <div className="flex h-full items-center justify-center text-sm text-gray-400">
                Nenhum ícone encontrado.
              </div>
            ) : (
              <div className="grid grid-cols-8 gap-1">
                {icons.map((icon) => (
                  <button
                    key={icon}
                    type="button"
                    title={icon}
                    onClick={() => handleSelect(icon)}
                    className={`flex items-center justify-center rounded-lg p-2 text-gray-600 hover:bg-green-50 hover:text-green-600 transition ${value === icon ? 'bg-green-100 text-green-600 ring-1 ring-green-400' : ''}`}
                  >
                    <Icon icon={icon} width={22} height={22} />
                  </button>
                ))}
              </div>
            )}
          </div>

          <div className="border-t border-gray-100 px-3 py-2 text-xs text-gray-400 text-right">
            Ícones por <span className="font-medium">Iconify</span>
          </div>
        </div>
      )}
    </div>
  )
}
