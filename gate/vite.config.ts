import path from 'node:path'
import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const target = env.PARKIN_API_URL || 'http://localhost:5000'
  const apiKey = env.GATE_API_KEY

  const proxy = {
    '/api/v1': {
      target,
      changeOrigin: true,
      headers: apiKey ? { 'X-Api-Key': apiKey } : {},
    },
  }

  return {
    plugins: [react(), tailwindcss()],
    server: {
      port: 5174,
      strictPort: true,
      proxy,
    },
    preview: {
      port: 5174,
      strictPort: true,
      proxy,
    },
    resolve: {
      alias: {
        '@': path.resolve(import.meta.dirname, './src'),
      },
    },
  }
})
