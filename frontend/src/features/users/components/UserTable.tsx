import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { useChangeUserRole } from '../queries'
import type { User } from '../schemas'
import { DisableUserButton } from './DisableUserButton'
import { EnableUserButton } from './EnableUserButton'
import { RoleSelect } from './RoleSelect'

type Props = {
  users: User[]
}

const statusLabel: Record<User['status'], string> = {
  Active: 'Active',
  Disabled: 'Disabled',
}

export function UserTable({ users }: Props) {
  const changeRoleMutation = useChangeUserRole()

  if (users.length === 0) {
    return <p className="text-sm text-muted-foreground">No users found.</p>
  }

  return (
    <Table>
      <TableHeader>
        <TableRow>
          <TableHead>Email</TableHead>
          <TableHead>Display name</TableHead>
          <TableHead>Role</TableHead>
          <TableHead>Status</TableHead>
          <TableHead />
        </TableRow>
      </TableHeader>
      <TableBody>
        {users.map((user) => (
          <TableRow key={user.id}>
            <TableCell className="font-medium">{user.email}</TableCell>
            <TableCell>{user.displayName}</TableCell>
            <TableCell>
              <RoleSelect
                value={user.role}
                disabled={changeRoleMutation.isPending}
                onChange={(role) =>
                  changeRoleMutation.mutate({ id: user.id, input: { role } })
                }
              />
            </TableCell>
            <TableCell>{statusLabel[user.status]}</TableCell>
            <TableCell>
              {user.status === 'Active' ? (
                <DisableUserButton
                  userId={user.id}
                  displayName={user.displayName}
                />
              ) : (
                <EnableUserButton userId={user.id} />
              )}
            </TableCell>
          </TableRow>
        ))}
      </TableBody>
    </Table>
  )
}
