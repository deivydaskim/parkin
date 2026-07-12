import { Button } from '@/components/ui/button'
import { useEnableUser } from '../queries'

type Props = {
  userId: string
}

export function EnableUserButton({ userId }: Props) {
  const enableUserMutation = useEnableUser()

  return (
    <Button
      variant="outline"
      size="sm"
      onClick={() => enableUserMutation.mutate(userId)}
      disabled={enableUserMutation.isPending}
    >
      {enableUserMutation.isPending ? 'Enabling…' : 'Enable'}
    </Button>
  )
}
