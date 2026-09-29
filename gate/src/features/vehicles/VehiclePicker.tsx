import { useState, type FormEvent } from 'react'
import { ArrowRight, Dices, TriangleAlert } from 'lucide-react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { env } from '@/config/env'
import { generateVisitorPlate, normalizePlate } from '@/lib/plates'
import type { Direction } from '@/lib/schemas'
import { isInside, useGateStore } from '@/store/gate-store'

type Props = {
  direction: Direction
  disabled: boolean
  onSend: (plate: string) => void
}

export function VehiclePicker({ direction, disabled, onSend }: Props) {
  const [plate, setPlate] = useState('')
  const vehiclesInside = useGateStore((state) => state.vehiclesInside)

  const normalized = normalizePlate(plate)
  const alreadyInside =
    direction === 'Enter' &&
    normalized !== '' &&
    isInside(vehiclesInside, normalized)
  const insidePlates = [
    ...new Set(vehiclesInside.map((vehicle) => vehicle.plate)),
  ]

  function submit(event: FormEvent) {
    event.preventDefault()
    if (normalized) onSend(normalized)
  }

  return (
    <div className="flex flex-col gap-3">
      <form onSubmit={submit} className="flex gap-2">
        <Input
          value={plate}
          onChange={(event) => setPlate(event.target.value)}
          placeholder="Plate, e.g. ABC123"
          maxLength={20}
          className="font-mono uppercase"
          aria-label={`${direction} lane plate`}
        />
        <Button
          type="button"
          variant="outline"
          onClick={() => setPlate(generateVisitorPlate())}
        >
          <Dices />
          {direction === 'Enter' ? 'Random visitor' : 'Unknown plate'}
        </Button>
        <Button type="submit" disabled={disabled || !normalized}>
          {direction === 'Enter' ? 'Drive in' : 'Drive out'}
          <ArrowRight />
        </Button>
      </form>

      {alreadyInside && (
        <p className="flex items-start gap-2 rounded-md border border-warning/40 bg-warning/10 px-3 py-2 text-xs text-warning">
          <TriangleAlert className="mt-0.5 size-3.5 shrink-0" />
          {normalized} is already inside. Parkin has no already-inside check, so
          a second session will open.
        </p>
      )}

      {direction === 'Enter' && env.demoPlates.length > 0 && (
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="text-xs text-muted-foreground">
            Reserved drivers
          </span>
          {env.demoPlates.map((demo) => (
            <Button
              key={demo.plate}
              type="button"
              size="sm"
              variant="secondary"
              onClick={() => setPlate(demo.plate)}
              title={demo.driverName}
            >
              <span className="font-mono">{demo.plate}</span>
              <span className="text-muted-foreground">
                {demo.driverName.split(' ')[0]}
              </span>
            </Button>
          ))}
        </div>
      )}

      {direction === 'Exit' && (
        <div className="flex flex-wrap items-center gap-1.5">
          <span className="text-xs text-muted-foreground">
            Inside ({vehiclesInside.length})
          </span>
          {insidePlates.length === 0 && (
            <span className="text-xs text-muted-foreground">
              No vehicles inside yet
            </span>
          )}
          {insidePlates.map((insidePlate) => {
            const vehicle = vehiclesInside.find(
              (item) => item.plate === insidePlate,
            )
            return (
              <Button
                key={insidePlate}
                type="button"
                size="sm"
                variant="secondary"
                onClick={() => setPlate(insidePlate)}
              >
                <span className="font-mono">{insidePlate}</span>
                {vehicle?.pool === 'Reserved' && (
                  <Badge variant="outline">
                    {vehicle.reservedSpaceLabel ?? 'Reserved'}
                  </Badge>
                )}
              </Button>
            )
          })}
        </div>
      )}
    </div>
  )
}
