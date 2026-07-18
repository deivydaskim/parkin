import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'
import { SpaceType, type SpaceType as SpaceTypeType } from '../schemas'

type Props = {
  value: SpaceTypeType
  onChange: (value: SpaceTypeType) => void
  disabled?: boolean
  id?: string
}

export function SpaceTypeSelect({
  value,
  onChange,
  disabled,
  id = 'space-type',
}: Props) {
  return (
    <Select
      value={value}
      onValueChange={(next) => onChange(next as SpaceTypeType)}
      disabled={disabled}
    >
      <SelectTrigger id={id} className="w-full">
        <SelectValue placeholder="Select space type" />
      </SelectTrigger>
      <SelectContent>
        <SelectItem value={SpaceType.General}>General</SelectItem>
        <SelectItem value={SpaceType.Reserved}>Reserved</SelectItem>
      </SelectContent>
    </Select>
  )
}
