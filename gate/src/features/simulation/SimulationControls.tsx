import { useState } from 'react'
import { RotateCw, ShieldAlert, Shuffle } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Switch } from '@/components/ui/switch'
import { resendLastEvent } from '@/features/lane/useLaneCycle'
import { useGateStore } from '@/store/gate-store'

export function SimulationControls() {
  const failSafe = useGateStore((state) => state.failSafe)
  const setFailSafe = useGateStore((state) => state.setFailSafe)
  const autoTraffic = useGateStore((state) => state.autoTraffic)
  const setAutoTraffic = useGateStore((state) => state.setAutoTraffic)
  const lastEvent = useGateStore((state) => state.lastEvent)
  const [resending, setResending] = useState(false)

  async function resend() {
    setResending(true)
    try {
      await resendLastEvent()
    } finally {
      setResending(false)
    }
  }

  return (
    <div className="flex flex-wrap items-center gap-x-6 gap-y-3">
      <label className="flex items-center gap-2 text-sm">
        <Switch checked={autoTraffic} onCheckedChange={setAutoTraffic} />
        <Shuffle className="size-4 text-muted-foreground" />
        Auto traffic
      </label>

      <label className="flex items-center gap-2 text-sm">
        <Switch
          checked={failSafe === 'open'}
          onCheckedChange={(checked) =>
            setFailSafe(checked ? 'open' : 'closed')
          }
        />
        <ShieldAlert className="size-4 text-muted-foreground" />
        Fail-safe when offline:
        <span className="font-medium">
          {failSafe === 'open' ? 'Fail-open' : 'Fail-closed'}
        </span>
      </label>

      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={resend}
        disabled={!lastEvent || resending}
        title="Sends the last request again with the same Idempotency-Key"
      >
        <RotateCw />
        Resend last event (same Idempotency-Key)
      </Button>
    </div>
  )
}
