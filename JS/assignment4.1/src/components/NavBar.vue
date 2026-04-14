<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <nav>
    <RouterLink to="/dashboard">Dashboard</RouterLink>
    <RouterLink to="/categories">Categories</RouterLink>
    <RouterLink to="/priorities">Priorities</RouterLink>
    <span v-if="auth.currentUser">{{ auth.currentUser }}</span>
    <button @click="logout">Logout</button>
  </nav>
</template>

<style scoped>
nav {
  display: flex;
  gap: 1rem;
  align-items: center;
  padding: 0.75rem 1.5rem;
  background: #1a1a2e;
  color: #e0e0e0;
}
nav a {
  color: #90caf9;
  text-decoration: none;
}
nav a:hover {
  text-decoration: underline;
}
nav span {
  margin-left: auto;
}
nav button {
  cursor: pointer;
  padding: 0.25rem 0.75rem;
  background: #e53935;
  color: white;
  border: none;
  border-radius: 4px;
}
</style>
