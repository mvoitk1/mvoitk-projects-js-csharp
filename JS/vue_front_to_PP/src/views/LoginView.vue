<script setup lang="ts">
import { ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import { useCartStore } from '@/stores/cart'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()
const cart = useCartStore()
const { t } = useLocale()
const { show } = useToast()

const email = ref('')
const password = ref('')
const loading = ref(false)
const error = ref('')

async function submit() {
  error.value = ''
  loading.value = true
  try {
    await auth.login({ email: email.value, password: password.value })
    try { await cart.fetchCart() } catch { /* cart unavailable for some roles */ }
    const redirect = (route.query.redirect as string) || '/'
    router.push(redirect)
  } catch (e) {
    error.value = (e as Error).message
    show(error.value, 'error')
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-page">
    <div class="auth-card">
      <h1>{{ t('Sign In', 'Logi sisse') }}</h1>

      <form @submit.prevent="submit">
        <div class="form-group">
          <label for="email">{{ t('Email', 'E-post') }}</label>
          <input id="email" v-model="email" type="email" required autocomplete="email" />
        </div>

        <div class="form-group">
          <label for="password">{{ t('Password', 'Parool') }}</label>
          <input
            id="password"
            v-model="password"
            type="password"
            required
            autocomplete="current-password"
          />
        </div>

        <p v-if="error" class="error">{{ error }}</p>

        <button type="submit" class="btn-submit" :disabled="loading">
          <template v-if="loading">{{ t('Signing in…', 'Sisselogimine…') }}</template>
          <template v-else>{{ t('Sign In', 'Logi sisse') }}</template>
        </button>
      </form>

      <p class="auth-link">
        {{ t("Don't have an account?", 'Pole kontot?') }}
        <RouterLink :to="{ name: 'register' }">{{ t('Register', 'Registreeru') }}</RouterLink>
      </p>
    </div>
  </div>
</template>

<style scoped>
.auth-page {
  display: flex;
  justify-content: center;
  align-items: flex-start;
  padding-top: 3rem;
}

.auth-card {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 10px;
  padding: 2.5rem;
  width: 100%;
  max-width: 400px;
}

.auth-card h1 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 0.3rem;
}

.form-group input {
  width: 100%;
  padding: 0.55rem 0.75rem;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 0.95rem;
}

.form-group input:focus {
  outline: none;
  border-color: #1a1a1a;
}

.error {
  color: #c0392b;
  font-size: 0.85rem;
  margin-bottom: 0.75rem;
}

.btn-submit {
  width: 100%;
  background: #1a1a1a;
  color: #fff;
  border: none;
  padding: 0.75rem;
  border-radius: 5px;
  font-size: 1rem;
  font-weight: 600;
  margin-top: 0.5rem;
}

.btn-submit:hover:not(:disabled) {
  background: #333;
}

.btn-submit:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.auth-link {
  text-align: center;
  font-size: 0.85rem;
  color: #666;
  margin-top: 1.25rem;
}

.auth-link a {
  color: #1a1a1a;
  font-weight: 600;
}
</style>
