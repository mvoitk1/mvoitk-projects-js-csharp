import { fileURLToPath, URL } from 'node:url'
import { defineConfig, configDefaults } from 'vitest/config'
import vue from '@vitejs/plugin-vue'

// Self-contained test config: reuses the `@` alias from vite.config.ts but
// omits the dev-only vue-devtools plugin and proxy server settings.
export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    clearMocks: true,
    setupFiles: ['./test/setup.ts'],
    env: { VITE_API_BASE: 'http://test.local/api/v1' },
    exclude: [...configDefaults.exclude, 'e2e/**'],
    coverage: {
      provider: 'v8',
      include: ['src/api/**', 'src/stores/**', 'src/composables/**', 'src/router/**'],
    },
  },
})
