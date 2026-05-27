import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
// Use vitest's defineConfig so the `test` key is typed correctly
import { defineConfig } from 'vitest/config'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      // Allows `import { ... } from '@/components/...'` everywhere
      '@': path.resolve(__dirname, './src'),
    },
  },
  test: {
    // Vitest runs inside Vite — no separate jest.config needed
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    include: ['src/**/*.test.{ts,tsx}'],
    passWithNoTests: true,
  },
})
