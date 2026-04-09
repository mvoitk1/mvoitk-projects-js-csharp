<template>
  <div class="auth-container">
    <h2>Register</h2>
    <form @submit.prevent="handleRegister">
      <div class="field">
        <label>First Name</label>
        <input v-model="firstName" type="text" required />
      </div>
      <div class="field">
        <label>Last Name</label>
        <input v-model="lastName" type="text" required />
      </div>
      <div class="field">
        <label>Email</label>
        <input v-model="email" type="email" required />
      </div>
      <div class="field">
        <label>Password</label>
        <input v-model="password" type="password" required />
      </div>
      <p v-if="error" class="error">{{ error }}</p>
      <button type="submit">Register</button>
    </form>
    <p>Already have an account? <RouterLink to="/login">Login</RouterLink></p>
  </div>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

const firstName = ref('')
const lastName = ref('')
const email = ref('')
const password = ref('')
const error = ref<string | null>(null)

async function handleRegister() {
  error.value = null
  try {
    await auth.register({ firstName: firstName.value, lastName: lastName.value, email: email.value, password: password.value })
    router.push('/dashboard')
  } catch (e: unknown) {
    const axiosErr = e as { response?: { data?: unknown } }
    if (axiosErr?.response?.data) {
      const data = axiosErr.response.data
      error.value = typeof data === 'string' ? data : JSON.stringify(data)
    } else {
      error.value = e instanceof Error ? e.message : 'Registration failed'
    }
  }
}
</script>
