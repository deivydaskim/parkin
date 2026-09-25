import { useState, type ReactNode } from 'react'
import { Check, ChevronsUpDown, Loader2, X } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Command,
  CommandEmpty,
  CommandGroup,
  CommandInput,
  CommandItem,
  CommandList,
} from '@/components/ui/command'
import {
  Popover,
  PopoverContent,
  PopoverTrigger,
} from '@/components/ui/popover'
import { cn } from '@/lib/utils'

export type ComboboxOption = {
  id: string
  label: string
  description?: ReactNode
}

type Props = {
  value: string | null
  selectedLabel?: string | null
  onChange: (option: ComboboxOption | null) => void
  options: ComboboxOption[]
  isLoading: boolean
  hasMore: boolean
  search: string
  onSearchChange: (search: string) => void
  placeholder: string
  searchPlaceholder: string
  emptyText: string
  clearable?: boolean
  disabled?: boolean
  id?: string
  className?: string
  'aria-invalid'?: boolean
}

export function EntityCombobox({
  value,
  selectedLabel,
  onChange,
  options,
  isLoading,
  hasMore,
  search,
  onSearchChange,
  placeholder,
  searchPlaceholder,
  emptyText,
  clearable = false,
  disabled,
  id,
  className,
  'aria-invalid': ariaInvalid,
}: Props) {
  const [open, setOpen] = useState(false)
  const [picked, setPicked] = useState<ComboboxOption | null>(null)

  const label =
    (picked && picked.id === value ? picked.label : null) ??
    selectedLabel ??
    options.find((option) => option.id === value)?.label ??
    null

  function select(option: ComboboxOption | null) {
    setPicked(option)
    onChange(option)
    setOpen(false)
    onSearchChange('')
  }

  return (
    <Popover open={open} onOpenChange={setOpen}>
      <div className={cn('relative', className)}>
        <PopoverTrigger asChild>
          <Button
            id={id}
            type="button"
            variant="outline"
            role="combobox"
            aria-expanded={open}
            aria-invalid={ariaInvalid}
            disabled={disabled}
            className={cn(
              'w-full justify-between px-3 font-normal',
              !value && 'text-muted-foreground',
              clearable && value && 'pr-14',
            )}
          >
            <span className="truncate">{value && label ? label : placeholder}</span>
            <ChevronsUpDown className="opacity-50" />
          </Button>
        </PopoverTrigger>
        {clearable && value ? (
          <button
            type="button"
            onClick={() => select(null)}
            className="absolute top-1/2 right-8 flex size-5 -translate-y-1/2 items-center justify-center rounded text-muted-foreground hover:bg-accent hover:text-foreground"
            aria-label="Clear selection"
          >
            <X className="size-3.5" />
          </button>
        ) : null}
      </div>
      <PopoverContent
        className="w-(--radix-popover-trigger-width) min-w-64 p-0"
        align="start"
      >
        <Command shouldFilter={false}>
          <CommandInput
            value={search}
            onValueChange={onSearchChange}
            placeholder={searchPlaceholder}
          />
          <CommandList>
            {isLoading ? (
              <div className="flex items-center justify-center gap-2 py-6 text-sm text-muted-foreground">
                <Loader2 className="size-4 animate-spin" />
                Searching…
              </div>
            ) : (
              <>
                <CommandEmpty>{emptyText}</CommandEmpty>
                <CommandGroup>
                  {options.map((option) => (
                    <CommandItem
                      key={option.id}
                      value={option.id}
                      onSelect={() => select(option)}
                    >
                      <Check
                        className={cn(
                          'size-4',
                          option.id === value ? 'opacity-100' : 'opacity-0',
                        )}
                      />
                      <div className="min-w-0 flex-1">
                        <p className="truncate">{option.label}</p>
                        {option.description ? (
                          <p className="truncate text-xs text-muted-foreground">
                            {option.description}
                          </p>
                        ) : null}
                      </div>
                    </CommandItem>
                  ))}
                </CommandGroup>
                {hasMore ? (
                  <p className="border-t px-3 py-2 text-xs text-muted-foreground">
                    Showing the first results — type to narrow down.
                  </p>
                ) : null}
              </>
            )}
          </CommandList>
        </Command>
      </PopoverContent>
    </Popover>
  )
}
