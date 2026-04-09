<template>
  <div>
    <h2>Categories</h2>

    <form @submit.prevent="handleCreate" class="create-form">
      <input v-model="newName" type="text" placeholder="Category name" required />
      <button type="submit">Add</button>
    </form>
    <p v-if="store.error" class="error">{{ store.error }}</p>

    <ul class="todo-list">
      <li v-for="cat in store.items" :key="cat.id">
        <span>{{ cat.todoCategoryName }}</span>
        <button class="delete-btn" @click="store.remove(cat.id)">✕</button>
      </li>
    </ul>

    <p v-if="!store.loading && store.items.length === 0">No categories yet.</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useTodoCategoryStore } from '../stores/todoCategory'

const store = useTodoCategoryStore()
const newName = ref('')

onMounted(() => store.fetchAll())

async function handleCreate() {
  if (!newName.value.trim()) return
  await store.create({ todoCategoryName: newName.value.trim(), todoCategorySort: 0 })
  newName.value = ''
}
</script>
