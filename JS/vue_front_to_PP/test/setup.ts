import { afterEach, beforeEach, vi } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'

// jsdom provides localStorage, CustomEvent and window.dispatchEvent natively,
// so we only need to guarantee a clean slate between tests.
beforeEach(() => {
  setActivePinia(createPinia())
  localStorage.clear()
})

afterEach(() => {
  vi.restoreAllMocks()
  vi.unstubAllGlobals()
})
