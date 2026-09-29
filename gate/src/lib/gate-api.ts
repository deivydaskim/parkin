import {
  accessEventDecisionSchema,
  type AccessEventDecision,
  type Direction,
} from '@/lib/schemas'

export const ACCESS_EVENTS_PATH = '/api/v1/access-events'
export const MASKED_API_KEY = 'pk_live_••••••••••••'
const REQUEST_TIMEOUT_MS = 5000

export type AccessEventBody = {
  lotId: string
  plate: string
  direction: Direction
  occurredAt: string
  source: 'Lpr'
}

export type RecordedRequest = {
  method: 'POST'
  path: string
  headers: Record<string, string>
  body: AccessEventBody
}

export type RecordedResponse = {
  status: number
  body: unknown
}

export type AccessEventExchange = {
  request: RecordedRequest
  response: RecordedResponse | null
  decision: AccessEventDecision | null
  latencyMs: number
  error: string | null
}

export type AccessEventRequest = {
  body: AccessEventBody
  idempotencyKey: string
}

export function createAccessEventRequest(
  lotId: string,
  plate: string,
  direction: Direction,
): AccessEventRequest {
  return {
    idempotencyKey: `gate-sim-${crypto.randomUUID()}`,
    body: {
      lotId,
      plate,
      direction,
      occurredAt: new Date().toISOString(),
      source: 'Lpr',
    },
  }
}

async function readBody(response: Response): Promise<unknown> {
  const text = await response.text()
  if (!text) return null
  try {
    return JSON.parse(text) as unknown
  } catch {
    return text
  }
}

export async function sendAccessEvent({
  body,
  idempotencyKey,
}: AccessEventRequest): Promise<AccessEventExchange> {
  const request: RecordedRequest = {
    method: 'POST',
    path: ACCESS_EVENTS_PATH,
    headers: {
      'Content-Type': 'application/json',
      'Idempotency-Key': idempotencyKey,
      'X-Api-Key': MASKED_API_KEY,
    },
    body,
  }

  const startedAt = performance.now()
  const elapsed = () => Math.round(performance.now() - startedAt)

  try {
    const response = await fetch(ACCESS_EVENTS_PATH, {
      method: request.method,
      headers: {
        'Content-Type': 'application/json',
        'Idempotency-Key': idempotencyKey,
      },
      body: JSON.stringify(body),
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    })
    const payload = await readBody(response)
    const latencyMs = elapsed()
    const recorded = { status: response.status, body: payload }

    if (!response.ok) {
      return {
        request,
        response: recorded,
        decision: null,
        latencyMs,
        error: `HTTP ${response.status}`,
      }
    }

    const parsed = accessEventDecisionSchema.safeParse(payload)
    if (!parsed.success) {
      return {
        request,
        response: recorded,
        decision: null,
        latencyMs,
        error: 'Unexpected response shape',
      }
    }

    return {
      request,
      response: recorded,
      decision: parsed.data,
      latencyMs,
      error: null,
    }
  } catch (error) {
    const timedOut =
      error instanceof DOMException && error.name === 'TimeoutError'
    return {
      request,
      response: null,
      decision: null,
      latencyMs: elapsed(),
      error: timedOut ? 'Request timed out' : 'Network error',
    }
  }
}
