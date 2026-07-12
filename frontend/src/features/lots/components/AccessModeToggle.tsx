import { Switch } from '@/components/ui/switch'
import { Label } from '@/components/ui/label'
import type { AccessMode } from '../schemas'

type Props = {
  value: AccessMode
  onChange: (value: AccessMode) => void
  disabled?: boolean
  id?: string
}

export function AccessModeToggle({
  value,
  onChange,
  disabled,
  id = 'access-mode',
}: Props) {
  const checked = value === 'RESTRICTED'

  return (
    <div className="flex items-center gap-2">
      <Switch
        id={id}
        checked={checked}
        disabled={disabled}
        onCheckedChange={(next) => onChange(next ? 'RESTRICTED' : 'OPEN')}
      />
      <Label htmlFor={id} className="font-normal">
        {checked ? 'Restricted' : 'Open'}
      </Label>
    </div>
  )
}
