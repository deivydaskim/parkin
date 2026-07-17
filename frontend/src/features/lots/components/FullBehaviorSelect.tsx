import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { FullBehavior, type FullBehavior as FullBehaviorType } from '../schemas'

type Props = {
  value: FullBehaviorType
  onChange: (value: FullBehaviorType) => void
  disabled?: boolean
  id?: string
}

export function FullBehaviorSelect({
  value,
  onChange,
  disabled,
  id = 'full-behavior',
}: Props) {
  return (
    <Select
      value={value}
      onValueChange={(next) => onChange(next as FullBehaviorType)}
      disabled={disabled}
    >
      <SelectTrigger id={id} className="w-full">
        <SelectValue placeholder="Select full-lot behavior" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={FullBehavior.Block}>
          Block (deny entry when full)
        </SelectItem>
        <SelectItem value={FullBehavior.AllowOverflow}>
          Allow overflow
        </SelectItem>
      </SelectContent>
    </Select>
  )
}
