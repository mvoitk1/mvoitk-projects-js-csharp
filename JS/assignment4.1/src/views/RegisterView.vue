<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const firstName = ref('')
const lastName = ref('')
const error = ref<string | null>(null)
const loading = ref(false)

async function submit() {
  error.value = null
  loading.value = true
  try {
    await auth.register({
      email: email.value,
      password: password.value,
      firstName: firstName.value,
      lastName: lastName.value,
    })
    router.push('/dashboard')
  } catch {
    error.value = 'Registration failed. The email may already be in use.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="auth-container">
    <h2>Register</h2>
    <form @submit.prevent="submit">
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
      <button type="submit" :disabled="loading">{{ loading ? 'Registering…' : 'Register' }}</button>
    </form>
    <p>Already have an account? <RouterLink to="/login">Login</RouterLink></p>
  </div>
</template>
