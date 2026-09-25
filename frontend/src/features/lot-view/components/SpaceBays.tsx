import { useEffect } from 'react'
import { Html, Instance, Instances } from '@react-three/drei'
import { useThree, type ThreeEvent } from '@react-three/fiber'
import { degreesToRadians } from '../geometry'
import { LEVEL_HEIGHT, type SceneBay, type ScenePalette } from '../scene-model'

type Props = {
  bays: SceneBay[]
  palette: ScenePalette
  hoveredId: string | null
  selectedId: string | null
  labelLevel: number | null
  onHover: (id: string | null) => void
  onSelect: (id: string) => void
}

const SLAB_HEIGHT = 0.04
const PAINT_HEIGHT = 0.02
const LINE_WIDTH = 0.1
const POST_HEIGHT = 1.3

function instanceLimit(count: number) {
  return Math.max(64, 2 ** Math.ceil(Math.log2(Math.max(count, 1))))
}

function bayTransform(bay: SceneBay) {
  return {
    position: [
      bay.placement.x,
      bay.placement.level * LEVEL_HEIGHT + 0.01,
      -bay.placement.y,
    ] as [number, number, number],
    rotation: [0, degreesToRadians(bay.placement.rotationDegrees), 0] as [
      number,
      number,
      number,
    ],
  }
}

function slabColor(
  bay: SceneBay,
  palette: ScenePalette,
  hoveredId: string | null,
  selectedId: string | null,
) {
  if (bay.id === selectedId) return palette.selected
  if (bay.id === hoveredId) return palette.hover
  if (bay.isPreview) return palette.preview
  return palette[bay.style]
}

export function SpaceBays({
  bays,
  palette,
  hoveredId,
  selectedId,
  labelLevel,
  onHover,
  onSelect,
}: Props) {
  const activeBays = bays.filter((bay) => bay.style !== 'inactive')
  const inactiveBays = bays.filter((bay) => bay.style === 'inactive')
  const reservedBays = bays.filter(
    (bay) =>
      bay.style === 'reservedAssigned' || bay.style === 'reservedUnassigned',
  )
  const invalidate = useThree((state) => state.invalidate)

  useEffect(() => {
    invalidate()
    const frame = requestAnimationFrame(() => invalidate())
    return () => cancelAnimationFrame(frame)
  }, [bays, hoveredId, selectedId, palette, labelLevel, invalidate])

  const focusedBay =
    bays.find((bay) => bay.id === selectedId) ??
    bays.find((bay) => bay.id === hoveredId)

  const handlers = (id: string) => ({
    onPointerOver: (event: ThreeEvent<PointerEvent>) => {
      event.stopPropagation()
      onHover(id)
    },
    onPointerOut: (event: ThreeEvent<PointerEvent>) => {
      event.stopPropagation()
      onHover(null)
    },
    onClick: (event: ThreeEvent<MouseEvent>) => {
      event.stopPropagation()
      onSelect(id)
    },
  })

  return (
    <group>
      <Instances
        key={instanceLimit(activeBays.length)}
        limit={instanceLimit(activeBays.length)}
      >
        <boxGeometry />
        <meshStandardMaterial roughness={0.9} />
        {activeBays.map((bay) => (
          <group key={bay.id} {...bayTransform(bay)}>
            <Instance
              position={[0, SLAB_HEIGHT / 2, 0]}
              scale={[
                bay.placement.width * 0.96,
                SLAB_HEIGHT,
                bay.placement.length * 0.98,
              ]}
              color={slabColor(bay, palette, hoveredId, selectedId)}
              {...handlers(bay.id)}
            />
          </group>
        ))}
      </Instances>

      <Instances
        key={instanceLimit(inactiveBays.length)}
        limit={instanceLimit(inactiveBays.length)}
      >
        <boxGeometry />
        <meshStandardMaterial
          transparent
          opacity={0.45}
          depthWrite={false}
          roughness={1}
        />
        {inactiveBays.map((bay) => (
          <group key={bay.id} {...bayTransform(bay)}>
            <Instance
              position={[0, SLAB_HEIGHT / 2, 0]}
              scale={[
                bay.placement.width * 0.96,
                SLAB_HEIGHT,
                bay.placement.length * 0.98,
              ]}
              color={slabColor(bay, palette, hoveredId, selectedId)}
              {...handlers(bay.id)}
            />
          </group>
        ))}
      </Instances>

      <Instances
        key={instanceLimit(bays.length * 4 + inactiveBays.length * 2)}
        limit={instanceLimit(bays.length * 4 + inactiveBays.length * 2)}
      >
        <boxGeometry />
        <meshStandardMaterial roughness={0.6} />
        {bays.map((bay) => (
          <BayPaint key={bay.id} bay={bay} color={palette.paint} />
        ))}
      </Instances>

      <Instances
        key={instanceLimit(reservedBays.length * 5)}
        limit={instanceLimit(reservedBays.length * 5)}
      >
        <boxGeometry />
        <meshStandardMaterial roughness={0.5} metalness={0.2} />
        {reservedBays.map((bay) => (
          <ReservedMarker key={bay.id} bay={bay} palette={palette} />
        ))}
      </Instances>

      {labelLevel !== null
        ? bays
            .filter((bay) => bay.placement.level === labelLevel)
            .map((bay) => (
              <Html
                key={bay.id}
                center
                position={[
                  bay.placement.x,
                  bay.placement.level * LEVEL_HEIGHT + 0.2,
                  -bay.placement.y,
                ]}
                zIndexRange={[10, 0]}
                style={{ pointerEvents: 'none' }}
              >
                <span className="rounded bg-background/80 px-1 text-[10px] font-medium whitespace-nowrap text-foreground tabular-nums">
                  {bay.label}
                </span>
              </Html>
            ))
        : null}

      {focusedBay ? <BayTooltip bay={focusedBay} /> : null}
    </group>
  )
}

