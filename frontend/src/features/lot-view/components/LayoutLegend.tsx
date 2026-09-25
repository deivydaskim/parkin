import type { ScenePalette } from '../scene-model'

type Props = {
  palette: ScenePalette
}

type LegendItem = {
  label: string
  color: string
  marker: 'none' | 'solid' | 'hollow' | 'cross'
}

function Swatch({ color, marker }: Pick<LegendItem, 'color' | 'marker'>) {
  return (
    <span
      aria-hidden
      className="relative inline-flex size-4 shrink-0 items-center justify-center rounded-sm border border-foreground/30"
      style={{ backgroundColor: color, opacity: marker === 'cross' ? 0.6 : 1 }}
    >
      {marker === 'solid' ? (
        <span className="size-2 rounded-[1px] bg-foreground/80" />
      ) : null}
      {marker === 'hollow' ? (
        <span className="size-2 rounded-[1px] border border-foreground/80" />
      ) : null}
      {marker === 'cross' ? (
        <span className="text-[10px] leading-none text-foreground">✕</span>
      ) : null}
    </span>
  )
}

export function LayoutLegend({ palette }: Props) {
  const items: LegendItem[] = [
    { label: 'General', color: palette.general, marker: 'none' },
    {
      label: 'Reserved · assigned (solid sign)',
      color: palette.reservedAssigned,
      marker: 'solid',
    },
    {
      label: 'Reserved · unassigned (open sign)',
      color: palette.reservedUnassigned,
      marker: 'hollow',
    },
    {
      label: 'Inactive (ghosted, crossed)',
      color: palette.inactive,
      marker: 'cross',
    },
    { label: 'Unsaved preview', color: palette.preview, marker: 'none' },
  ]

  return (
    <details
      open
      className="group rounded-md border bg-background/90 p-2.5 text-xs shadow-sm backdrop-blur"
    >
      <summary className="cursor-pointer font-medium select-none group-open:mb-1.5">
        Legend
      </summary>
      <ul className="space-y-1">
        {items.map((item) => (
          <li key={item.label} className="flex items-center gap-2">
            <Swatch color={item.color} marker={item.marker} />
            {item.label}
          </li>
        ))}
      </ul>
      <p className="mt-2 max-w-52 text-[11px] text-muted-foreground">
        Bays show configuration only — never whether a car is parked there.
      </p>
    </details>
  )
}
