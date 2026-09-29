import { z } from 'zod'

const demoPlateSchema = z.object({
  plate: z.string().min(1),
  driverName: z.string().min(1),
})

export type DemoPlate = z.infer<typeof demoPlateSchema>

const jsonDemoPlates = z
  .string()
  .default('[]')
  .transform((value, ctx) => {
    try {
      return JSON.parse(value) as unknown
    } catch {
      ctx.addIssue({ code: 'custom', message: 'Must be valid JSON' })
      return z.NEVER
    }
  })
  .pipe(z.array(demoPlateSchema))

const envSchema = z.object({
  VITE_GATE_LOT_ID: z.uuid(),
  VITE_GATE_LOT_NAME: z.string().min(1).default('Parking lot'),
  VITE_DEMO_PLATES: jsonDemoPlates,
})

const parsed = envSchema.safeParse(import.meta.env)

export const envError = parsed.success
  ? null
  : parsed.error.issues.map(
      (issue) => `${issue.path.join('.')}: ${issue.message}`,
    )

export const env = parsed.success
  ? {
      lotId: parsed.data.VITE_GATE_LOT_ID,
      lotName: parsed.data.VITE_GATE_LOT_NAME,
      demoPlates: parsed.data.VITE_DEMO_PLATES,
    }
  : { lotId: '', lotName: '', demoPlates: [] as DemoPlate[] }
