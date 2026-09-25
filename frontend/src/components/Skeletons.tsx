import { Card } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

type ListSkeletonProps = {
  rows?: number
  className?: string
}

export function ListSkeleton({ rows = 5, className }: ListSkeletonProps) {
  return (
    <div
      className={cn('divide-y rounded-xl border bg-card', className)}
      aria-busy
      aria-label="Loading"
    >
      {Array.from({ length: rows }, (_, index) => (
        <div key={index} className="flex items-center gap-4 p-4">
          <Skeleton className="size-9 rounded-full" />
          <div className="flex-1 space-y-2">
            <Skeleton className="h-3.5 w-1/3" />
            <Skeleton className="h-3 w-1/5" />
          </div>
          <Skeleton className="h-6 w-16 rounded-full" />
        </div>
      ))}
    </div>
  )
}

type CardSkeletonProps = {
  count?: number
  className?: string
}

export function CardSkeleton({ count = 3, className }: CardSkeletonProps) {
  return (
    <div
      className={cn('grid gap-4 sm:grid-cols-2 xl:grid-cols-3', className)}
      aria-busy
      aria-label="Loading"
    >
      {Array.from({ length: count }, (_, index) => (
        <Card key={index} className="gap-3 p-5">
          <Skeleton className="h-4 w-1/2" />
          <Skeleton className="h-3 w-2/3" />
          <Skeleton className="mt-3 h-2.5 w-full rounded-full" />
        </Card>
      ))}
    </div>
  )
}
