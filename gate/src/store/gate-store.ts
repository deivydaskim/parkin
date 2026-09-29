import { create } from 'zustand'
import {
  createJSONStorage,
  persist,
  type StateStorage,
} from 'zustand/middleware'
import type { DisplayMessage, FailSafeMode } from '@/lib/decision-messages'
import type {
  AccessEventRequest,
  RecordedRequest,
  RecordedResponse,
} from '@/lib/gate-api'
import type { AccessEventDecision, Direction, Pool } from '@/lib/schemas'

export const MAX_LOG_ENTRIES = 100

export type LanePhase =
  | 'idle'
  | 'approaching'
  | 'scanning'
  | 'deciding'
  | 'opening'
  | 'passing'
  | 'closing'
  | 'denied'
  | 'reversing'

export type LaneState = {
  phase: LanePhase
  plate: string | null
  display: DisplayMessage | null
}

export type VehicleInside = {
  plate: string
  pool: Pool | null
  reservedSpaceLabel: string | null
  enteredAt: string
}

export type LogEntry = {
  id: string
  at: string
  lane: Direction
  plate: string
  idempotencyKey: string
  request: RecordedRequest
  response: RecordedResponse | null
  decision: AccessEventDecision | null
  error: string | null
  latencyMs: number
  replayOf: string | null
  replayIdentical: boolean | null
}

export type LastEvent = {
  lane: Direction
  request: AccessEventRequest
  entryId: string
  decision: AccessEventDecision | null
}

export type VehicleEvent = {
  direction: Direction
  plate: string
  decision: AccessEventDecision
}

const IDLE_LANE: LaneState = { phase: 'idle', plate: null, display: null }

export function reduceVehiclesInside(
  vehicles: VehicleInside[],
  event: VehicleEvent,
): VehicleInside[] {
  if (event.decision.decision !== 'Allow') return vehicles

  if (event.direction === 'Enter') {
    return [
      ...vehicles,
      {
        plate: event.plate,
        pool: event.decision.pool,
        reservedSpaceLabel: event.decision.reservedSpaceLabel,
        enteredAt: event.decision.occurredAt,
      },
    ]
  }

  const index = vehicles.findLastIndex(
    (vehicle) => vehicle.plate === event.plate,
  )
  if (index === -1) return vehicles
  return vehicles.filter((_, position) => position !== index)
}

export function isInside(vehicles: VehicleInside[], plate: string) {
  return vehicles.some((vehicle) => vehicle.plate === plate)
}

type GateState = {
  lanes: Record<Direction, LaneState>
  vehiclesInside: VehicleInside[]
  log: LogEntry[]
  lastEvent: LastEvent | null
  failSafe: FailSafeMode
  autoTraffic: boolean
  patchLane: (lane: Direction, patch: Partial<LaneState>) => void
  resetLane: (lane: Direction) => void
  applyVehicleEvent: (event: VehicleEvent) => void
  addLogEntry: (entry: LogEntry) => void
  clearLog: () => void
  setLastEvent: (event: LastEvent) => void
  setFailSafe: (mode: FailSafeMode) => void
  setAutoTraffic: (enabled: boolean) => void
  clearVehiclesInside: () => void
}

const safeStorage: StateStorage = {
  getItem: (name) => {
    try {
      return localStorage.getItem(name)
    } catch {
      return null
    }
  },
  setItem: (name, value) => {
    try {
      localStorage.setItem(name, value)
    } catch {
      return
    }
  },
  removeItem: (name) => {
    try {
      localStorage.removeItem(name)
    } catch {
      return
    }
  },
}

export const useGateStore = create<GateState>()(
  persist(
    (set) => ({
      lanes: { Enter: IDLE_LANE, Exit: IDLE_LANE },
      vehiclesInside: [],
      log: [],
      lastEvent: null,
      failSafe: 'closed',
      autoTraffic: false,
      patchLane: (lane, patch) =>
        set((state) => ({
          lanes: { ...state.lanes, [lane]: { ...state.lanes[lane], ...patch } },
        })),
      resetLane: (lane) =>
        set((state) => ({ lanes: { ...state.lanes, [lane]: IDLE_LANE } })),
      applyVehicleEvent: (event) =>
        set((state) => ({
          vehiclesInside: reduceVehiclesInside(state.vehiclesInside, event),
        })),
      addLogEntry: (entry) =>
        set((state) => ({
          log: [entry, ...state.log].slice(0, MAX_LOG_ENTRIES),
        })),
      clearLog: () => set({ log: [], lastEvent: null }),
      setLastEvent: (lastEvent) => set({ lastEvent }),
      setFailSafe: (failSafe) => set({ failSafe }),
      setAutoTraffic: (autoTraffic) => set({ autoTraffic }),
      clearVehiclesInside: () => set({ vehiclesInside: [] }),
    }),
    {
      name: 'parkin-gate-simulator',
      version: 1,
      storage: createJSONStorage(() => safeStorage),
      partialize: (state) => ({
        vehiclesInside: state.vehiclesInside,
        log: state.log,
        failSafe: state.failSafe,
      }),
    },
  ),
)
