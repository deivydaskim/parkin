import { useState } from 'react'
import { Info, KeyRound, Plus } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog'
import { DataTablePagination } from '@/components/DataTablePagination'
import { EmptyState } from '@/components/EmptyState'
import { ListSkeleton } from '@/components/Skeletons'
import { RoleGate } from '@/features/auth/components/RoleGate'
import { AccessMode, LotStatus, type Lot } from '@/features/lots/schemas'
import { useCreateGrant, useGrantsByLot, useRevokeGrant } from '../queries'
import type { GrantFormInput } from '../schemas'
import { GrantForm } from './GrantForm'
import { GrantList } from './GrantList'

const PAGE_SIZE = 20

type Props = {
  lot: Lot
  canEdit: boolean
}

export function LotGrantsPanel({ lot, canEdit }: Props) {
  const [page, setPage] = useState(1)
  const [isGrantOpen, setIsGrantOpen] = useState(false)
  const { data: grantsData, isLoading } = useGrantsByLot(lot.id, {
    page,
    perPage: PAGE_SIZE,
  })
  const createGrantMutation = useCreateGrant()
  const revokeGrantMutation = useRevokeGrant()
  const grants = grantsData?.items ?? []
  const isArchived = lot.status !== LotStatus.Active

  function handleCreate(values: GrantFormInput) {
    createGrantMutation.mutate(
      {
        driverId: values.targetId,
        lotId: lot.id,
        validFrom: values.validFrom,
        validTo: values.validTo,
      },
      { onSuccess: () => setIsGrantOpen(false) },
    )
  }

  const grantButton = canEdit ? (
    <RoleGate roles={['Operator', 'SystemAdmin']}>
      <Button onClick={() => setIsGrantOpen(true)} disabled={isArchived}>
        <Plus />
        Grant access
      </Button>
    </RoleGate>
  ) : null

  return (
    <div className="space-y-4">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <p className="text-sm text-muted-foreground">
          Grants decide which drivers may enter this lot while it is restricted.
        </p>
        {grants.length > 0 ? grantButton : null}
      </div>

      {lot.accessMode === AccessMode.Open ? (
        <p className="flex items-start gap-2 rounded-lg border border-info/30 bg-info/10 p-3 text-sm">
          <Info className="mt-0.5 size-4 shrink-0 text-info" aria-hidden />
          <span>
            This lot is open, so grants have no effect until it is switched to
            restricted.
          </span>
        </p>
      ) : null}

      {isLoading ? (
        <ListSkeleton rows={2} />
      ) : grants.length === 0 ? (
        <EmptyState
          icon={KeyRound}
          title="No drivers granted access"
          hint="When this lot is restricted, only drivers with a grant can enter."
          action={grantButton}
        />
      ) : (
        <GrantList
          subject="driver"
          grants={grants}
          canEdit={canEdit}
          isRevoking={revokeGrantMutation.isPending}
          onRevoke={(grantId, onDone) =>
            revokeGrantMutation.mutate(grantId, { onSuccess: onDone })
          }
        />
      )}

      {grantsData && grantsData.totalPages > 1 ? (
        <DataTablePagination
          page={page}
          totalPages={grantsData.totalPages}
          totalCount={grantsData.totalCount}
          perPage={PAGE_SIZE}
          itemLabel="grants"
          onPageChange={setPage}
        />
      ) : null}

      <Dialog open={isGrantOpen} onOpenChange={setIsGrantOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Grant driver access</DialogTitle>
            <DialogDescription>
              Let a driver enter this lot, optionally for a limited period.
            </DialogDescription>
          </DialogHeader>
          {isGrantOpen ? (
            <GrantForm
              pick="driver"
              onSubmit={handleCreate}
              isSubmitting={createGrantMutation.isPending}
            />
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  )
}
