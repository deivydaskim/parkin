import type { Bounds } from '../geometry'
import {
  LEVEL_HEIGHT,
  isLevelVisible,
  type LevelFilter,
  type ScenePalette,
} from '../scene-model'

type Props = {
  bounds: Bounds
  levels: number[]
  levelFilter: LevelFilter
  palette: ScenePalette
  onBackgroundClick: () => void
}

const SLAB_THICKNESS = 0.3
const CURB_HEIGHT = 0.18
const CURB_WIDTH = 0.3
const PILLAR_SIZE = 0.5

export function LotGround({
  bounds,
  levels,
  levelFilter,
  palette,
  onBackgroundClick,
}: Props) {
  const width = bounds.maxX - bounds.minX
  const length = bounds.maxY - bounds.minY
  const centerX = bounds.minX + width / 2
  const centerZ = -(bounds.minY + length / 2)

  return (
    <group>
      <mesh
        position={[centerX, -0.02, centerZ]}
        rotation={[-Math.PI / 2, 0, 0]}
        onClick={(event) => {
          event.stopPropagation()
          onBackgroundClick()
        }}
      >
        <planeGeometry args={[width + 40, length + 40]} />
        <meshStandardMaterial color={palette.background} roughness={1} />
      </mesh>

      {levels.map((level) =>
        isLevelVisible(level, levelFilter) ? (
          <LevelDeck
            key={level}
            level={level}
            width={width}
            length={length}
            centerX={centerX}
            centerZ={centerZ}
            bounds={bounds}
            palette={palette}
            isTranslucent={levelFilter === 'all' && level > 0}
          />
        ) : null,
      )}
    </group>
  )
}

type LevelDeckProps = {
  level: number
  width: number
  length: number
  centerX: number
  centerZ: number
  bounds: Bounds
  palette: ScenePalette
  isTranslucent: boolean
}

function LevelDeck({
  level,
  width,
  length,
  centerX,
  centerZ,
  bounds,
  palette,
  isTranslucent,
}: LevelDeckProps) {
  const floorY = level * LEVEL_HEIGHT
  const deckColor = level === 0 ? palette.ground : palette.slab
  const pillarCorners: Array<[number, number]> = [
    [bounds.minX + PILLAR_SIZE, -(bounds.minY + PILLAR_SIZE)],
    [bounds.maxX - PILLAR_SIZE, -(bounds.minY + PILLAR_SIZE)],
    [bounds.minX + PILLAR_SIZE, -(bounds.maxY - PILLAR_SIZE)],
    [bounds.maxX - PILLAR_SIZE, -(bounds.maxY - PILLAR_SIZE)],
  ]
  const curbs: Array<{
    position: [number, number, number]
    size: [number, number, number]
  }> = [
    {
      position: [centerX, floorY + CURB_HEIGHT / 2, -bounds.minY],
      size: [width, CURB_HEIGHT, CURB_WIDTH],
    },
    {
      position: [centerX, floorY + CURB_HEIGHT / 2, -bounds.maxY],
      size: [width, CURB_HEIGHT, CURB_WIDTH],
    },
    {
      position: [bounds.minX, floorY + CURB_HEIGHT / 2, centerZ],
      size: [CURB_WIDTH, CURB_HEIGHT, length],
    },
    {
      position: [bounds.maxX, floorY + CURB_HEIGHT / 2, centerZ],
      size: [CURB_WIDTH, CURB_HEIGHT, length],
    },
  ]

  return (
    <group>
      <mesh
        position={[centerX, floorY - SLAB_THICKNESS / 2, centerZ]}
        receiveShadow
      >
        <boxGeometry args={[width, SLAB_THICKNESS, length]} />
        <meshStandardMaterial
          color={deckColor}
          roughness={0.95}
          transparent={isTranslucent}
          opacity={isTranslucent ? 0.55 : 1}
          depthWrite={!isTranslucent}
        />
      </mesh>

      {curbs.map((curb, index) => (
        <mesh key={index} position={curb.position}>
          <boxGeometry args={curb.size} />
          <meshStandardMaterial color={palette.curb} roughness={0.8} />
        </mesh>
      ))}

      {level > 0
        ? pillarCorners.map(([x, z], index) => (
            <mesh key={index} position={[x, floorY - LEVEL_HEIGHT / 2, z]}>
              <boxGeometry
                args={[PILLAR_SIZE, LEVEL_HEIGHT - SLAB_THICKNESS, PILLAR_SIZE]}
              />
              <meshStandardMaterial color={palette.curb} roughness={0.8} />
            </mesh>
          ))
        : null}
    </group>
  )
}
