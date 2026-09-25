import { ChevronLeft, ChevronRight } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

type Props = {
  page: number
  totalPages: number
  totalCount: number
  perPage: number
  onPageChange: (page: number) => void
  onPerPageChange?: (perPage: number) => void
  pageSizes?: number[]
  itemLabel?: string
}

export function DataTablePagination({
  page,
  totalPages,
  totalCount,
  perPage,
  onPageChange,
  onPerPageChange,
  pageSizes = [10, 20, 50],
  itemLabel = 'items',
}: Props) {
  const lastPage = Math.max(totalPages, 1)
  const first = totalCount === 0 ? 0 : (page - 1) * perPage + 1
  const last = Math.min(page * perPage, totalCount)

  return (
    <div className="flex flex-col-reverse items-center justify-between gap-3 text-sm text-muted-foreground sm:flex-row">
      <p className="tabular-nums">
        {totalCount === 0
          ? `No ${itemLabel}`
          : `${first}–${last} of ${totalCount} ${itemLabel}`}
      </p>
      <div className="flex items-center gap-4">
        {onPerPageChange ? (
          <div className="hidden items-center gap-2 sm:flex">
            <span>Rows</span>
            <Select
              value={String(perPage)}
              onValueChange={(next) => onPerPageChange(Number(next))}
            >
              <SelectTrigger size="sm" className="w-18" aria-label="Rows per page">
                <SelectValue />
              </SelectTrigger>
              <SelectContent>
                {pageSizes.map((size) => (
                  <SelectItem key={size} value={String(size)}>
                    {size}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        ) : null}
        <span className="tabular-nums">
          Page {Math.min(page, lastPage)} of {lastPage}
        </span>
        <div className="flex gap-1">
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            disabled={page <= 1}
            onClick={() => onPageChange(page - 1)}
            aria-label="Previous page"
          >
            <ChevronLeft />
          </Button>
          <Button
            type="button"
            variant="outline"
            size="icon-sm"
            disabled={page >= lastPage}
            onClick={() => onPageChange(page + 1)}
            aria-label="Next page"
          >
            <ChevronRight />
          </Button>
        </div>
      </div>
    </div>
  )
}
