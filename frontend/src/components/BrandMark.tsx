import { cn } from '@/lib/utils'

type Props = {
  className?: string
}

export function BrandMark({ className }: Props) {
  return (
    <span
      className={cn(
        'flex size-8 shrink-0 items-center justify-center rounded-lg bg-sidebar-primary text-base font-bold text-sidebar-primary-foreground shadow-sm ring-1 ring-white/20',
        className,
      )}
      aria-hidden
    >
      P
    </span>
  )
}
