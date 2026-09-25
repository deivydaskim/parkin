import { useEffect, useState } from 'react'
import { Search, X } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { useDebounce } from '@/hooks/use-debounce'
import { cn } from '@/lib/utils'

type Props = {
  value: string
  onChange: (value: string) => void
  placeholder?: string
  className?: string
  delayMs?: number
}

export function SearchInput({
  value,
  onChange,
  placeholder = 'Search…',
  className,
  delayMs = 300,
}: Props) {
  const [draft, setDraft] = useState(value)
  const [lastExternal, setLastExternal] = useState(value)
  const debounced = useDebounce(draft, delayMs)

  if (value !== lastExternal) {
    setLastExternal(value)
    setDraft(value)
  }

  useEffect(() => {
    if (debounced === draft && debounced !== value) onChange(debounced)
  }, [debounced, draft, value, onChange])

  function clear() {
    setDraft('')
    onChange('')
  }

  return (
    <div className={cn('relative w-full sm:w-72', className)}>
      <Search
        className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground"
        aria-hidden
      />
      <Input
        type="search"
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        placeholder={placeholder}
        aria-label={placeholder}
        className="pr-9 pl-9 [&::-webkit-search-cancel-button]:hidden"
      />
      {draft ? (
        <button
          type="button"
          onClick={clear}
          className="absolute top-1/2 right-2 flex size-6 -translate-y-1/2 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-foreground"
          aria-label="Clear search"
        >
          <X className="size-3.5" />
        </button>
      ) : null}
    </div>
  )
}
