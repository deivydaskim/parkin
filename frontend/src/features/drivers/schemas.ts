import { z } from 'zod'

export const driverStatusSchema = z.enum(['Active', 'Archived', 'Anonymized'])
export type DriverStatus = z.infer<typeof driverStatusSchema>
export const DriverStatus = driverStatusSchema.enum

// Full resource, as returned by the API.
export const driverSchema = z.object({
  id: z.uuid(),
  name: z.string(),
  contact: z.string().nullable(),
  status: driverStatusSchema,
  plateCount: z.number().int().nonnegative(),
})

export type Driver = z.infer<typeof driverSchema>

export const driverFormSchema = z.object({
  name: z
    .string()
    .min(1, 'Name is required')
    .max(200, 'Name must not exceed 200 characters'),
  contact: z
    .string()
    .max(300, 'Contact must not exceed 300 characters')
    .optional(),
})

export type DriverFormInput = z.infer<typeof driverFormSchema>

export const driverListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
  status: driverStatusSchema.or(z.literal('All')).optional(),
})

export type DriverListParams = z.infer<typeof driverListParamsSchema>

export const driverListResponseSchema = z.object({
  items: z.array(driverSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type DriverListResponse = z.infer<typeof driverListResponseSchema>

export const plateStatusSchema = z.enum(['Active', 'Inactive'])
export type PlateStatus = z.infer<typeof plateStatusSchema>
export const PlateStatus = plateStatusSchema.enum

// Full resource, as returned by the API.
export const plateSchema = z.object({
  id: z.uuid(),
  driverId: z.uuid(),
  normalizedPlateNumber: z.string(),
  status: plateStatusSchema,
})

export type Plate = z.infer<typeof plateSchema>

export const plateFormSchema = z.object({
  plateNumber: z
    .string()
    .min(1, 'Plate number is required')
    .max(20, 'Plate number must not exceed 20 characters'),
})

export type PlateFormInput = z.infer<typeof plateFormSchema>

export const plateListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
})

export type PlateListParams = z.infer<typeof plateListParamsSchema>

export const plateListResponseSchema = z.object({
  items: z.array(plateSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type PlateListResponse = z.infer<typeof plateListResponseSchema>
