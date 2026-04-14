<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

const email = ref('')
const password = ref('')
const error = ref<string | null>(null)
const loading = ref(false)

async function submit() {
  error.value = null
  loading.value = true
  try {
    await auth.login({ email: email.value, password: password.value })
    router.push('/dashboard')
  } catch {
    error.value = 'Invalid email or password.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="form-container">
    <h2>Sign In</h2>
    <p v-if="error" class="error">{{ error }}</p>
    <form @submit.prevent="submit">
      <label>Email<input v-model="email" type="email" required /></label>
      <label>Password<input v-model="password" type="password" required /></label>
      <button type="submit" :disabled="loading">{{ loading ? 'Signing in…' : 'Sign In' }}</button>
    </form>
    <p>No account? <RouterLink to="/register">Register</RouterLink></p>
  </div>
</template>

<style scoped>
.form-container {
  max-width: 400px;
  margin: 4rem auto;
  padding: 2rem;
  border: 1px solid #333;
  border-radius: 8px;
}
h2 {
  margin-bottom: 1.5rem;
}
form {
  display: flex;
  flex-direction: column;
  gap: 1rem;
}
label {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}
input {
  padding: 0.5rem;
  border: 1px solid #555;
  border-radius: 4px;
  background: #222;
  color: inherit;
}
button {
  padding: 0.6rem;
  background: #1565c0;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
button:disabled {
  opacity: 0.6;
  cursor: default;
}
.error {
  color: #ef5350;
  margin-bottom: 0.5rem;
}
</style>
