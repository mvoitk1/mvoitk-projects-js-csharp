import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest'
import { useToast } from '@/composables/useToast'

describe('useToast', () => {
  beforeEach(() => {
    // The toasts ref is module-level state shared across calls — drain it.
    const { toasts, dismiss } = useToast()
    ;[...toasts.value].forEach((t) => dismiss(t.id))
    vi.useFakeTimers()
  })
  afterEach(() => vi.useRealTimers())

  it('show pushes a toast with incrementing id and the given type', () => {
    const { toasts, show } = useToast()
    show('first', 'success')
    show('second', 'error')

    expect(toasts.value).toHaveLength(2)
    expect(toasts.value[0]).toMatchObject({ message: 'first', type: 'success' })
    expect(toasts.value[1]).toMatchObject({ message: 'second', type: 'error' })
    expect(toasts.value[1].id).toBeGreaterThan(toasts.value[0].id)
  })

  it('defaults the type to info', () => {
    const { toasts, show } = useToast()
    show('hi')
    expect(toasts.value[0].type).toBe('info')
  })

  it('dismiss removes the toast by id', () => {
    const { toasts, show, dismiss } = useToast()
    show('a')
    show('b')
    const removedId = toasts.value[0].id
    dismiss(removedId)
    expect(toasts.value.find((t) => t.id === removedId)).toBeUndefined()
    expect(toasts.value).toHaveLength(1)
  })

  it('auto-dismisses after the duration elapses', () => {
    const { toasts, show } = useToast()
    show('temp', 'info', 1000)
    expect(toasts.value).toHaveLength(1)

    vi.advanceTimersByTime(1000)
    expect(toasts.value).toHaveLength(0)
  })
})
