<template>
  <nav>
    <RouterLink to="/dashboard">Dashboard</RouterLink>
    <RouterLink to="/categories">Category</RouterLink>
    <RouterLink to="/priorities">Priority</RouterLink>
    <ThemeToggle />
    <span class="user-info">{{ auth.currentUser }}</span>
    <button @click="logout">Logout</button>
  </nav>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import ThemeToggle from '@/components/ThemeToggle.vue'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()

function logout() {
  auth.logout()
  router.push('/login')
}
</script>
