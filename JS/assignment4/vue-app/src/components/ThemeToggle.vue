<script setup lang="ts">
import { onMounted, ref } from 'vue'

const theme = ref<'light' | 'dark'>('light')

onMounted(() => {
  const savedTheme = localStorage.getItem('theme')
  theme.value = savedTheme === 'dark' ? 'dark' : 'light'
  applyTheme()
})

function toggleTheme() {
  theme.value = theme.value === 'dark' ? 'light' : 'dark'
  localStorage.setItem('theme', theme.value)
  applyTheme()
}

function applyTheme() {
  document.documentElement.setAttribute('data-theme', theme.value)
}
</script>

<template>
  <button type="button" class="theme-toggle" @click="toggleTheme">
    {{ theme === 'dark' ? 'Light mode' : 'Dark mode' }}
  </button>
</template>
