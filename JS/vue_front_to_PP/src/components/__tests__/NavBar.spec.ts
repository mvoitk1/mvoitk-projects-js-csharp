import { describe, it, expect, vi } from 'vitest'
import { mount, RouterLinkStub } from '@vue/test-utils'
import NavBar from '@/components/NavBar.vue'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useLocaleStore } from '@/stores/locale'
import { adminJwt, validJwt } from '../../../test/helpers/jwt'
import type { CartDto } from '@/types'

const push = vi.fn()
vi.mock('vue-router', () => ({
  useRouter: () => ({ push }),
}))

vi.mock('@/api/auth', () => ({
  login: vi.fn(),
  register: vi.fn(),
  logout: vi.fn().mockResolvedValue(undefined),
  renewToken: vi.fn(),
}))

function mountNav() {
  return mount(NavBar, { global: { stubs: { RouterLink: RouterLinkStub } } })
}

/** Names of the routes the rendered RouterLinks point to. */
function linkNames(wrapper: ReturnType<typeof mountNav>): string[] {
  return wrapper
    .findAllComponents(RouterLinkStub)
    .map((l) => (l.props('to') as { name?: string }).name)
    .filter((n): n is string => !!n)
}

describe('NavBar — auth-dependent links', () => {
  it('shows Login and Register when logged out, not Orders/Cart', () => {
    const wrapper = mountNav()
    const names = linkNames(wrapper)
    expect(names).toContain('login')
    expect(names).toContain('register')
    expect(names).not.toContain('orders')
    expect(names).not.toContain('cart')
  })

  it('shows Cart and Orders when logged in, not Login', () => {
    useAuthStore().jwt = validJwt()
    const wrapper = mountNav()
    const names = linkNames(wrapper)
    expect(names).toContain('cart')
    expect(names).toContain('orders')
    expect(names).not.toContain('login')
  })

  it('shows the Admin link only for admins', () => {
    const wrapper = mountNav()
    expect(linkNames(wrapper)).not.toContain('admin-products')

    useAuthStore().jwt = adminJwt()
    const adminWrapper = mountNav()
    expect(linkNames(adminWrapper)).toContain('admin-products')
  })
})

describe('NavBar — cart badge', () => {
  it('shows the item count when the cart is non-empty', async () => {
    useAuthStore().jwt = validJwt()
    useCartStore().cart = { itemCount: 4 } as CartDto
    const wrapper = mountNav()
    await wrapper.vm.$nextTick()
    expect(wrapper.find('.badge').text()).toBe('4')
  })

  it('hides the badge when the cart is empty', async () => {
    useAuthStore().jwt = validJwt()
    const wrapper = mountNav()
    await wrapper.vm.$nextTick()
    expect(wrapper.find('.badge').exists()).toBe(false)
  })
})

describe('NavBar — language toggle', () => {
  it('toggles the locale when the toggle button is clicked', async () => {
    const locale = useLocaleStore()
    const wrapper = mountNav()
    expect(locale.locale).toBe('en')

    // The first ghost button is the language toggle.
    await wrapper.find('button.btn-ghost').trigger('click')
    expect(locale.locale).toBe('et')
  })
})

describe('NavBar — logout', () => {
  it('logs out, clears the cart, and redirects home', async () => {
    const auth = useAuthStore()
    auth.jwt = validJwt()
    const cart = useCartStore()
    cart.cart = { itemCount: 2 } as CartDto
    const wrapper = mountNav()

    const logoutBtn = wrapper.findAll('button.btn-ghost').at(-1)!
    await logoutBtn.trigger('click')
    await wrapper.vm.$nextTick()

    expect(auth.jwt).toBeNull()
    expect(cart.cart).toBeNull()
    expect(push).toHaveBeenCalledWith({ name: 'home' })
  })
})
