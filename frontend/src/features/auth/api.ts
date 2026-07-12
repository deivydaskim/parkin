import { apiClient } from '@/lib/api-client'
import { currentUserSchema, type CurrentUser, type LoginInput } from './schemas'

export async function login(body: LoginInput): Promise<CurrentUser> {
  const { data } = await apiClient.post('/auth/login', body)
  return currentUserSchema.parse(data)
}

export async function fetchCurrentUser(): Promise<CurrentUser> {
  const { data } = await apiClient.get('/auth/me')
  return currentUserSchema.parse(data)
}

export async function logout(): Promise<void> {
  await apiClient.post('/auth/logout')
}
