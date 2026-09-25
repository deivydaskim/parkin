import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRightLeft, Plus, Power, PowerOff, RectangleHorizontal } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import type { ComboboxOption } from '@/components/EntityCombobox'
import { EmptyState } from '@/components/EmptyState'
import { PlateChip } from '@/components/PlateChip'
import { ListSkeleton } from '@/components/Skeletons'
import { StatusBadge } from '@/components/StatusBadge'
import { RoleGate } from '@/features/auth/components/RoleGate'
import {
  useAddPlate,
  useDeactivatePlate,
  usePlates,
  useReactivatePlate,
  useReassignPlate,
} from '../queries'
import {
  PlateStatus,
  plateFormSchema,
  type Plate,
  type PlateFormInput,
} from '../schemas'
import { DriverCombobox } from './DriverCombobox'

type Props = {
  driverId: string
  canEdit: boolean
}

export function PlateManager({ driverId, canEdit }: Props) {
  const { data: platesData, isLoading } = usePlates(driverId, { perPage: 100 })
  const addPlateMutation = useAddPlate(driverId)
  const plates = platesData?.items ?? []

  const form = useForm<PlateFormInput>({
    resolver: zodResolver(plateFormSchema),
    defaultValues: { plateNumber: '' },
  })

  function handleAddPlate(values: PlateFormInput) {
    addPlateMutation.mutate(values, {
      onSuccess: () => form.reset(),
    })
  }

  return (
    <div className="space-y-4">
      {canEdit ? (
        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <Form {...form}>
            <form
              onSubmit={form.handleSubmit(handleAddPlate)}
              className="flex flex-col gap-2 rounded-xl border bg-card p-4 sm:flex-row sm:items-end"
            >
              <FormField
                control={form.control}
                name="plateNumber"
                render={({ field }) => (
                  <FormItem className="flex-1">
                    <FormLabel>Add a plate</FormLabel>
                    <FormControl>
                      <Input
                        autoComplete="off"
                        placeholder="ABC 123"
                        spellCheck={false}
                        className="font-mono tracking-widest uppercase"
                        {...field}
                        onChange={(event) =>
                          field.onChange(event.target.value.toUpperCase())
                        }
                      />
                    </FormControl>
                    <FormMessage />
                  </FormItem>
                )}
              />
              <Button type="submit" disabled={addPlateMutation.isPending}>
                <Plus />
                {addPlateMutation.isPending ? 'Adding…' : 'Add plate'}
              </Button>
            </form>
          </Form>
        </RoleGate>
      ) : null}

      {isLoading ? (
        <ListSkeleton rows={2} />
      ) : plates.length === 0 ? (
        <EmptyState
          icon={RectangleHorizontal}
          title="No plates yet"
          hint="Add at least one plate so the gate can recognise this driver."
        />
      ) : (
        <ul className="divide-y rounded-xl border bg-card">
          {plates.map((plate) => (
            <PlateRow
              key={plate.id}
              plate={plate}
              driverId={driverId}
              canEdit={canEdit}
            />
          ))}
        </ul>
      )}
    </div>
  )
}

type PlateRowProps = {
  plate: Plate
  driverId: string
  canEdit: boolean
}

function PlateRow({ plate, driverId, canEdit }: PlateRowProps) {
  const [isReassignOpen, setIsReassignOpen] = useState(false)
  const [target, setTarget] = useState<ComboboxOption | null>(null)
  const [confirmDeactivate, setConfirmDeactivate] = useState(false)
  const reassignMutation = useReassignPlate(driverId)
  const deactivateMutation = useDeactivatePlate(driverId)
  const reactivateMutation = useReactivatePlate(driverId)
  const isActive = plate.status === PlateStatus.Active

  function closeReassign(open: boolean) {
    setIsReassignOpen(open)
    if (!open) setTarget(null)
  }

  function handleReassign() {
    if (!target) return
    reassignMutation.mutate(
      { plateId: plate.id, targetDriverId: target.id },
      { onSuccess: () => closeReassign(false) },
    )
  }

  return (
    <li className="flex flex-col gap-3 p-4 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-3">
        <PlateChip plate={plate.normalizedPlateNumber} muted={!isActive} />
        <StatusBadge status={plate.status} />
      </div>
      {canEdit ? (
        <RoleGate roles={['Operator', 'SystemAdmin']}>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" onClick={() => setIsReassignOpen(true)}>
              <ArrowRightLeft />
              Reassign
            </Button>
            {isActive ? (
              <Button
                variant="outline"
                size="sm"
                onClick={() => setConfirmDeactivate(true)}
              >
                <PowerOff />
                Deactivate
              </Button>
            ) : (
              <Button
                variant="outline"
                size="sm"
                disabled={reactivateMutation.isPending}
                onClick={() => reactivateMutation.mutate(plate.id)}
              >
                <Power />
                {reactivateMutation.isPending ? 'Reactivating…' : 'Reactivate'}
              </Button>
            )}
          </div>
        </RoleGate>
      ) : null}

      <Dialog open={isReassignOpen} onOpenChange={closeReassign}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Reassign plate</DialogTitle>
            <DialogDescription>
              Move <span className="font-mono font-semibold">{plate.normalizedPlateNumber}</span>{' '}
              to another driver. Future gate events will match them instead.
            </DialogDescription>
          </DialogHeader>
          <div className="space-y-2">
            <Label htmlFor={`reassign-${plate.id}`}>New owner</Label>
            <DriverCombobox
              id={`reassign-${plate.id}`}
              value={target?.id ?? null}
              selectedLabel={target?.label}
              onChange={setTarget}
              excludeIds={[driverId]}
            />
          </div>
          <DialogFooter>
            <Button variant="outline" onClick={() => closeReassign(false)}>
              Cancel
            </Button>
            <Button
              onClick={handleReassign}
              disabled={!target || reassignMutation.isPending}
            >
              {reassignMutation.isPending ? 'Reassigning…' : 'Reassign plate'}
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <ConfirmDialog
        open={confirmDeactivate}
        onOpenChange={setConfirmDeactivate}
        title={`Deactivate ${plate.normalizedPlateNumber}?`}
        description="The gate will treat this plate as unknown until it is reactivated."
        confirmLabel="Deactivate"
        pendingLabel="Deactivating…"
        destructive
        isPending={deactivateMutation.isPending}
        onConfirm={() =>
          deactivateMutation.mutate(plate.id, {
            onSuccess: () => setConfirmDeactivate(false),
          })
        }
      />
    </li>
  )
}
