<script setup lang="ts">
import NavBar from '@/components/NavBar.vue'
import ToastContainer from '@/components/ToastContainer.vue'
</script>

<template>
  <NavBar />
  <main class="main-content">
    <RouterView />
  </main>
  <ToastContainer />
</template>

<style>
*,
*::before,
*::after {
  box-sizing: border-box;
  margin: 0;
  padding: 0;
}

body {
  font-family: 'Segoe UI', system-ui, sans-serif;
  background: #f8f8f6;
  color: #1a1a1a;
  line-height: 1.5;
}

.main-content {
  max-width: 1200px;
  margin: 0 auto;
  padding: 2rem 1rem;
}

a {
  color: inherit;
  text-decoration: none;
}

button {
  cursor: pointer;
}
</style>
