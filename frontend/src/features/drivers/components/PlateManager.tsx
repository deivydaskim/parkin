import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
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
  DialogFooter,
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
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import {
  useAddPlate,
  useDeactivatePlate,
  useDrivers,
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

type Props = {
  driverId: string
}

export function PlateManager({ driverId }: Props) {
  const { data: platesData, isLoading } = usePlates(driverId)
  const addPlateMutation = useAddPlate(driverId)
  const reassignPlateMutation = useReassignPlate(driverId)
  const deactivatePlateMutation = useDeactivatePlate(driverId)
  const reactivatePlateMutation = useReactivatePlate(driverId)

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
      <Form {...form}>
        <form
          onSubmit={form.handleSubmit(handleAddPlate)}
          className="flex items-end gap-2"
        >
          <FormField
            control={form.control}
            name="plateNumber"
            render={({ field }) => (
              <FormItem className="flex-1">
                <FormLabel>Plate number</FormLabel>
                <FormControl>
                  <Input autoComplete="off" {...field} />
                </FormControl>
                <FormMessage />
              </FormItem>
            )}
          />
          <Button type="submit" disabled={addPlateMutation.isPending}>
            {addPlateMutation.isPending ? 'Adding…' : 'Add plate'}
          </Button>
        </form>
      </Form>

      {isLoading ? (
        <p className="text-sm text-muted-foreground">Loading…</p>
      ) : platesData && platesData.items.length > 0 ? (
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Plate number</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Actions</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {platesData.items.map((plate) => (
              <PlateRow
                key={plate.id}
                plate={plate}
                currentDriverId={driverId}
                onReassign={(targetDriverId) =>
                  reassignPlateMutation.mutate({
                    plateId: plate.id,
                    targetDriverId,
                  })
                }
                isReassigning={reassignPlateMutation.isPending}
                onDeactivate={() => deactivatePlateMutation.mutate(plate.id)}
                isDeactivating={deactivatePlateMutation.isPending}
                onReactivate={() => reactivatePlateMutation.mutate(plate.id)}
                isReactivating={reactivatePlateMutation.isPending}
              />
            ))}
          </TableBody>
        </Table>
      ) : (
        <p className="text-sm text-muted-foreground">No plates yet.</p>
      )}
    </div>
  )
}

type PlateRowProps = {
  plate: Plate
  currentDriverId: string
  onReassign: (targetDriverId: string) => void
  isReassigning: boolean
  onDeactivate: () => void
  isDeactivating: boolean
  onReactivate: () => void
  isReactivating: boolean
}

function PlateRow({
  plate,
  currentDriverId,
  onReassign,
  isReassigning,
  onDeactivate,
  isDeactivating,
  onReactivate,
  isReactivating,
}: PlateRowProps) {
  const [open, setOpen] = useState(false)
  const [targetDriverId, setTargetDriverId] = useState<string>('')
  const { data: driversData } = useDrivers({ perPage: 100 })

  const otherDrivers = (driversData?.items ?? []).filter(
    (driver) => driver.id !== currentDriverId,
  )

  function handleConfirm() {
    if (!targetDriverId) return
    onReassign(targetDriverId)
    setOpen(false)
    setTargetDriverId('')
  }

  return (
    <TableRow>
      <TableCell className="font-medium">
        {plate.normalizedPlateNumber}
      </TableCell>
      <TableCell>{plate.status}</TableCell>
      <TableCell className="text-right space-x-2">
        {plate.status === PlateStatus.Active ? (
          <Button
            variant="outline"
            size="sm"
            onClick={onDeactivate}
            disabled={isDeactivating}
          >
            {isDeactivating ? 'Deactivating…' : 'Deactivate'}
          </Button>
        ) : (
          <Button
            variant="outline"
            size="sm"
            onClick={onReactivate}
            disabled={isReactivating}
          >
            {isReactivating ? 'Reactivating…' : 'Reactivate'}
          </Button>
        )}
        <Dialog open={open} onOpenChange={setOpen}>
          <DialogTrigger asChild>
            <Button variant="outline" size="sm">
              Reassign
            </Button>
          </DialogTrigger>
          <DialogContent>
            <DialogHeader>
              <DialogTitle>
                Reassign {plate.normalizedPlateNumber}
              </DialogTitle>
            </DialogHeader>

            <Select value={targetDriverId} onValueChange={setTargetDriverId}>
              <SelectTrigger>
                <SelectValue placeholder="Select a driver" />
              </SelectTrigger>
              <SelectContent>
                {otherDrivers.map((driver) => (
                  <SelectItem key={driver.id} value={driver.id}>
                    {driver.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>

            <DialogFooter>
              <Button
                onClick={handleConfirm}
                disabled={!targetDriverId || isReassigning}
              >
                {isReassigning ? 'Reassigning…' : 'Confirm reassignment'}
              </Button>
            </DialogFooter>
          </DialogContent>
        </Dialog>
      </TableCell>
    </TableRow>
  )
}
