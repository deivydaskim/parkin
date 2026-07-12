import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import type { FullBehavior } from '../schemas'

interface FullBehaviorSelectProps {
  value: FullBehavior
  onChange: (value: FullBehavior) => void
  disabled?: boolean
  id?: string
}

export function FullBehaviorSelect({
  value,
  onChange,
  disabled,
  id = 'full-behavior',
}: FullBehaviorSelectProps) {
  return (
    <Select
      value={value}
      onValueChange={(next) => onChange(next as FullBehavior)}
      disabled={disabled}
    >
      <SelectTrigger id={id} className="w-full">
        <SelectValue placeholder="Select full-lot behavior" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value="BLOCK">Block (deny entry when full)</SelectItem>
        <SelectItem value="ALLOW_OVERFLOW">Allow overflow</SelectItem>
      </SelectContent>
    </Select>
  )
}
