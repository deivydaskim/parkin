import { useCallback, useEffect, useRef } from 'react'
import { env } from '@/config/env'
import {
  describeDecision,
  describeOffline,
  type DisplayMessage,
} from '@/lib/decision-messages'
import {
  createAccessEventRequest,
  sendAccessEvent,
  type AccessEventExchange,
  type AccessEventRequest,
} from '@/lib/gate-api'
import { normalizePlate } from '@/lib/plates'
import type { AccessEventDecision, Direction } from '@/lib/schemas'
import { useGateStore, type LogEntry } from '@/store/gate-store'

const APPROACH_MS = 1000
const SCAN_MS = 1300
const BARRIER_MS = 700
const PASS_MS = 1400
const DENIED_HOLD_MS = 1800
const REVERSE_MS = 1200

const DETECTED: DisplayMessage = {
  text: 'Vehicle detected',
  subtext: null,
  tone: 'info',
}
const READING: DisplayMessage = {
  text: 'Reading plate',
  subtext: null,
  tone: 'info',
}

function sleep(ms: number) {
  return new Promise<void>((resolve) => setTimeout(resolve, ms))
}

function decisionsMatch(a: AccessEventDecision, b: AccessEventDecision) {
  return JSON.stringify(a) === JSON.stringify(b)
}

function buildLogEntry(
  lane: Direction,
  plate: string,
  request: AccessEventRequest,
  exchange: AccessEventExchange,
  replay: { of: string; identical: boolean } | null,
): LogEntry {
  return {
    id: crypto.randomUUID(),
    at: new Date().toISOString(),
    lane,
    plate,
    idempotencyKey: request.idempotencyKey,
    request: exchange.request,
    response: exchange.response,
    decision: exchange.decision,
    error: exchange.error,
    latencyMs: exchange.latencyMs,
    replayOf: replay?.of ?? null,
    replayIdentical: replay?.identical ?? null,
  }
}

export async function resendLastEvent() {
  const { lastEvent, addLogEntry } = useGateStore.getState()
  if (!lastEvent) return

  const exchange = await sendAccessEvent(lastEvent.request)
  const identical =
    exchange.decision !== null &&
    lastEvent.decision !== null &&
    decisionsMatch(exchange.decision, lastEvent.decision)

  addLogEntry(
    buildLogEntry(
      lastEvent.lane,
      lastEvent.request.body.plate,
      lastEvent.request,
      exchange,
      { of: lastEvent.entryId, identical },
    ),
  )
}

export function useLaneCycle(direction: Direction) {
  const lane = useGateStore((state) => state.lanes[direction])
  const busyRef = useRef(false)
  const mountedRef = useRef(true)

  useEffect(() => {
    mountedRef.current = true
    return () => {
      mountedRef.current = false
    }
  }, [])

  const run = useCallback(
    async (rawPlate: string) => {
      const plate = normalizePlate(rawPlate)
      if (!plate || busyRef.current) return false

      const store = useGateStore.getState()
      const wait = async (ms: number) => {
        await sleep(ms)
        return mountedRef.current
      }

      busyRef.current = true
      try {
        store.patchLane(direction, {
          phase: 'approaching',
          plate,
          display: DETECTED,
        })
        if (!(await wait(APPROACH_MS))) return false

        store.patchLane(direction, { phase: 'scanning', display: READING })
        if (!(await wait(SCAN_MS))) return false

        store.patchLane(direction, { phase: 'deciding' })
        const request = createAccessEventRequest(env.lotId, plate, direction)
        const exchange = await sendAccessEvent(request)
        if (!mountedRef.current) return false

        const entry = buildLogEntry(direction, plate, request, exchange, null)
        const current = useGateStore.getState()
        current.addLogEntry(entry)
        current.setLastEvent({
          lane: direction,
          request,
          entryId: entry.id,
          decision: exchange.decision,
        })

        let display: DisplayMessage
        let barrierOpens: boolean
        if (exchange.decision) {
          display = describeDecision(exchange.decision, direction)
          barrierOpens = exchange.decision.decision === 'Allow'
          current.applyVehicleEvent({
            direction,
            plate,
            decision: exchange.decision,
          })
        } else {
          display = describeOffline(current.failSafe)
          barrierOpens = current.failSafe === 'open'
        }

        if (barrierOpens) {
          current.patchLane(direction, { phase: 'opening', display })
          if (!(await wait(BARRIER_MS))) return false
          current.patchLane(direction, { phase: 'passing' })
          if (!(await wait(PASS_MS))) return false
          current.patchLane(direction, { phase: 'closing' })
          if (!(await wait(BARRIER_MS))) return false
        } else {
          current.patchLane(direction, { phase: 'denied', display })
          if (!(await wait(DENIED_HOLD_MS))) return false
          current.patchLane(direction, { phase: 'reversing' })
          if (!(await wait(REVERSE_MS))) return false
        }

        return true
      } finally {
        busyRef.current = false
        if (mountedRef.current) useGateStore.getState().resetLane(direction)
      }
    },
    [direction],
  )

  return { lane, run }
}
