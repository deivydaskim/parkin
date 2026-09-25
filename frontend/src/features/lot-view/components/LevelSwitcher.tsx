import { Button } from '@/components/ui/button'
import { levelName, type LevelFilter } from '../scene-model'

type Props = {
  levels: number[]
  value: LevelFilter
  onChange: (value: LevelFilter) => void
}

export function LevelSwitcher({ levels, value, onChange }: Props) {
  if (levels.length <= 1) return null

  return (
    <div
      role="group"
      aria-label="Visible levels"
      className="flex flex-wrap items-center gap-1"
    >
      <Button
        size="sm"
        variant={value === 'all' ? 'default' : 'outline'}
        aria-pressed={value === 'all'}
        onClick={() => onChange('all')}
      >
        All levels
      </Button>
      {levels.map((level) => (
        <Button
          key={level}
          size="sm"
          variant={value === level ? 'default' : 'outline'}
          aria-pressed={value === level}
          onClick={() => onChange(level)}
        >
          {levelName(level)}
        </Button>
      ))}
    </div>
  )
}
