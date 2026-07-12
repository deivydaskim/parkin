import { useState } from 'react'
import { createFileRoute } from '@tanstack/react-router'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { LotForm } from '@/features/lots/components/LotForm'
import { LotTable } from '@/features/lots/components/LotTable'
import { useCreateLot, useLots } from '@/features/lots/queries'
import type { LotFormInput } from '@/features/lots/schemas'

export const Route = createFileRoute('/_authenticated/lots/')({
  component: LotsPage,
})

function LotsPage() {
  const { data, isLoading } = useLots()
  const [open, setOpen] = useState(false)
  const createLotMutation = useCreateLot()

  function handleCreate(values: LotFormInput) {
    createLotMutation.mutate(values, {
      onSuccess: () => {
        setOpen(false)
        createLotMutation.reset()
      },
    })
  }

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between">
        <h1 className="text-xl font-semibold">Parking lots</h1>

        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Dialog open={open} onOpenChange={setOpen}>
            <DialogTrigger asChild>
              <Button>New lot</Button>
            </DialogTrigger>
            <DialogContent>
              <DialogHeader>
                <DialogTitle>Create parking lot</DialogTitle>
              </DialogHeader>
              <LotForm
                onSubmit={handleCreate}
                isSubmitting={createLotMutation.isPending}
                error={createLotMutation.error}
                submitLabel="Create lot"
              />
            </DialogContent>
          </Dialog>
        </RoleGate>
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <LotTable lots={data?.items ?? []} />
      )}
    </div>
  )
}
