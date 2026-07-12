import { z } from 'zod'

export const roleSchema = z.enum(['SystemAdmin', 'Operator'])
export type Role = z.infer<typeof roleSchema>

export const loginSchema = z.object({
  email: z.email('A valid email is required'),
  password: z.string().min(1, 'Password is required'),
})

export type LoginInput = z.infer<typeof loginSchema>

export const currentUserSchema = z.object({
  id: z.string(),
  email: z.string(),
  displayName: z.string(),
  roles: z.array(roleSchema),
})

export type CurrentUser = z.infer<typeof currentUserSchema>
