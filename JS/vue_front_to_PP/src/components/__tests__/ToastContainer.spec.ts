import { describe, it, expect, beforeEach } from 'vitest'
import { mount } from '@vue/test-utils'
import ToastContainer from '@/components/ToastContainer.vue'
import { useToast } from '@/composables/useToast'

beforeEach(() => {
  // Drain the module-level toast state between tests.
  const { toasts, dismiss } = useToast()
  ;[...toasts.value].forEach((t) => dismiss(t.id))
})

describe('ToastContainer', () => {
  it('renders a toast per entry with a type-specific class', async () => {
    const { show } = useToast()
    show('Saved', 'success', 99999)
    show('Failed', 'error', 99999)

    const wrapper = mount(ToastContainer)
    await wrapper.vm.$nextTick()

    const toasts = wrapper.findAll('.toast')
    expect(toasts).toHaveLength(2)
    expect(toasts[0].classes()).toContain('toast--success')
    expect(toasts[0].text()).toBe('Saved')
    expect(toasts[1].classes()).toContain('toast--error')
  })

  it('dismisses a toast when clicked', async () => {
    const { show } = useToast()
    show('Dismiss me', 'info', 99999)

    const wrapper = mount(ToastContainer)
    await wrapper.vm.$nextTick()
    expect(wrapper.findAll('.toast')).toHaveLength(1)

    await wrapper.find('.toast').trigger('click')
    expect(wrapper.findAll('.toast')).toHaveLength(0)
  })
})
