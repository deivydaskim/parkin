import { useRef, useState, type KeyboardEvent } from 'react'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { compareLabels } from '../geometry'
import { bayStyleOf, type LayoutSpace } from '../schemas'

type Props = {
  title: string
  description?: string
  spaces: LayoutSpace[]
  selectedId: string | null
  onSelect: (id: string) => void
  searchable?: boolean
  emptyText: string
}

const styleText = {
  general: 'General',
  reservedAssigned: 'Reserved',
  reservedUnassigned: 'Reserved · unassigned',
  inactive: 'Inactive',
} as const

export function SpaceList({
  title,
  description,
  spaces,
  selectedId,
  onSelect,
  searchable = false,
  emptyText,
}: Props) {
  const [query, setQuery] = useState('')
  const listRef = useRef<HTMLUListElement>(null)
  const normalizedQuery = query.trim().toLowerCase()
  const visible = spaces
    .filter(
      (space) =>
        !normalizedQuery ||
        space.label.toLowerCase().includes(normalizedQuery) ||
        (space.zone ?? '').toLowerCase().includes(normalizedQuery) ||
        (space.reservation?.driverName ?? '')
          .toLowerCase()
          .includes(normalizedQuery),
    )
    .sort((a, b) => compareLabels(a.label, b.label))

  function handleKeyDown(event: KeyboardEvent<HTMLUListElement>) {
    if (event.key !== 'ArrowDown' && event.key !== 'ArrowUp') return
    event.preventDefault()
    const buttons = Array.from(
      listRef.current?.querySelectorAll('button') ?? [],
    )
    const index = buttons.findIndex(
      (button) => button === document.activeElement,
    )
    const nextIndex =
      event.key === 'ArrowDown'
        ? Math.min(buttons.length - 1, index + 1)
        : Math.max(0, index - 1)
    const next = buttons[nextIndex]
    if (next) {
      next.focus()
      next.click()
    }
  }

  return (
    <section className="space-y-2">
      <div>
        <h3 className="text-sm font-semibold">
          {title}{' '}
          <span className="text-muted-foreground">({spaces.length})</span>
        </h3>
        {description ? (
          <p className="text-xs text-muted-foreground">{description}</p>
        ) : null}
      </div>

      {searchable ? (
        <Input
          value={query}
          onChange={(event) => setQuery(event.target.value)}
          placeholder="Filter by label, zone or driver"
          aria-label={`Filter ${title.toLowerCase()}`}
          className="h-8"
        />
      ) : null}

      {visible.length === 0 ? (
        <p className="text-xs text-muted-foreground">{emptyText}</p>
      ) : (
        <ul
          ref={listRef}
          onKeyDown={handleKeyDown}
          aria-label={title}
          className="max-h-64 space-y-0.5 overflow-y-auto pr-1"
        >
          {visible.map((space) => {
            const isSelected = space.id === selectedId
            return (
              <li key={space.id}>
                <button
                  type="button"
                  aria-pressed={isSelected}
                  onClick={() => onSelect(space.id)}
                  className={cn(
                    'flex w-full items-center justify-between gap-2 rounded px-2 py-1 text-left text-sm outline-none hover:bg-accent focus-visible:ring-2 focus-visible:ring-ring',
                    isSelected && 'bg-accent font-medium',
                  )}
                >
                  <span className="tabular-nums">{space.label}</span>
                  <span className="truncate text-xs text-muted-foreground">
                    {space.reservation?.driverName ??
                      styleText[bayStyleOf(space)]}
                  </span>
                </button>
              </li>
            )
          })}
        </ul>
      )}
    </section>
  )
}
