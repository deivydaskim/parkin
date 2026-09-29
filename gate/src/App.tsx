import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { env, envError } from '@/config/env'
import { Lane } from '@/features/lane/Lane'
import { useLaneCycle } from '@/features/lane/useLaneCycle'
import { ApiLog } from '@/features/log/ApiLog'
import { SimulationControls } from '@/features/simulation/SimulationControls'
import { useAutoTraffic } from '@/features/simulation/useAutoTraffic'

function SetupRequired({ issues }: { issues: string[] }) {
  return (
    <main className="mx-auto flex min-h-screen max-w-xl items-center p-6">
      <Card className="w-full">
        <CardHeader>
          <CardTitle>Gate simulator is not configured</CardTitle>
        </CardHeader>
        <CardContent className="flex flex-col gap-3 text-sm">
          <p>
            Start the Parkin API, then run{' '}
            <code className="rounded bg-muted px-1.5 py-0.5 font-mono">
              pnpm gate:setup
            </code>{' '}
            and restart the dev server.
          </p>
          <ul className="list-disc pl-5 text-muted-foreground">
            {issues.map((issue) => (
              <li key={issue}>{issue}</li>
            ))}
          </ul>
        </CardContent>
      </Card>
    </main>
  )
}

export default function App() {
  const entry = useLaneCycle('Enter')
  const exit = useLaneCycle('Exit')
  useAutoTraffic({ enter: entry.run, exit: exit.run })

  if (envError) return <SetupRequired issues={envError} />

  return (
    <main className="mx-auto flex min-h-screen max-w-6xl flex-col gap-6 p-6">
      <header className="flex flex-col gap-4">
        <div>
          <h1 className="text-2xl font-semibold tracking-tight">
            Gate Simulator — {env.lotName}
          </h1>
          <p className="text-sm text-muted-foreground">
            A plate-reader gate calling the real{' '}
            <code className="font-mono">POST /api/v1/access-events</code> with
            an API key and an Idempotency-Key.
          </p>
        </div>
        <SimulationControls />
      </header>

      <section className="grid gap-6 lg:grid-cols-2">
        <Lane direction="Enter" lane={entry.lane} onSend={entry.run} />
        <Lane direction="Exit" lane={exit.lane} onSend={exit.run} />
      </section>

      <ApiLog />
    </main>
  )
}
