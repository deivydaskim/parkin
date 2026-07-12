import { z } from 'zod'
import { roleSchema } from '@/features/auth/schemas'

export type { Role } from '@/features/auth/schemas'

export const userStatusSchema = z.enum(['Active', 'Disabled'])
export type UserStatus = z.infer<typeof userStatusSchema>

// Full resource, as returned by the API.
export const userSchema = z.object({
  id: z.uuid(),
  email: z.string(),
  displayName: z.string(),
  role: roleSchema,
  status: userStatusSchema,
})

export type User = z.infer<typeof userSchema>

export const createUserSchema = z.object({
  email: z.email('A valid email is required'),
  password: z.string().min(1, 'Password is required'),
  displayName: z
    .string()
    .min(1, 'Display name is required')
    .max(200, 'Display name must not exceed 200 characters'),
  role: roleSchema,
})

export type CreateUserInput = z.infer<typeof createUserSchema>

export const changeRoleSchema = z.object({
  role: roleSchema,
})

export type ChangeRoleInput = z.infer<typeof changeRoleSchema>

export const userListParamsSchema = z.object({
  page: z.number().int().min(1).optional(),
  perPage: z.number().int().min(1).max(100).optional(),
})

export type UserListParams = z.infer<typeof userListParamsSchema>

export const userListResponseSchema = z.object({
  items: z.array(userSchema),
  page: z.number(),
  perPage: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})

export type UserListResponse = z.infer<typeof userListResponseSchema>
