import { Box, Map as MapIcon, RotateCcw, Tags } from 'lucide-react'
import { Button } from '@/components/ui/button'
import type { CameraMode } from './LotScene'

type Props = {
  cameraMode: CameraMode
  showLabels: boolean
  onCameraModeChange: (mode: CameraMode) => void
  onShowLabelsChange: (show: boolean) => void
  onReset: () => void
}

export function ViewControls({
  cameraMode,
  showLabels,
  onCameraModeChange,
  onShowLabelsChange,
  onReset,
}: Props) {
  return (
    <div
      role="group"
      aria-label="View controls"
      className="flex flex-wrap items-center gap-1"
    >
      <Button
        size="sm"
        variant="outline"
        onClick={() =>
          onCameraModeChange(cameraMode === 'top' ? 'perspective' : 'top')
        }
        aria-pressed={cameraMode === 'top'}
      >
        {cameraMode === 'top' ? <Box /> : <MapIcon />}
        {cameraMode === 'top' ? '3D view' : 'Top-down'}
      </Button>
      <Button
        size="sm"
        variant={showLabels ? 'default' : 'outline'}
        aria-pressed={showLabels}
        onClick={() => onShowLabelsChange(!showLabels)}
      >
        <Tags />
        Labels
      </Button>
      <Button size="sm" variant="outline" onClick={onReset}>
        <RotateCcw />
        Fit to lot
      </Button>
    </div>
  )
}
