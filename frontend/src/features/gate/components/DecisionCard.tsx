import { CircleCheck, CircleX } from 'lucide-react'
import { PlateChip } from '@/components/PlateChip'
import { cn } from '@/lib/utils'
import { describeOutcome, directionLabels } from '../decision'
import {
  Decision,
  type AccessEventDecision,
  type ManualEventFormInput,
} from '../schemas'

type Props = {
  decision: AccessEventDecision
  input: ManualEventFormInput
  size?: 'default' | 'compact'
}

export function DecisionCard({ decision, input, size = 'default' }: Props) {
  const isAllowed = decision.decision === Decision.Allow
  const Icon = isAllowed ? CircleCheck : CircleX
  const compact = size === 'compact'

  return (
    <div
      role="status"
      aria-live="polite"
      className={cn(
        'flex items-center gap-4 rounded-xl border-2 animate-in fade-in-0 zoom-in-95',
        compact ? 'p-3' : 'p-5 sm:p-6',
        isAllowed
          ? 'border-success/50 bg-success/10'
          : 'border-destructive/50 bg-destructive/10',
      )}
    >
      <span
        className={cn(
          'flex shrink-0 items-center justify-center rounded-full',
          compact ? 'size-10' : 'size-14 sm:size-16',
          isAllowed
            ? 'bg-success text-success-foreground'
            : 'bg-destructive text-destructive-foreground',
        )}
      >
        <Icon className={compact ? 'size-5' : 'size-8'} aria-hidden />
      </span>
      <div className="min-w-0 flex-1 space-y-1">
        <p
          className={cn(
            'font-bold tracking-tight',
            compact ? 'text-base' : 'text-2xl sm:text-3xl',
            isAllowed ? 'text-success' : 'text-destructive',
          )}
        >
          {isAllowed ? 'ALLOW' : 'DENY'}
          <span className="ml-2 text-sm font-medium text-muted-foreground">
            {directionLabels[input.direction]}
          </span>
        </p>
        <p className={cn('font-medium', compact ? 'text-sm' : 'text-base')}>
          {describeOutcome(decision)}
        </p>
      </div>
      <PlateChip plate={input.plate} className="hidden sm:inline-flex" />
    </div>
  )
}
