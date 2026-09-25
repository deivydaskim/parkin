import { useState } from 'react'
import { Star } from 'lucide-react'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { ReservationPanel } from './ReservationPanel'

type Props = {
  lotId: string
  spaceId: string
  spaceLabel: string
  holderName?: string | null
}

export function ReservationDialog({
  lotId,
  spaceId,
  spaceLabel,
  holderName,
}: Props) {
  const [open, setOpen] = useState(false)

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger asChild>
        <Button variant="outline" size="sm">
          <Star />
          Reservation
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Reservation — {spaceLabel}</DialogTitle>
          <DialogDescription>
            A reserved space is held for one driver and bypasses the general
            pool.
          </DialogDescription>
        </DialogHeader>
        {open ? (
          <ReservationPanel
            lotId={lotId}
            spaceId={spaceId}
            spaceLabel={spaceLabel}
            holderName={holderName}
            onDone={() => setOpen(false)}
          />
        ) : null}
      </DialogContent>
    </Dialog>
  )
}
