import { createFileRoute, Link, useNavigate } from '@tanstack/react-router'
import { DoorOpen, ParkingSquare } from 'lucide-react'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Label } from '@/components/ui/label'
import { EmptyState } from '@/components/EmptyState'
import { PageHeader } from '@/components/PageHeader'
import { ManualEntryForm } from '@/features/gate/components/ManualEntryForm'
import { RecentActivity } from '@/features/gate/components/RecentActivity'
import { LotCombobox } from '@/features/lots/components/LotCombobox'
import { useLot } from '@/features/lots/queries'
import { LotStatus } from '@/features/lots/schemas'
import { OccupancyStats } from '@/features/occupancy/components/OccupancyStats'

const LAST_LOT_STORAGE_KEY = 'parkin.gate.lotId'

function readLastLot() {
  try {
    return window.localStorage.getItem(LAST_LOT_STORAGE_KEY) ?? undefined
  } catch {
    return undefined
  }
}

function rememberLot(lotId: string) {
  try {
    window.localStorage.setItem(LAST_LOT_STORAGE_KEY, lotId)
  } catch {
    return
  }
}

const gateSearchSchema = z.object({
  lot: z.uuid().optional().catch(undefined),
})

export const Route = createFileRoute('/_authenticated/gate')({
  validateSearch: gateSearchSchema,
  staticData: { crumb: 'Gate console' },
  component: GatePage,
})

function GatePage() {
  const { lot: lotFromUrl } = Route.useSearch()
  const navigate = useNavigate({ from: Route.fullPath })
  const lotId = lotFromUrl ?? readLastLot() ?? null
  const { data: lot, isError } = useLot(lotId ?? '')

  function selectLot(nextLotId: string | null) {
    if (nextLotId) rememberLot(nextLotId)
    navigate({ search: { lot: nextLotId ?? undefined }, replace: true })
  }

  const isArchived = lot?.status === LotStatus.Archived

  return (
    <div className="space-y-6">
      <PageHeader
        title="Gate console"
        description="Record entries and exits by hand. Runs the same checks as the plate reader."
      />

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_22rem]">
        <Card className="gap-6">
          <CardHeader className="gap-4">
            <div className="space-y-2">
              <Label htmlFor="gate-lot">Lot</Label>
              <LotCombobox
                id="gate-lot"
                value={lotId}
                selectedLabel={lot?.name}
                onChange={(option) => selectLot(option?.id ?? null)}
                placeholder="Choose the lot you are working at"
                className="sm:max-w-md"
              />
            </div>
          </CardHeader>
          <CardContent>
            {!lotId || isError ? (
              <EmptyState
                icon={ParkingSquare}
                title="Pick a lot to start"
                hint="Your choice is remembered on this device for next time."
              />
            ) : isArchived ? (
              <EmptyState
                icon={DoorOpen}
                title="This lot is archived"
                hint="Archived lots deny every entry. Restore it to record events."
                action={
                  <Button variant="outline" asChild>
                    <Link to="/lots/$lotId" params={{ lotId }}>
                      Open lot
                    </Link>
                  </Button>
                }
              />
            ) : (
              <ManualEntryForm key={lotId} lotId={lotId} size="large" />
            )}
          </CardContent>
        </Card>

        {lotId && lot ? (
          <div className="space-y-6">
            <Card className="gap-4">
              <CardHeader>
                <CardTitle className="text-base">
                  <Link
                    to="/lots/$lotId"
                    params={{ lotId }}
                    className="hover:underline"
                  >
                    {lot.name}
                  </Link>
                </CardTitle>
              </CardHeader>
              <CardContent>
                <OccupancyStats lotId={lotId} compact />
              </CardContent>
            </Card>
            <RecentActivity lotId={lotId} limit={8} />
          </div>
        ) : null}
      </div>
    </div>
  )
}
