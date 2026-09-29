import { cn } from '@/lib/utils'
import type { DisplayTone } from '@/lib/decision-messages'
import type { LanePhase } from '@/store/gate-store'

type Props = {
  phase: LanePhase
  plate: string | null
  tone: DisplayTone | null
}

type Light = 'off' | 'red' | 'green' | 'amber'

const BARRIER_OPEN_PHASES: LanePhase[] = ['opening', 'passing']

function carOffset(phase: LanePhase) {
  switch (phase) {
    case 'approaching':
    case 'scanning':
    case 'deciding':
    case 'opening':
    case 'denied':
      return 0
    case 'passing':
    case 'closing':
      return 460
    default:
      return -280
  }
}

function carTransition(phase: LanePhase) {
  switch (phase) {
    case 'approaching':
      return 'transform 1000ms cubic-bezier(0.2, 0.7, 0.3, 1)'
    case 'passing':
      return 'transform 1400ms cubic-bezier(0.55, 0, 0.9, 0.6)'
    case 'reversing':
      return 'transform 1200ms ease-in'
    default:
      return 'none'
  }
}

function resolveLight(phase: LanePhase, tone: DisplayTone | null): Light {
  if (phase === 'scanning' || phase === 'deciding') return 'amber'
  if (BARRIER_OPEN_PHASES.includes(phase) || phase === 'closing') {
    return tone === 'warning' ? 'amber' : 'green'
  }
  if (phase === 'denied' || phase === 'reversing') return 'red'
  return 'off'
}

export function LaneScene({ phase, plate, tone }: Props) {
  const light = resolveLight(phase, tone)
  const barrierOpen = BARRIER_OPEN_PHASES.includes(phase)
  const carVisible = phase !== 'idle'

  return (
    <div className="bg-parking-grid relative overflow-hidden rounded-xl border bg-black/30">
      <svg viewBox="0 0 400 210" className="block w-full" role="img">
        <title>Gate lane</title>

        <rect
          x="0"
          y="150"
          width="400"
          height="60"
          className="fill-slate-800"
        />
        <rect x="0" y="146" width="400" height="4" className="fill-slate-600" />
        <line
          x1="0"
          y1="182"
          x2="400"
          y2="182"
          className="stroke-slate-500"
          strokeWidth="3"
          strokeDasharray="22 16"
        />
        <rect
          x="238"
          y="150"
          width="4"
          height="60"
          className="fill-slate-100/70"
        />

        <g>
          <rect
            x="72"
            y="44"
            width="6"
            height="106"
            className="fill-slate-500"
          />
          <rect
            x="60"
            y="36"
            width="34"
            height="16"
            rx="3"
            className="fill-slate-700"
          />
          <circle cx="88" cy="44" r="5" className="fill-slate-950" />
          <circle cx="88" cy="44" r="2.5" className="fill-info" />
          {phase === 'scanning' && (
            <path
              d="M88 48 L150 150 L60 150 Z"
              className="fill-info/15 animate-pulse"
            />
          )}
        </g>

        <g>
          <rect
            x="296"
            y="62"
            width="14"
            height="88"
            rx="2"
            className="fill-slate-600"
          />
          <rect
            x="286"
            y="132"
            width="34"
            height="18"
            rx="3"
            className="fill-slate-700"
          />

          <rect
            x="300"
            y="18"
            width="24"
            height="48"
            rx="5"
            className="fill-slate-900 stroke-slate-600"
          />
          <circle
            cx="312"
            cy="31"
            r="7"
            className={cn(
              'transition-colors',
              light === 'red' ? 'fill-destructive' : 'fill-destructive/20',
            )}
          />
          <circle
            cx="312"
            cy="53"
            r="7"
            className={cn(
              'transition-colors',
              light === 'green' && 'fill-success',
              light === 'amber' && 'animate-blink fill-warning',
              (light === 'off' || light === 'red') && 'fill-success/20',
            )}
          />

          <g
            style={{
              transformOrigin: '303px 118px',
              transform: `rotate(${barrierOpen ? 78 : 0}deg)`,
              transition: 'transform 650ms cubic-bezier(0.4, 0, 0.2, 1)',
            }}
          >
            <g transform="translate(303 118) scale(-1 1)">
              <rect
                x="0"
                y="-5"
                width="150"
                height="10"
                rx="3"
                className="fill-slate-100"
              />
              <rect
                x="20"
                y="-5"
                width="18"
                height="10"
                className="fill-destructive"
              />
              <rect
                x="56"
                y="-5"
                width="18"
                height="10"
                className="fill-destructive"
              />
              <rect
                x="92"
                y="-5"
                width="18"
                height="10"
                className="fill-destructive"
              />
              <rect
                x="128"
                y="-5"
                width="18"
                height="10"
                className="fill-destructive"
              />
            </g>
          </g>
          <circle cx="303" cy="118" r="7" className="fill-slate-400" />
        </g>

        <g
          style={{
            transform: `translateX(${carOffset(phase)}px)`,
            transition: carTransition(phase),
            opacity: carVisible ? 1 : 0,
          }}
        >
          <rect
            x="30"
            y="138"
            width="126"
            height="34"
            rx="9"
            className="fill-primary"
          />
          <path d="M58 138 L74 112 H118 L136 138 Z" className="fill-primary" />
          <path d="M66 138 L79 117 H96 V138 Z" className="fill-slate-900/80" />
          <path
            d="M101 117 H115 L129 138 H101 Z"
            className="fill-slate-900/80"
          />
          <rect
            x="148"
            y="146"
            width="10"
            height="7"
            rx="2"
            className="fill-warning"
          />
          <rect
            x="28"
            y="146"
            width="6"
            height="7"
            rx="2"
            className="fill-destructive"
          />
          <circle cx="60" cy="172" r="12" className="fill-slate-950" />
          <circle cx="60" cy="172" r="5" className="fill-slate-400" />
          <circle cx="126" cy="172" r="12" className="fill-slate-950" />
          <circle cx="126" cy="172" r="5" className="fill-slate-400" />
          <rect
            x="56"
            y="186"
            width="74"
            height="16"
            rx="3"
            className="fill-slate-100"
          />
          <text
            x="93"
            y="198"
            textAnchor="middle"
            className="fill-slate-900 font-mono text-[11px] font-bold"
          >
            {plate ?? ''}
          </text>
        </g>
      </svg>
    </div>
  )
}
