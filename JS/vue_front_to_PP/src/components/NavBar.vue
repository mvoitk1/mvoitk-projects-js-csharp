<script setup lang="ts">
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useLocaleStore } from '@/stores/locale'
import { useRouter } from 'vue-router'
import { useToast } from '@/composables/useToast'

const auth = useAuthStore()
const cart = useCartStore()
const localeStore = useLocaleStore()
const router = useRouter()
const { show } = useToast()

async function handleLogout() {
  await auth.logout()
  cart.clear()
  show('Logged out', 'info')
  router.push({ name: 'home' })
}
</script>

<template>
  <header class="navbar">
    <div class="navbar__inner">
      <RouterLink class="navbar__brand" :to="{ name: 'home' }">BrandStore</RouterLink>

      <nav class="navbar__links">
        <RouterLink :to="{ name: 'home' }">Home</RouterLink>
        <RouterLink :to="{ name: 'products' }">Products</RouterLink>
        <RouterLink v-if="auth.isAdmin" :to="{ name: 'admin-products' }">Admin</RouterLink>
      </nav>

      <div class="navbar__actions">
        <button class="btn-ghost" @click="localeStore.toggle">
          {{ localeStore.locale === 'en' ? 'ET' : 'EN' }}
        </button>

        <RouterLink v-if="auth.isLoggedIn" :to="{ name: 'cart' }" class="cart-link">
          Cart
          <span v-if="cart.itemCount > 0" class="badge">{{ cart.itemCount }}</span>
        </RouterLink>

        <template v-if="auth.isLoggedIn">
          <RouterLink :to="{ name: 'orders' }">Orders</RouterLink>
          <button class="btn-ghost" @click="handleLogout">Logout</button>
        </template>
        <template v-else>
          <RouterLink :to="{ name: 'login' }">Login</RouterLink>
          <RouterLink :to="{ name: 'register' }" class="btn-primary">Register</RouterLink>
        </template>
      </div>
    </div>
  </header>
</template>

<style scoped>
.navbar {
  background: #fff;
  border-bottom: 1px solid #e8e8e4;
  position: sticky;
  top: 0;
  z-index: 100;
}

.navbar__inner {
  max-width: 1200px;
  margin: 0 auto;
  padding: 0 1rem;
  height: 60px;
  display: flex;
  align-items: center;
  gap: 2rem;
}

.navbar__brand {
  font-size: 1.25rem;
  font-weight: 700;
  letter-spacing: -0.02em;
}

.navbar__links {
  display: flex;
  gap: 1.5rem;
  flex: 1;
}

.navbar__links a,
.navbar__actions a {
  font-size: 0.9rem;
  color: #555;
  transition: color 0.15s;
}

.navbar__links a:hover,
.navbar__actions a:hover,
.navbar__links a.router-link-active,
.navbar__actions a.router-link-active {
  color: #1a1a1a;
}

.navbar__actions {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.cart-link {
  position: relative;
}

.badge {
  position: absolute;
  top: -6px;
  right: -10px;
  background: #c0392b;
  color: #fff;
  border-radius: 50%;
  font-size: 0.65rem;
  width: 16px;
  height: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.btn-ghost {
  background: none;
  border: 1px solid #ddd;
  border-radius: 4px;
  padding: 0.25rem 0.6rem;
  font-size: 0.85rem;
  color: #555;
}

.btn-ghost:hover {
  border-color: #aaa;
  color: #1a1a1a;
}

.btn-primary {
  background: #1a1a1a;
  color: #fff !important;
  padding: 0.35rem 0.9rem;
  border-radius: 4px;
  font-size: 0.85rem;
}

.btn-primary:hover {
  background: #333;
}
</style>
