import type { LayoutSpace } from '../schemas'
import { SpaceList } from './SpaceList'

type Props = {
  spaces: LayoutSpace[]
  selectedId: string | null
  onSelect: (id: string) => void
}

export function UnplacedTray({ spaces, selectedId, onSelect }: Props) {
  return (
    <SpaceList
      title="Unplaced"
      description="Not on the map yet. Select one to place it, or use Auto-arrange."
      spaces={spaces}
      selectedId={selectedId}
      onSelect={onSelect}
      emptyText="Every space has a position."
    />
  )
}
