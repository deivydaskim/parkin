export const qk = {
  auth: {
    me: () => ['auth', 'me'] as const,
  },
  lots: {
    list: (params?: Record<string, unknown>) =>
      ['lots', 'list', params ?? {}] as const,
    detail: (id: string) => ['lots', 'detail', id] as const,
  },
  users: {
    list: (params?: Record<string, unknown>) =>
      ['users', 'list', params ?? {}] as const,
  },
}
