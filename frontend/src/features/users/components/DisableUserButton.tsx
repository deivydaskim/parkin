import { useState } from 'react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { useDisableUser } from '../queries'

type Props = {
  userId: string
  displayName: string
}

export function DisableUserButton({ userId, displayName }: Props) {
  const [open, setOpen] = useState(false)
  const disableUserMutation = useDisableUser()

  function handleConfirm() {
    disableUserMutation.mutate(userId, { onSuccess: () => setOpen(false) })
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          Disable
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Disable {displayName}?</DialogTitle>
          <DialogDescription>
            This ends their active session immediately and blocks sign-in until
            re-enabled.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={disableUserMutation.isPending}
          >
            {disableUserMutation.isPending ? 'Disabling…' : 'Disable'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
