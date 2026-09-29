import { cn } from '@/lib/utils'
import type { DisplayMessage, DisplayTone } from '@/lib/decision-messages'

type Props = {
  message: DisplayMessage | null
}

const IDLE_MESSAGE: DisplayMessage = {
  text: 'Ready',
  subtext: 'Waiting for vehicle',
  tone: 'info',
}

const TONE_CLASSES: Record<DisplayTone, string> = {
  success: 'text-success',
  danger: 'text-destructive',
  warning: 'text-warning',
  info: 'text-info',
}

export function GateDisplay({ message }: Props) {
  const shown = message ?? IDLE_MESSAGE

  return (
    <div className="flex min-h-24 flex-col justify-center gap-1 rounded-xl border-2 border-slate-700 bg-black px-4 py-3 text-center">
      <span
        className={cn(
          'font-led text-xl font-bold [text-shadow:0_0_10px_currentColor]',
          TONE_CLASSES[shown.tone],
          shown.tone === 'warning' && 'animate-blink',
        )}
      >
        {shown.text}
      </span>
      <span
        className={cn(
          'font-led min-h-4 text-xs opacity-80',
          TONE_CLASSES[shown.tone],
        )}
      >
        {shown.subtext ?? ''}
      </span>
    </div>
  )
}
