import { useState } from 'react'
import { useDebounce } from '@/hooks/use-debounce'
import { EntityCombobox, type ComboboxOption } from '@/components/EntityCombobox'
import { useLots } from '../queries'

const PICKER_PAGE_SIZE = 20

type Props = {
  value: string | null
  selectedLabel?: string | null
  onChange: (option: ComboboxOption | null) => void
  disabled?: boolean
  id?: string
  placeholder?: string
  className?: string
  'aria-invalid'?: boolean
}

export function LotCombobox({
  value,
  selectedLabel,
  onChange,
  disabled,
  id,
  placeholder = 'Select a lot',
  className,
  'aria-invalid': ariaInvalid,
}: Props) {
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounce(search, 250)
  const { data, isLoading } = useLots({
    search: debouncedSearch,
    status: 'Active',
    perPage: PICKER_PAGE_SIZE,
  })
  const options = (data?.items ?? []).map((lot) => ({
    id: lot.id,
    label: lot.name,
    description: lot.address ?? `${lot.capacity} general spaces`,
  }))

  return (
    <EntityCombobox
      id={id}
      value={value}
      selectedLabel={selectedLabel}
      onChange={onChange}
      options={options}
      isLoading={isLoading}
      hasMore={(data?.totalCount ?? 0) > PICKER_PAGE_SIZE}
      search={search}
      onSearchChange={setSearch}
      placeholder={placeholder}
      searchPlaceholder="Search lots…"
      emptyText="No matching lots."
      disabled={disabled}
      className={className}
      aria-invalid={ariaInvalid}
    />
  )
}
