import { z } from 'zod'

export const loginSchema = z.object({
  email: z.email('A valid email is required'),
  password: z.string().min(1, 'Password is required'),
})

export type LoginInput = z.infer<typeof loginSchema>

export const currentUserSchema = z.object({
  id: z.string(),
  email: z.string(),
  displayName: z.string(),
  roles: z.array(z.string()),
})

export type CurrentUser = z.infer<typeof currentUserSchema>
