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
import { useRevokeApiKey } from '../queries'

type Props = {
  apiKeyId: string
  name: string
}

export function RevokeApiKeyButton({ apiKeyId, name }: Props) {
  const [open, setOpen] = useState(false)
  const revokeApiKeyMutation = useRevokeApiKey()

  function handleConfirm() {
    revokeApiKeyMutation.mutate(apiKeyId, { onSuccess: () => setOpen(false) })
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          Revoke
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Revoke "{name}"?</DialogTitle>
          <DialogDescription>
            Any integration using this key will be rejected immediately. This
            cannot be undone.
          </DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => setOpen(false)}>
            Cancel
          </Button>
          <Button
            variant="destructive"
            onClick={handleConfirm}
            disabled={revokeApiKeyMutation.isPending}
          >
            {revokeApiKeyMutation.isPending ? 'Revoking…' : 'Revoke'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  )
}
