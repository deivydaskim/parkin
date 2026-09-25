import { useState } from 'react'
import { useDebounce } from '@/hooks/use-debounce'
import { EntityCombobox, type ComboboxOption } from '@/components/EntityCombobox'
import { useDrivers } from '../queries'

const PICKER_PAGE_SIZE = 20

type Props = {
  value: string | null
  selectedLabel?: string | null
  onChange: (option: ComboboxOption | null) => void
  excludeIds?: string[]
  clearable?: boolean
  disabled?: boolean
  id?: string
  placeholder?: string
}

export function DriverCombobox({
  value,
  selectedLabel,
  onChange,
  excludeIds = [],
  clearable,
  disabled,
  id,
  placeholder = 'Select a driver',
}: Props) {
  const [search, setSearch] = useState('')
  const debouncedSearch = useDebounce(search, 250)
  const { data, isLoading } = useDrivers({
    search: debouncedSearch,
    status: 'Active',
    perPage: PICKER_PAGE_SIZE,
  })
  const options = (data?.items ?? [])
    .filter((driver) => !excludeIds.includes(driver.id))
    .map((driver) => ({
      id: driver.id,
      label: driver.name,
      description:
        driver.contact ??
        `${driver.plateCount} plate${driver.plateCount === 1 ? '' : 's'}`,
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
      searchPlaceholder="Search name, contact or plate…"
      emptyText="No matching drivers."
      clearable={clearable}
      disabled={disabled}
    />
  )
}
