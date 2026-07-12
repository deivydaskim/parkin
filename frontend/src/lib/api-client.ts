import axios from 'axios'
import { env } from '@/config/env'

export const apiClient = axios.create({
  baseURL: env.VITE_API_BASE_URL,
  withCredentials: true,
})

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    // Let the auth endpoints surface their own 401s (login error, /me guard);
    // only hard-redirect when a normal API call finds the session gone.
    const url = error.config?.url ?? ''
    const isAuthEndpoint = url.startsWith('/auth/')
    if (error.response?.status === 401 && !isAuthEndpoint) {
      window.location.assign('/login')
    }
    return Promise.reject(error)
  },
)
