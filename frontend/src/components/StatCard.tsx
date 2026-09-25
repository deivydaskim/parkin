import type { ReactNode } from 'react'
import type { LucideIcon } from 'lucide-react'
import { Card } from '@/components/ui/card'
import { cn } from '@/lib/utils'

type Tone = 'default' | 'primary' | 'success' | 'warning' | 'destructive'

const iconToneClasses: Record<Tone, string> = {
  default: 'bg-muted text-muted-foreground',
  primary: 'bg-primary/10 text-primary',
  success: 'bg-success/12 text-success',
  warning: 'bg-warning/20 text-warning-foreground dark:text-warning',
  destructive: 'bg-destructive/10 text-destructive',
}

type Props = {
  label: string
  value: ReactNode
  hint?: ReactNode
  icon?: LucideIcon
  tone?: Tone
  valueClassName?: string
  className?: string
}

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  tone = 'default',
  valueClassName,
  className,
}: Props) {
  return (
    <Card className={cn('gap-0 p-5', className)}>
      <div className="flex items-start justify-between gap-3">
        <p className="text-sm font-medium text-muted-foreground">{label}</p>
        {Icon ? (
          <span
            className={cn(
              'flex size-9 shrink-0 items-center justify-center rounded-lg',
              iconToneClasses[tone],
            )}
          >
            <Icon className="size-4.5" aria-hidden />
          </span>
        ) : null}
      </div>
      <p
        className={cn(
          'mt-1 text-3xl font-semibold tracking-tight tabular-nums',
          valueClassName,
        )}
      >
        {value}
      </p>
      {hint ? (
        <p className="mt-1 text-xs text-muted-foreground">{hint}</p>
      ) : null}
    </Card>
  )
}
