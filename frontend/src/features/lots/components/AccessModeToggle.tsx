import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import { AccessMode, type AccessMode as AccessModeType } from '../schemas'

type Props = {
  value: AccessModeType
  onChange: (value: AccessModeType) => void
  disabled?: boolean
  id?: string
}

export function AccessModeToggle({
  value,
  onChange,
  disabled,
  id = 'access-mode',
}: Props) {
  const checked = value === AccessMode.Restricted

  return (
    <div className="flex items-center gap-2">
      <Switch
        id={id}
        checked={checked}
        disabled={disabled}
        onCheckedChange={(next) =>
          onChange(next ? AccessMode.Restricted : AccessMode.Open)
        }
      />
      <Label htmlFor={id} className="font-normal">
        {checked ? 'Restricted' : 'Open'}
      </Label>
    </div>
  )
}
