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
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { LotForm } from '@/features/lots/components/LotForm'
import { LotTable } from '@/features/lots/components/LotTable'
import { useCreateLot, useLots } from '@/features/lots/queries'
import type { LotFormInput, LotListParams } from '@/features/lots/schemas'

const statusFilterOptions: Array<{
  value: NonNullable<LotListParams['status']>
  label: string
}> = [
  { value: 'Active', label: 'Active' },
  { value: 'Archived', label: 'Archived' },
  { value: 'All', label: 'All' },
]

export const Route = createFileRoute('/_authenticated/lots/')({
  component: LotsPage,
})

function LotsPage() {
  const [status, setStatus] =
    useState<NonNullable<LotListParams['status']>>('Active')
  const { data, isLoading } = useLots({ status })
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
      <div className="mb-4 flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">Parking lots</h1>

        <div className="flex items-center gap-4">
          <Select
            value={status}
            onValueChange={(next) =>
              setStatus(next as NonNullable<LotListParams['status']>)
            }
          >
            <SelectTrigger className="w-36">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {statusFilterOptions.map((option) => (
                <SelectItem key={option.value} value={option.value}>
                  {option.label}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>

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
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <LotTable lots={data?.items ?? []} />
      )}
    </div>
  )
}
