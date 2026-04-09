<template>
  <nav v-if="auth.isAuthenticated">
    <RouterLink to="/dashboard">Dashboard</RouterLink>
    <RouterLink to="/categories">Category</RouterLink>
    <RouterLink to="/priorities">Priority</RouterLink>
    <ThemeToggle />
    <span class="user-info">{{ auth.currentUser }}</span>
    <button @click="handleLogout">Logout</button>
  </nav>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import ThemeToggle from './ThemeToggle.vue'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

function handleLogout() {
  auth.logout()
  router.push('/login')
}
</script>
