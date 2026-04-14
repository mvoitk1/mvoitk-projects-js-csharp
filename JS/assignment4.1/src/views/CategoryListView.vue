<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useTodoCategoryStore } from '@/stores/todoCategory'

const store = useTodoCategoryStore()

const newName = ref('')
const newSort = ref(0)
const newTag = ref('')
const error = ref<string | null>(null)

onMounted(() => {
  store.fetchAll()
})

async function create() {
  error.value = null
  try {
    await store.create({
      categoryName: newName.value,
      categorySort: newSort.value,
      tag: newTag.value || undefined,
    })
    newName.value = ''
    newSort.value = 0
    newTag.value = ''
  } catch {
    error.value = 'Failed to create category.'
  }
}

async function remove(id: string) {
  await store.remove(id)
}
</script>

<template>
  <div class="page">
    <h2>Categories</h2>
    <p v-if="store.error || error" class="error">{{ store.error ?? error }}</p>

    <ul class="list">
      <li v-for="cat in store.items" :key="cat.id" class="item">
        <span>{{ cat.categoryName }}</span>
        <span class="meta">sort: {{ cat.categorySort }}</span>
        <span v-if="cat.tag" class="tag">{{ cat.tag }}</span>
        <button class="btn-del" @click="remove(cat.id)">Delete</button>
      </li>
      <li v-if="store.items.length === 0 && !store.loading">No categories yet.</li>
    </ul>

    <form class="create-form" @submit.prevent="create">
      <h3>New Category</h3>
      <label>Name<input v-model="newName" type="text" required /></label>
      <label>Sort<input v-model.number="newSort" type="number" /></label>
      <label>Tag<input v-model="newTag" type="text" /></label>
      <button type="submit" :disabled="store.loading">Add</button>
    </form>
  </div>
</template>

<style scoped>
.page {
  max-width: 600px;
  margin: 2rem auto;
  padding: 0 1rem;
}
.list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  margin-bottom: 2rem;
}
.item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.8rem;
  border: 1px solid #333;
  border-radius: 6px;
}
.item span:first-child {
  flex: 1;
}
.meta {
  font-size: 0.8rem;
  color: #aaa;
}
.tag {
  font-size: 0.75rem;
  background: #263238;
  padding: 0.1rem 0.5rem;
  border-radius: 10px;
}
.btn-del {
  padding: 0.2rem 0.6rem;
  background: #c62828;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
.create-form {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  border: 1px solid #333;
  padding: 1.25rem;
  border-radius: 8px;
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
button[type='submit'] {
  padding: 0.6rem;
  background: #1565c0;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
button[type='submit']:disabled {
  opacity: 0.6;
}
.error {
  color: #ef5350;
  margin-bottom: 0.5rem;
}
</style>
