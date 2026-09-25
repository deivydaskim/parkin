import { cn } from '@/lib/utils'

type Props = {
  plate: string
  muted?: boolean
  className?: string
}

export function PlateChip({ plate, muted = false, className }: Props) {
  return (
    <span
      className={cn(
        'inline-flex items-center overflow-hidden rounded-md border-2 border-foreground/80 bg-white font-mono text-sm font-semibold tracking-widest text-neutral-900 shadow-xs dark:border-foreground/60',
        muted && 'opacity-50',
        className,
      )}
    >
      <span className="self-stretch bg-primary px-1 text-[0.6rem] leading-6 font-bold tracking-normal text-primary-foreground">
        P
      </span>
      <span className="px-2 py-0.5 uppercase">{plate}</span>
    </span>
  )
}
