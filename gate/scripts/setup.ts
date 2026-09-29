import { existsSync, readFileSync, writeFileSync } from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { parseArgs, parseEnv } from 'node:util'

const GATE_DIR = path.resolve(
  path.dirname(fileURLToPath(import.meta.url)),
  '..',
)
const ENV_PATH = path.join(GATE_DIR, '.env.local')
const DEFAULT_LOT_NAME = 'Demo Parking - Central'
const API_KEY_NAME = 'Gate simulator'
const AUTH_COOKIE = 'parkin.auth'
const DEMO_DRIVERS = [
  { name: 'Ona Petraitė', plate: 'ONA-001' },
  { name: 'Jonas Kazlauskas', plate: 'JON-002' },
  { name: 'Rūta Jankauskienė', plate: 'RUT-003' },
]

type Page<T> = { items: T[] }
type Lot = { id: string; name: string }
type ApiKey = { id: string; name: string; prefix: string; status: string }
type CreatedApiKey = ApiKey & { key: string }
type Driver = { id: string; name: string; plateCount: number }
type Plate = { normalizedPlateNumber: string; status: string }
type DemoPlate = { plate: string; driverName: string }

class SetupError extends Error {}

function readExistingEnv() {
  if (!existsSync(ENV_PATH)) return {}
  return parseEnv(readFileSync(ENV_PATH, 'utf8')) as Record<string, string>
}

function log(message: string) {
  console.log(`[gate:setup] ${message}`)
}

function createClient(baseUrl: string) {
  let cookie = ''

  async function request<T>(method: string, route: string, body?: unknown) {
    const response = await fetch(`${baseUrl}${route}`, {
      method,
      headers: {
        ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
        ...(cookie ? { Cookie: cookie } : {}),
      },
      body: body === undefined ? undefined : JSON.stringify(body),
    }).catch(() => {
      throw new SetupError(
        `Cannot reach the Parkin API at ${baseUrl}. Start it first (dotnet run --project src/Parkin.AspireHost).`,
      )
    })

    if (!response.ok) {
      const detail = await response.text()
      throw new SetupError(
        `${method} ${route} failed with ${response.status}: ${detail}`,
      )
    }

    if (route === '/auth/login') {
      const setCookie = response.headers
        .getSetCookie()
        .find((value) => value.startsWith(`${AUTH_COOKIE}=`))
      if (!setCookie)
        throw new SetupError('Login did not return the auth cookie')
      cookie = setCookie.split(';')[0]
    }

    const text = await response.text()
    return (text ? JSON.parse(text) : null) as T
  }

  return {
    get: <T>(route: string) => request<T>('GET', route),
    post: <T>(route: string, body: unknown) => request<T>('POST', route, body),
  }
}

type Client = ReturnType<typeof createClient>

async function findLot(client: Client, name: string) {
  const page = await client.get<Page<Lot>>(
    `/lots?search=${encodeURIComponent(name)}&per_page=50`,
  )
  const lot = page.items.find(
    (item) => item.name.toLowerCase() === name.toLowerCase(),
  )
  if (!lot) {
    const found = page.items.map((item) => item.name).join(', ') || 'none'
    throw new SetupError(
      `Lot "${name}" not found (matches: ${found}). Use --lot "<name>".`,
    )
  }
  return lot
}

async function resolveApiKey(client: Client, existingKey: string | undefined) {
  const keys = await client.get<ApiKey[]>('/api-keys')
  const stillActive =
    existingKey &&
    keys.some(
      (key) => key.status === 'Active' && existingKey.startsWith(key.prefix),
    )

  if (existingKey && stillActive) {
    log('Existing API key is still active, keeping it')
    return existingKey
  }

  const created = await client.post<CreatedApiKey>('/api-keys', {
    name: API_KEY_NAME,
  })
  log(`Created API key "${API_KEY_NAME}" (${created.prefix}…)`)
  return created.key
}

async function ensureDemoPlate(client: Client, name: string, plate: string) {
  const page = await client.get<Page<Driver>>(
    `/drivers?search=${encodeURIComponent(name)}&status=All&per_page=50`,
  )
  const driver = page.items.find((item) => item.name === name)
  if (!driver) {
    log(`Driver "${name}" not found, skipping`)
    return null
  }

  if (driver.plateCount === 0) {
    await client.post(`/drivers/${driver.id}/plates`, { plateNumber: plate })
    log(`Added plate ${plate} to ${name}`)
    return { plate, driverName: name } satisfies DemoPlate
  }

  const plates = await client.get<Page<Plate>>(
    `/drivers/${driver.id}/plates?per_page=50`,
  )
  const active = plates.items.find((item) => item.status === 'Active')
  if (!active) {
    log(`${name} has no active plate, skipping`)
    return null
  }
  log(`${name} already has plate ${active.normalizedPlateNumber}`)
  return {
    plate: active.normalizedPlateNumber,
    driverName: name,
  } satisfies DemoPlate
}

async function ensureDemoPlates(client: Client) {
  const results: DemoPlate[] = []
  for (const driver of DEMO_DRIVERS) {
    try {
      const demo = await ensureDemoPlate(client, driver.name, driver.plate)
      if (demo) results.push(demo)
    } catch (error) {
      if (!(error instanceof SetupError)) throw error
      log(`Skipped ${driver.name}: ${error.message}`)
    }
  }
  return results
}

function writeEnvFile(values: Record<string, string>) {
  const content = Object.entries(values)
    .map(([key, value]) => `${key}=${value}`)
    .join('\n')
  writeFileSync(ENV_PATH, `${content}\n`, 'utf8')
}

async function main() {
  const { values } = parseArgs({ options: { lot: { type: 'string' } } })
  const existing = readExistingEnv()

  const apiUrl =
    process.env.PARKIN_API_URL ??
    existing.PARKIN_API_URL ??
    'http://localhost:5000'
  const email = process.env.PARKIN_ADMIN_EMAIL ?? 'admin@parkin.local'
  const password = process.env.PARKIN_ADMIN_PASSWORD ?? 'Admin!2345'
  const lotName = values.lot ?? DEFAULT_LOT_NAME

  const client = createClient(apiUrl)

  await client.post('/auth/login', { email, password })
  log(`Logged in as ${email}`)

  const lot = await findLot(client, lotName)
  log(`Using lot "${lot.name}" (${lot.id})`)

  const apiKey = await resolveApiKey(client, existing.GATE_API_KEY)
  const demoPlates = await ensureDemoPlates(client)

  writeEnvFile({
    PARKIN_API_URL: apiUrl,
    GATE_API_KEY: apiKey,
    VITE_GATE_LOT_ID: lot.id,
    VITE_GATE_LOT_NAME: lot.name,
    VITE_DEMO_PLATES: JSON.stringify(demoPlates),
  })
  log(`Wrote ${path.relative(process.cwd(), ENV_PATH) || '.env.local'}`)
}

main().catch((error: unknown) => {
  const message = error instanceof Error ? error.message : String(error)
  console.error(`[gate:setup] ${message}`)
  process.exit(1)
})
