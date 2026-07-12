import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { Role } from '../schemas'

type Props = {
  value: Role
  onChange: (value: Role) => void
  disabled?: boolean
  id?: string
}

export function RoleSelect({ value, onChange, disabled, id = 'role' }: Props) {
  return (
    <Select
      value={value}
      onValueChange={(next) => onChange(next as Role)}
      disabled={disabled}
    >
      <SelectTrigger id={id} className="w-full">
        <SelectValue placeholder="Select role" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="Operator">Operator</SelectItem>
        <SelectItem value="SystemAdmin">System Admin</SelectItem>
      </SelectContent>
    </Select>
  )
}
