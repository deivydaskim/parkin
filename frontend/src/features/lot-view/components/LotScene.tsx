import { useEffect } from 'react'
import { Canvas, useThree } from '@react-three/fiber'
import {
  OrbitControls,
  OrthographicCamera,
  PerspectiveCamera,
} from '@react-three/drei'
import type { Vector3 } from 'three'
import type { Bounds } from '../geometry'
import {
  LEVEL_HEIGHT,
  isLevelVisible,
  type LevelFilter,
  type SceneBay,
  type ScenePalette,
} from '../scene-model'
import { LotGround } from './LotGround'
import { SpaceBays } from './SpaceBays'

export type CameraMode = 'perspective' | 'top'

type Props = {
  bays: SceneBay[]
  bounds: Bounds
  levels: number[]
  levelFilter: LevelFilter
  palette: ScenePalette
  cameraMode: CameraMode
  resetToken: number
  showLabels: boolean
  hoveredId: string | null
  selectedId: string | null
  onHover: (id: string | null) => void
  onSelect: (id: string | null) => void
}

const MAX_POLAR_ANGLE = Math.PI / 2 - 0.12

export function LotScene({
  bays,
  bounds,
  levels,
  levelFilter,
  palette,
  cameraMode,
  resetToken,
  showLabels,
  hoveredId,
  selectedId,
  onHover,
  onSelect,
}: Props) {
  const visibleBays = bays.filter((bay) =>
    isLevelVisible(bay.placement.level, levelFilter),
  )
  const focusLevel = levelFilter === 'all' ? 0 : levelFilter

  return (
    <Canvas
      frameloop="demand"
      dpr={[1, 2]}
      onPointerMissed={() => onSelect(null)}
      style={{ background: palette.background, touchAction: 'none' }}
      aria-label="Interactive 3D view of the parking lot"
    >
      {cameraMode === 'perspective' ? (
        <PerspectiveCamera makeDefault fov={45} near={0.5} far={5000} />
      ) : (
        <OrthographicCamera makeDefault near={0.5} far={5000} />
      )}
      <OrbitControls
        makeDefault
        enableDamping={false}
        enableRotate={cameraMode === 'perspective'}
        screenSpacePanning={cameraMode === 'top'}
        maxPolarAngle={cameraMode === 'perspective' ? MAX_POLAR_ANGLE : 0}
        minDistance={4}
        maxDistance={1500}
      />
      <CameraRig
        bounds={bounds}
        mode={cameraMode}
        resetToken={resetToken}
        focusY={focusLevel * LEVEL_HEIGHT}
      />

      <ambientLight intensity={0.75} />
      <hemisphereLight args={['#ffffff', '#5b6470', 0.5]} />
      <directionalLight
        position={[bounds.maxX, 60, -bounds.minY + 30]}
        intensity={1.1}
      />

      <LotGround
        bounds={bounds}
        levels={levels}
        levelFilter={levelFilter}
        palette={palette}
        onBackgroundClick={() => onSelect(null)}
      />
      <SpaceBays
        bays={visibleBays}
        palette={palette}
        hoveredId={hoveredId}
        selectedId={selectedId}
        labelLevel={showLabels ? focusLevel : null}
        onHover={onHover}
        onSelect={onSelect}
      />
    </Canvas>
  )
}

type ControlsWithTarget = { target: Vector3; update: () => void }

type CameraRigProps = {
  bounds: Bounds
  mode: CameraMode
  resetToken: number
  focusY: number
}

function CameraRig({ bounds, mode, resetToken, focusY }: CameraRigProps) {
  const camera = useThree((state) => state.camera)
  const controls = useThree(
    (state) => state.controls,
  ) as ControlsWithTarget | null
  const getState = useThree((state) => state.get)
  const invalidate = useThree((state) => state.invalidate)

  const width = bounds.maxX - bounds.minX
  const length = bounds.maxY - bounds.minY
  const centerX = bounds.minX + width / 2
  const centerZ = -(bounds.minY + length / 2)

  useEffect(() => {
    const span = Math.max(width, length, 10)
    const { size, camera: activeCamera } = getState()

    if (mode === 'top') {
      activeCamera.position.set(centerX, focusY + span * 2, centerZ + 0.001)
      activeCamera.zoom = Math.max(
        0.5,
        Math.min(size.width / (width + 6), size.height / (length + 6)),
      )
    } else {
      activeCamera.position.set(
        centerX,
        focusY + span * 0.8,
        centerZ + span * 0.85,
      )
      activeCamera.zoom = 1
    }
    activeCamera.updateProjectionMatrix()

    if (controls) {
      controls.target.set(centerX, focusY, centerZ)
      controls.update()
    } else {
      activeCamera.lookAt(centerX, focusY, centerZ)
    }
    invalidate()
  }, [
    camera,
    controls,
    mode,
    resetToken,
    centerX,
    centerZ,
    width,
    length,
    focusY,
    getState,
    invalidate,
  ])

  return null
}
