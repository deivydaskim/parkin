import { Camera } from 'lucide-react'
import { cn } from '@/lib/utils'
import type { LanePhase } from '@/store/gate-store'

type Props = {
  phase: LanePhase
  plate: string | null
}

const PLACEHOLDER = '· · · · · ·'

function readout(phase: LanePhase, plate: string | null) {
  if (!plate || phase === 'idle' || phase === 'approaching') return PLACEHOLDER
  return plate
}

export function PlateCamera({ phase, plate }: Props) {
  const scanning = phase === 'scanning'
  const locked =
    plate !== null && !['idle', 'approaching', 'scanning'].includes(phase)

  return (
    <div className="flex flex-col gap-2 rounded-xl border bg-card p-3">
      <div className="flex items-center justify-between text-xs text-muted-foreground">
        <span className="flex items-center gap-1.5 font-medium">
          <Camera className="size-3.5" />
          LPR camera
        </span>
        <span className="flex items-center gap-1.5">
          <span
            className={cn(
              'size-2 rounded-full',
              scanning
                ? 'animate-blink bg-destructive'
                : 'bg-muted-foreground/40',
            )}
          />
          {scanning ? 'Scanning' : locked ? 'Plate locked' : 'Standby'}
        </span>
      </div>

      <div className="relative flex h-14 items-center overflow-hidden rounded-md border-2 border-slate-300 bg-slate-100">
        <div className="flex h-full w-8 shrink-0 flex-col items-center justify-center bg-blue-700 text-[9px] font-bold text-white">
          <span>LT</span>
        </div>
        <span
          className={cn(
            'flex-1 text-center font-mono text-2xl font-bold tracking-widest text-slate-900 transition-opacity',
            scanning && 'opacity-60',
          )}
        >
          {readout(phase, plate)}
        </span>
        {scanning && (
          <span className="pointer-events-none absolute inset-x-0 h-0.5 animate-scan bg-destructive shadow-[0_0_8px_var(--destructive)]" />
        )}
      </div>
    </div>
  )
}
