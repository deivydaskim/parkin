import { useState } from 'react'
import { Archive, ArchiveRestore } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import { lotToFormValues } from '../form-values'
import { useArchiveLot, useRestoreLot, useUpdateLot } from '../queries'
import { LotStatus, type Lot, type LotFormInput } from '../schemas'
import { LotStatusConfirm } from './LotActions'
import { LotForm } from './LotForm'

type Props = {
  lot: Lot
}

export function LotSettingsPanel({ lot }: Props) {
  const [confirm, setConfirm] = useState<'archive' | 'restore' | null>(null)
  const updateMutation = useUpdateLot(lot.id)
  const archiveMutation = useArchiveLot(lot.id)
  const restoreMutation = useRestoreLot(lot.id)
  const isArchived = lot.status === LotStatus.Archived

  function handleUpdate(values: LotFormInput) {
    updateMutation.mutate(values)
  }

  function handleConfirm() {
    const mutation = confirm === 'archive' ? archiveMutation : restoreMutation
    mutation.mutate(undefined, { onSuccess: () => setConfirm(null) })
  }

  return (
    <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
      <Card>
        <CardHeader>
          <CardTitle>Lot details</CardTitle>
          <CardDescription>
            Name, location, access rules and the optional footprint for the 3D
            view.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <LotForm
            key={`${lot.id}-${lot.name}-${lot.accessMode}-${lot.fullBehavior}`}
            defaultValues={lotToFormValues(lot)}
            onSubmit={handleUpdate}
            isSubmitting={updateMutation.isPending}
            error={updateMutation.error}
            submitLabel="Save changes"
          />
        </CardContent>
      </Card>

      <Card className="h-fit border-destructive/30">
        <CardHeader>
          <CardTitle className="text-destructive">Danger zone</CardTitle>
          <CardDescription>
            {isArchived
              ? 'This lot is archived and denies all new entries.'
              : 'Archiving stops all new entries. Cars inside can still leave.'}
          </CardDescription>
        </CardHeader>
        <CardContent>
          {isArchived ? (
            <Button variant="outline" onClick={() => setConfirm('restore')}>
              <ArchiveRestore />
              Restore lot
            </Button>
          ) : (
            <Button variant="destructive" onClick={() => setConfirm('archive')}>
              <Archive />
              Archive lot
            </Button>
          )}
        </CardContent>
      </Card>

      <LotStatusConfirm
        lot={lot}
        action={confirm}
        onClose={() => setConfirm(null)}
        onConfirm={handleConfirm}
        isPending={archiveMutation.isPending || restoreMutation.isPending}
      />
    </div>
  )
}
