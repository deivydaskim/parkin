import { apiClient } from '@/lib/api-client'
import {
  userListResponseSchema,
  userSchema,
  type ChangeRoleInput,
  type CreateUserInput,
  type User,
  type UserListParams,
  type UserListResponse,
} from './schemas'

export async function fetchUsers(
  params?: UserListParams,
): Promise<UserListResponse> {
  const { data } = await apiClient.get('/users', {
    params: {
      page: params?.page,
      per_page: params?.perPage,
    },
  })
  return userListResponseSchema.parse(data)
}

export async function createUser(input: CreateUserInput): Promise<User> {
  const { data } = await apiClient.post('/users', input)
  return userSchema.parse(data)
}

export async function disableUser(id: string): Promise<void> {
  await apiClient.post(`/users/${id}/disable`, {})
}

export async function enableUser(id: string): Promise<void> {
  await apiClient.post(`/users/${id}/enable`, {})
}

export async function changeUserRole(
  id: string,
  input: ChangeRoleInput,
): Promise<User> {
  const { data } = await apiClient.patch(`/users/${id}/role`, input)
  return userSchema.parse(data)
}
