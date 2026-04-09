<template>
  <div class="auth-container">
    <h2>Login</h2>
    <form @submit.prevent="handleLogin">
      <div class="field">
        <label>Email</label>
        <input v-model="email" type="email" required />
      </div>
      <div class="field">
        <label>Password</label>
        <input v-model="password" type="password" required />
      </div>
      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit">Login</button>
    </form>
    <p>No account? <RouterLink to="/register">Register</RouterLink></p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)

async function handleLogin() {
  error.value = null
  try {
    await auth.login({ email: email.value, password: password.value })
    router.push('/todos')
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Login failed'
  }
}
</script>
