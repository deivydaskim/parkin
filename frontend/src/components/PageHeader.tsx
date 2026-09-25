import type { ReactNode } from 'react'
import { cn } from '@/lib/utils'

type Props = {
  title: ReactNode
  description?: ReactNode
  badges?: ReactNode
  actions?: ReactNode
  leading?: ReactNode
  className?: string
}

export function PageHeader({
  title,
  description,
  badges,
  actions,
  leading,
  className,
}: Props) {
  return (
    <header
      className={cn(
        'flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between',
        className,
      )}
    >
      <div className="flex min-w-0 items-start gap-4">
        {leading}
        <div className="min-w-0 space-y-1.5">
          <h1 className="text-2xl font-semibold tracking-tight text-balance">
            {title}
          </h1>
          {description ? (
            <p className="text-sm text-muted-foreground">{description}</p>
          ) : null}
          {badges ? (
            <div className="flex flex-wrap items-center gap-1.5 pt-1">
              {badges}
            </div>
          ) : null}
        </div>
      </div>
      {actions ? (
        <div className="flex shrink-0 flex-wrap items-center gap-2">
          {actions}
        </div>
      ) : null}
    </header>
  )
}
