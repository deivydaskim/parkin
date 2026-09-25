import { useState } from 'react'
import { Ban, CircleCheck, MoreHorizontal, ShieldCheck } from 'lucide-react'
import { Avatar, AvatarFallback } from '@/components/ui/avatar'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuRadioGroup,
  DropdownMenuRadioItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { ConfirmDialog } from '@/components/ConfirmDialog'
import { StatusBadge } from '@/components/StatusBadge'
import { useAuthStore } from '@/features/auth/store'
import { initialsOf, roleLabels } from '@/lib/format'
import { useChangeUserRole, useDisableUser, useEnableUser } from '../queries'
import type { Role, User } from '../schemas'

type Props = {
  users: User[]
}

type PendingStatusChange = { user: User; action: 'disable' | 'enable' }

export function UserTable({ users }: Props) {
  const currentUserId = useAuthStore((state) => state.user?.id)
  const changeRoleMutation = useChangeUserRole()
  const disableMutation = useDisableUser()
  const enableMutation = useEnableUser()
  const [pending, setPending] = useState<PendingStatusChange | null>(null)
  const isDisable = pending?.action === 'disable'

  function handleConfirm() {
    if (!pending) return
    const mutation = isDisable ? disableMutation : enableMutation
    mutation.mutate(pending.user.id, { onSuccess: () => setPending(null) })
  }

  return (
    <div className="overflow-hidden rounded-xl border bg-card">
      <Table>
        <TableHeader>
          <TableRow className="bg-muted/40 hover:bg-muted/40">
            <TableHead>Staff member</TableHead>
            <TableHead>Role</TableHead>
            <TableHead>Status</TableHead>
            <TableHead className="w-12">
              <span className="sr-only">Actions</span>
            </TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {users.map((user) => {
            const isSelf = user.id === currentUserId
            return (
              <TableRow key={user.id}>
                <TableCell>
                  <div className="flex items-center gap-3">
                    <Avatar className="size-9">
                      <AvatarFallback className="bg-primary/10 text-xs font-semibold text-primary">
                        {initialsOf(user.displayName)}
                      </AvatarFallback>
                    </Avatar>
                    <div className="min-w-0">
                      <p className="flex items-center gap-2 truncate font-medium">
                        {user.displayName}
                        {isSelf ? <Badge variant="secondary">You</Badge> : null}
                      </p>
                      <p className="truncate text-xs text-muted-foreground">
                        {user.email}
                      </p>
                    </div>
                  </div>
                </TableCell>
                <TableCell>
                  <StatusBadge status={user.role} />
                </TableCell>
                <TableCell>
                  <StatusBadge status={user.status} />
                </TableCell>
                <TableCell>
                  {isSelf ? null : (
                    <DropdownMenu>
                      <DropdownMenuTrigger asChild>
                        <Button
                          variant="ghost"
                          size="icon-sm"
                          aria-label={`Actions for ${user.displayName}`}
                        >
                          <MoreHorizontal />
                        </Button>
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end" className="w-48">
                        <DropdownMenuLabel className="flex items-center gap-2 text-xs font-normal text-muted-foreground">
                          <ShieldCheck className="size-3.5" />
                          Role
                        </DropdownMenuLabel>
                        <DropdownMenuRadioGroup
                          value={user.role}
                          onValueChange={(role) =>
                            changeRoleMutation.mutate({
                              id: user.id,
                              input: { role: role as Role },
                            })
                          }
                        >
                          <DropdownMenuRadioItem value="Operator">
                            {roleLabels.Operator}
                          </DropdownMenuRadioItem>
                          <DropdownMenuRadioItem value="SystemAdmin">
                            {roleLabels.SystemAdmin}
                          </DropdownMenuRadioItem>
                        </DropdownMenuRadioGroup>
                        <DropdownMenuSeparator />
                        {user.status === 'Active' ? (
                          <DropdownMenuItem
                            variant="destructive"
                            onSelect={() => setPending({ user, action: 'disable' })}
                          >
                            <Ban />
                            Disable account
                          </DropdownMenuItem>
                        ) : (
                          <DropdownMenuItem
                            onSelect={() => setPending({ user, action: 'enable' })}
                          >
                            <CircleCheck />
                            Enable account
                          </DropdownMenuItem>
                        )}
                      </DropdownMenuContent>
                    </DropdownMenu>
                  )}
                </TableCell>
              </TableRow>
            )
          })}
        </TableBody>
      </Table>

      <ConfirmDialog
        open={pending !== null}
        onOpenChange={(open) => {
          if (!open) setPending(null)
        }}
        title={
          isDisable
            ? `Disable ${pending?.user.displayName}?`
            : `Enable ${pending?.user.displayName}?`
        }
        description={
          isDisable
            ? 'This ends their active session immediately and blocks sign-in until re-enabled.'
            : 'They will be able to sign in again with their existing password.'
        }
        confirmLabel={isDisable ? 'Disable account' : 'Enable account'}
        pendingLabel={isDisable ? 'Disabling…' : 'Enabling…'}
        destructive={isDisable}
        isPending={disableMutation.isPending || enableMutation.isPending}
        onConfirm={handleConfirm}
      />
    </div>
  )
}
