<script setup lang="ts">
import { useToast } from '@/composables/useToast'

const { toasts, dismiss } = useToast()
</script>

<template>
  <div class="toast-container">
    <TransitionGroup name="toast">
      <div
        v-for="toast in toasts"
        :key="toast.id"
        class="toast"
        :class="`toast--${toast.type}`"
        @click="dismiss(toast.id)"
      >
        {{ toast.message }}
      </div>
    </TransitionGroup>
  </div>
</template>

<style scoped>
.toast-container {
  position: fixed;
  bottom: 1.5rem;
  right: 1.5rem;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  z-index: 1000;
}

.toast {
  padding: 0.75rem 1.25rem;
  border-radius: 6px;
  color: #fff;
  font-size: 0.9rem;
  cursor: pointer;
  max-width: 320px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.toast--success {
  background: #2d7a4f;
}

.toast--error {
  background: #c0392b;
}

.toast--info {
  background: #2c5fa8;
}

.toast-enter-active,
.toast-leave-active {
  transition: all 0.3s ease;
}

.toast-enter-from,
.toast-leave-to {
  opacity: 0;
  transform: translateX(1rem);
}
</style>
