import { useState } from 'react'
import { LayoutGrid, Rows3, SquareParking } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { ToggleGroup, ToggleGroupItem } from '@/components/ui/toggle-group'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { SearchInput } from '@/components/SearchInput'
import { ListSkeleton } from '@/components/Skeletons'
import { Skeleton } from '@/components/ui/skeleton'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { cn } from '@/lib/utils'
import { useSpaces } from '../queries'
import type { Space, SpaceStatus, SpaceType } from '../schemas'
import { CreateSpaceDialog } from './CreateSpaceDialog'
import { SpaceSheet } from './SpaceSheet'
import { SpaceTable } from './SpaceTable'
import { SpaceTile } from './SpaceTile'

export type SpaceFilters = {
  status: SpaceStatus | 'All'
  type: SpaceType | 'All'
  q: string
  view: 'grid' | 'table'
  page: number
}

type Props = {
  lotId: string
  filters: SpaceFilters
  onFiltersChange: (next: Partial<SpaceFilters>) => void
  canCreate: boolean
}

const GRID_PAGE_SIZE = 48
const TABLE_PAGE_SIZE = 20

export function SpacesPanel({ lotId, filters, onFiltersChange, canCreate }: Props) {
  const [selected, setSelected] = useState<Space | null>(null)
  const perPage = filters.view === 'grid' ? GRID_PAGE_SIZE : TABLE_PAGE_SIZE
  const { data, isLoading, isPlaceholderData } = useSpaces(lotId, {
    status: filters.status,
    type: filters.type === 'All' ? undefined : filters.type,
    search: filters.q,
    page: filters.page,
    perPage,
  })
  const spaces = data?.items ?? []
  const liveSelected = selected
    ? (spaces.find((space) => space.id === selected.id) ?? selected)
    : null
  const isFiltered =
    filters.q !== '' || filters.type !== 'All' || filters.status !== 'Active'

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
          <SearchInput
            value={filters.q}
            onChange={(q) => onFiltersChange({ q, page: 1 })}
            placeholder="Search labels"
            className="sm:w-56"
          />
          <ToggleGroup
            type="single"
            variant="outline"
            value={filters.type}
            onValueChange={(next) => {
              if (next) onFiltersChange({ type: next as SpaceFilters['type'], page: 1 })
            }}
            aria-label="Space type"
          >
            <ToggleGroupItem value="All" className="px-3">All</ToggleGroupItem>
            <ToggleGroupItem value="General" className="px-3">General</ToggleGroupItem>
            <ToggleGroupItem value="Reserved" className="px-3">Reserved</ToggleGroupItem>
          </ToggleGroup>
          <Select
            value={filters.status}
            onValueChange={(next) =>
              onFiltersChange({ status: next as SpaceFilters['status'], page: 1 })
            }
          >
            <SelectTrigger className="w-full sm:w-36" aria-label="Space status">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              <SelectItem value="Active">Active</SelectItem>
              <SelectItem value="Inactive">Inactive</SelectItem>
              <SelectItem value="All">All statuses</SelectItem>
            </SelectContent>
          </Select>
        </div>
        <div className="flex items-center gap-2">
          <ToggleGroup
            type="single"
            variant="outline"
            value={filters.view}
            onValueChange={(next) => {
              if (next) onFiltersChange({ view: next as SpaceFilters['view'], page: 1 })
            }}
            aria-label="View"
          >
            <ToggleGroupItem value="grid" aria-label="Grid view">
              <LayoutGrid />
            </ToggleGroupItem>
            <ToggleGroupItem value="table" aria-label="Table view">
              <Rows3 />
            </ToggleGroupItem>
          </ToggleGroup>
          {canCreate ? (
            <RoleGate roles={['Operator', 'SystemAdmin']}>
              <CreateSpaceDialog lotId={lotId} />
            </RoleGate>
          ) : null}
        </div>
      </div>

      <SpaceLegend />

      {isLoading ? (
        filters.view === 'grid' ? (
          <div className="grid grid-cols-3 gap-2 sm:grid-cols-5 lg:grid-cols-8">
            {Array.from({ length: 16 }, (_, index) => (
              <Skeleton key={index} className="aspect-[4/5] rounded-lg" />
            ))}
          </div>
        ) : (
          <ListSkeleton rows={6} />
        )
      ) : spaces.length === 0 ? (
        <EmptyState
          icon={SquareParking}
          title={isFiltered ? 'No spaces match' : 'No spaces yet'}
          hint={
            isFiltered
              ? 'Try a different search, type or status.'
              : 'Add general spaces to give the lot capacity, or reserved spaces for specific drivers.'
          }
          action={
            isFiltered ? (
              <Button
                variant="outline"
                onClick={() =>
                  onFiltersChange({ q: '', type: 'All', status: 'Active', page: 1 })
                }
              >
                Clear filters
              </Button>
            ) : canCreate ? (
              <RoleGate roles={['Operator', 'SystemAdmin']}>
                <CreateSpaceDialog lotId={lotId} />
              </RoleGate>
            ) : null
          }
        />
      ) : filters.view === 'grid' ? (
        <div
          className={cn(
            'grid grid-cols-3 gap-2 transition-opacity sm:grid-cols-5 lg:grid-cols-8',
            isPlaceholderData && 'opacity-60',
          )}
        >
          {spaces.map((space) => (
            <SpaceTile key={space.id} space={space} onSelect={setSelected} />
          ))}
        </div>
      ) : (
        <div className={cn('transition-opacity', isPlaceholderData && 'opacity-60')}>
          <SpaceTable spaces={spaces} onSelect={setSelected} />
        </div>
      )}

      {data && data.totalCount > 0 ? (
        <DataTablePagination
          page={filters.page}
          totalPages={data.totalPages}
          totalCount={data.totalCount}
          perPage={perPage}
          itemLabel="spaces"
          onPageChange={(page) => onFiltersChange({ page })}
        />
      ) : null}

      <SpaceSheet
        lotId={lotId}
        space={liveSelected}
        onOpenChange={(open) => {
          if (!open) setSelected(null)
        }}
      />
    </div>
  )
}

function SpaceLegend() {
  return (
    <ul className="flex flex-wrap items-center gap-x-4 gap-y-1 text-xs text-muted-foreground" aria-label="Legend">
      <li className="flex items-center gap-1.5">
        <span className="size-3 rounded-sm border-2 border-primary/40 bg-primary/8" />
        General
      </li>
      <li className="flex items-center gap-1.5">
        <span className="size-3 rounded-sm border-2 border-warning/60 bg-warning/15" />
        Reserved
      </li>
      <li className="flex items-center gap-1.5">
        <span className="size-3 rounded-sm border-2 border-dashed border-muted-foreground/40 bg-muted/50" />
        Inactive
      </li>
    </ul>
  )
}
