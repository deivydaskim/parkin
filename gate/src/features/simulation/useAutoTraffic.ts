import { useEffect, useEffectEvent } from 'react'
import { env } from '@/config/env'
import { generateVisitorPlate, normalizePlate } from '@/lib/plates'
import { isInside, useGateStore } from '@/store/gate-store'

const MIN_DELAY_MS = 2500
const MAX_DELAY_MS = 5500
const EXIT_CHANCE = 0.45
const DEMO_PLATE_CHANCE = 0.3
const UNKNOWN_EXIT_CHANCE = 0.08

type Runner = (plate: string) => Promise<boolean>

type Props = {
  enter: Runner
  exit: Runner
}

function pickRandom<T>(items: T[]) {
  return items[Math.floor(Math.random() * items.length)]
}

export function useAutoTraffic({ enter, exit }: Props) {
  const enabled = useGateStore((state) => state.autoTraffic)

  const tick = useEffectEvent(() => {
    const { vehiclesInside } = useGateStore.getState()

    if (Math.random() < UNKNOWN_EXIT_CHANCE) {
      void exit(generateVisitorPlate())
      return
    }

    if (vehiclesInside.length > 0 && Math.random() < EXIT_CHANCE) {
      void exit(pickRandom(vehiclesInside).plate)
      return
    }

    const freeDemoPlates = env.demoPlates
      .map((demo) => normalizePlate(demo.plate))
      .filter((plate) => !isInside(vehiclesInside, plate))

    if (freeDemoPlates.length > 0 && Math.random() < DEMO_PLATE_CHANCE) {
      void enter(pickRandom(freeDemoPlates))
      return
    }

    void enter(generateVisitorPlate())
  })

  useEffect(() => {
    if (!enabled) return

    let timer: ReturnType<typeof setTimeout>
    const schedule = () => {
      const delay = MIN_DELAY_MS + Math.random() * (MAX_DELAY_MS - MIN_DELAY_MS)
      timer = setTimeout(() => {
        tick()
        schedule()
      }, delay)
    }
    schedule()

    return () => clearTimeout(timer)
  }, [enabled])
}
