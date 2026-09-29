import { LogIn, LogOut } from 'lucide-react'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { VehiclePicker } from '@/features/vehicles/VehiclePicker'
import type { Direction } from '@/lib/schemas'
import type { LaneState } from '@/store/gate-store'
import { GateDisplay } from './GateDisplay'
import { LaneScene } from './LaneScene'
import { PlateCamera } from './PlateCamera'

type Props = {
  direction: Direction
  lane: LaneState
  onSend: (plate: string) => void
}

export function Lane({ direction, lane, onSend }: Props) {
  const Icon = direction === 'Enter' ? LogIn : LogOut

  return (
    <Card>
      <CardHeader>
        <CardTitle className="flex items-center gap-2">
          <Icon className="size-4 text-primary" />
          {direction === 'Enter' ? 'Entry lane' : 'Exit lane'}
        </CardTitle>
      </CardHeader>
      <CardContent className="flex flex-col gap-4">
        <LaneScene
          phase={lane.phase}
          plate={lane.plate}
          tone={lane.display?.tone ?? null}
        />
        <div className="grid gap-3 sm:grid-cols-2">
          <PlateCamera phase={lane.phase} plate={lane.plate} />
          <GateDisplay message={lane.display} />
        </div>
        <VehiclePicker
          direction={direction}
          disabled={lane.phase !== 'idle'}
          onSend={onSend}
        />
      </CardContent>
    </Card>
  )
}
