import { describe, it, expect, beforeEach } from 'vitest'
import router from '@/router'
import { useAuthStore } from '@/stores/auth'
import { adminJwt, validJwt } from '../../../test/helpers/jwt'

// The guard resolves the auth store lazily on the first navigation, so we set
// the token on the store instance rather than localStorage to be sure the
// already-created store reflects it.
beforeEach(async () => {
  await router.replace('/')
  await router.isReady()
})

function signInWith(jwt: string | null) {
  useAuthStore().jwt = jwt
}

describe('router guard — requiresAuth', () => {
  it('redirects an unauthenticated user to login with a redirect query', async () => {
    signInWith(null)
    await router.push('/cart')
    expect(router.currentRoute.value.name).toBe('login')
    expect(router.currentRoute.value.query.redirect).toBe('/cart')
  })

  it('allows an authenticated user through', async () => {
    signInWith(validJwt())
    await router.push('/orders')
    expect(router.currentRoute.value.name).toBe('orders')
  })
})

describe('router guard — requiresAdmin', () => {
  it('redirects a non-admin away from admin routes to home', async () => {
    signInWith(validJwt({ role: 'User' }))
    await router.push('/admin/products')
    expect(router.currentRoute.value.name).toBe('home')
  })

  it('lets an admin into admin routes', async () => {
    signInWith(adminJwt())
    await router.push('/admin/products')
    expect(router.currentRoute.value.name).toBe('admin-products')
  })
})

describe('router guard — public routes', () => {
  it('leaves public routes unaffected', async () => {
    signInWith(null)
    await router.push('/products')
    expect(router.currentRoute.value.name).toBe('products')
  })
})
