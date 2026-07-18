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
import { DriverForm } from '@/features/drivers/components/DriverForm'
import { DriverTable } from '@/features/drivers/components/DriverTable'
import { useCreateDriver, useDrivers } from '@/features/drivers/queries'
import type { DriverFormInput, DriverListParams } from '@/features/drivers/schemas'

const statusFilterOptions: Array<{
  value: NonNullable<DriverListParams['status']>
  label: string
}> = [
  { value: 'Active', label: 'Active' },
  { value: 'Archived', label: 'Archived' },
  { value: 'All', label: 'All' },
]

export const Route = createFileRoute('/_authenticated/drivers/')({
  component: DriversPage,
})

function DriversPage() {
  const [status, setStatus] =
    useState<NonNullable<DriverListParams['status']>>('Active')
  const { data, isLoading } = useDrivers({ status })
  const [open, setOpen] = useState(false)
  const createDriverMutation = useCreateDriver()

  function handleCreate(values: DriverFormInput) {
    createDriverMutation.mutate(values, {
      onSuccess: () => {
        setOpen(false)
        createDriverMutation.reset()
      },
    })
  }

  return (
    <div className="p-6">
      <div className="mb-4 flex items-center justify-between gap-4">
        <h1 className="text-xl font-semibold">Drivers</h1>

        <div className="flex items-center gap-4">
          <Select
            value={status}
            onValueChange={(next) =>
              setStatus(next as NonNullable<DriverListParams['status']>)
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
                <Button>New driver</Button>
              </DialogTrigger>
              <DialogContent>
                <DialogHeader>
                  <DialogTitle>Create driver</DialogTitle>
                </DialogHeader>
                <DriverForm
                  onSubmit={handleCreate}
                  isSubmitting={createDriverMutation.isPending}
                  error={createDriverMutation.error}
                  submitLabel="Create driver"
                />
              </DialogContent>
            </Dialog>
          </RoleGate>
        </div>
      </div>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : (
        <DriverTable drivers={data?.items ?? []} />
      )}
    </div>
  )
}