function BayPaint({ bay, color }: { bay: SceneBay; color: string }) {
  const { width, length } = bay.placement
  const y = SLAB_HEIGHT + PAINT_HEIGHT / 2
  const diagonal = Math.hypot(width, length) * 0.85
  const diagonalAngle = Math.atan2(width, length)

  return (
    <group {...bayTransform(bay)}>
      <Instance
        position={[-width / 2, y, 0]}
        scale={[LINE_WIDTH, PAINT_HEIGHT, length]}
        color={color}
      />
      <Instance
        position={[width / 2, y, 0]}
        scale={[LINE_WIDTH, PAINT_HEIGHT, length]}
        color={color}
      />
      <Instance
        position={[0, y, length / 2]}
        scale={[width + LINE_WIDTH, PAINT_HEIGHT, LINE_WIDTH]}
        color={color}
      />
      <Instance
        position={[0, 0.07, length / 2 - 0.55]}
        scale={[width * 0.55, 0.12, 0.18]}
        color={color}
      />
      {bay.style === 'inactive' ? (
        <>
          <Instance
            position={[0, y, 0]}
            rotation={[0, diagonalAngle, 0]}
            scale={[LINE_WIDTH * 1.5, PAINT_HEIGHT, diagonal]}
            color={color}
          />
          <Instance
            position={[0, y, 0]}
            rotation={[0, -diagonalAngle, 0]}
            scale={[LINE_WIDTH * 1.5, PAINT_HEIGHT, diagonal]}
            color={color}
          />
        </>
      ) : null}
    </group>
  )
}

function ReservedMarker({
  bay,
  palette,
}: {
  bay: SceneBay
  palette: ScenePalette
}) {
  const { length } = bay.placement
  const z = length / 2 + 0.15
  const signY = POST_HEIGHT + 0.22
  const isAssigned = bay.style === 'reservedAssigned'
  const signColor = isAssigned
    ? palette.reservedAssigned
    : palette.reservedUnassigned

  return (
    <group {...bayTransform(bay)}>
      <Instance
        position={[0, POST_HEIGHT / 2, z]}
        scale={[0.07, POST_HEIGHT, 0.07]}
        color={palette.marker}
      />
      {isAssigned ? (
        <Instance
          position={[0, signY, z]}
          scale={[0.6, 0.42, 0.05]}
          color={signColor}
        />
      ) : (
        <>
          <Instance
            position={[0, signY + 0.19, z]}
            scale={[0.6, 0.06, 0.05]}
            color={signColor}
          />
          <Instance
            position={[0, signY - 0.19, z]}
            scale={[0.6, 0.06, 0.05]}
            color={signColor}
          />
          <Instance
            position={[-0.27, signY, z]}
            scale={[0.06, 0.42, 0.05]}
            color={signColor}
          />
          <Instance
            position={[0.27, signY, z]}
            scale={[0.06, 0.42, 0.05]}
            color={signColor}
          />
        </>
      )}
    </group>
  )
}

const styleLabel: Record<SceneBay['style'], string> = {
  general: 'General',
  reservedAssigned: 'Reserved',
  reservedUnassigned: 'Reserved · unassigned',
  inactive: 'Inactive',
}

function BayTooltip({ bay }: { bay: SceneBay }) {
  return (
    <Html
      position={[
        bay.placement.x,
        bay.placement.level * LEVEL_HEIGHT + 2.2,
        -bay.placement.y,
      ]}
      center
      zIndexRange={[20, 11]}
      style={{ pointerEvents: 'none' }}
    >
      <div className="min-w-32 rounded-md border bg-popover px-2.5 py-1.5 text-xs text-popover-foreground shadow-md">
        <p className="font-semibold">{bay.label}</p>
        <p className="text-muted-foreground">{styleLabel[bay.style]}</p>
        {bay.assignee ? <p>Driver: {bay.assignee}</p> : null}
        {bay.isPreview ? (
          <p className="text-muted-foreground">Unsaved position</p>
        ) : null}
      </div>
    </Html>
  )
}
