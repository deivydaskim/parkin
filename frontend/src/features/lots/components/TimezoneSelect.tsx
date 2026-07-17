import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select'

const timezones = Intl.supportedValuesOf('timeZone')

type Props = {
  value: string
  onChange: (value: string) => void
  disabled?: boolean
  id?: string
}

export function TimezoneSelect({
  value,
  onChange,
  disabled,
  id = 'timezone',
}: Props) {
  return (
    <Select value={value} onValueChange={onChange} disabled={disabled}>
      <SelectTrigger id={id} className="w-full">
        <SelectValue placeholder="Select timezone" />
      </SelectTrigger>
      <SelectContent className="max-h-72">
        {timezones.map((timezone) => (
          <SelectItem key={timezone} value={timezone}>
            {timezone}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  )
}
