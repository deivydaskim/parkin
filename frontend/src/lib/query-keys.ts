export const qk = {
  auth: {
    me: () => ['auth', 'me'] as const,
  },
  lots: {
    list: (params?: Record<string, unknown>) =>
      ['lots', 'list', params ?? {}] as const,
    detail: (id: string) => ['lots', 'detail', id] as const,
  },
  spaces: {
    list: (lotId: string, params?: Record<string, unknown>) =>
      ['spaces', 'list', lotId, params ?? {}] as const,
    detail: (id: string) => ['spaces', 'detail', id] as const,
  },
  users: {
    list: (params?: Record<string, unknown>) =>
      ['users', 'list', params ?? {}] as const,
  },
  drivers: {
    list: (params?: Record<string, unknown>) =>
      ['drivers', 'list', params ?? {}] as const,
    detail: (id: string) => ['drivers', 'detail', id] as const,
  },
  plates: {
    list: (driverId: string, params?: Record<string, unknown>) =>
      ['plates', 'list', driverId, params ?? {}] as const,
  },
  grants: {
    list: (driverId: string, params?: Record<string, unknown>) =>
      ['grants', 'list', driverId, params ?? {}] as const,
  },
  apiKeys: {
    list: () => ['apiKeys', 'list'] as const,
  },
  reservations: {
    active: (spaceId: string) => ['reservations', 'active', spaceId] as const,
  },
}
