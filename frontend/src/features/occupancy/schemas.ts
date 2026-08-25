import { z } from 'zod'

export const lotOccupancySchema = z.object({
  lotId: z.uuid(),
  generalCapacity: z.number().int(),
  generalUsed: z.number().int(),
  generalFree: z.number().int(),
  isGeneralPoolFull: z.boolean(),
  isOverCapacity: z.boolean(),
  reservedSpaceCount: z.number().int(),
  reservedOccupied: z.number().int(),
  asOf: z.iso.datetime({ offset: true }),
})

export type LotOccupancy = z.infer<typeof lotOccupancySchema>
