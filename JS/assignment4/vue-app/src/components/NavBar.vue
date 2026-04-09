<template>
  <nav v-if="auth.isAuthenticated">
    <RouterLink to="/todos">Todos</RouterLink>
    <RouterLink to="/categories">Categories</RouterLink>
    <span class="user-info">{{ auth.currentUser }}</span>
    <button @click="handleLogout">Logout</button>
  </nav>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router'
import { useAuthStore } from '../stores/auth'

const router = useRouter()
const auth = useAuthStore()

function handleLogout() {
  auth.logout()
  router.push('/login')
}
</script>
