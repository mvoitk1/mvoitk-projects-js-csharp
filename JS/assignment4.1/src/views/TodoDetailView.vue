<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTodoTasksStore } from '@/stores/todoTasks'
import { useTodoCategoryStore } from '@/stores/todoCategory'
import { useTodoPriorityStore } from '@/stores/todoPriority'
import { todoTasksApi } from '@/api/todoTasks'
import type { TodoTask } from '@/types'

const route = useRoute()
const router = useRouter()
const tasksStore = useTodoTasksStore()
const categoryStore = useTodoCategoryStore()
const priorityStore = useTodoPriorityStore()

const task = ref<TodoTask | null>(null)
const error = ref<string | null>(null)
const loading = ref(false)

onMounted(async () => {
  if (!categoryStore.items.length) categoryStore.fetchAll()
  if (!priorityStore.items.length) priorityStore.fetchAll()
  try {
    const res = await todoTasksApi.getById(route.params.id as string)
    task.value = { ...res.data }
  } catch {
    error.value = 'Task not found.'
  }
})

async function save() {
  if (!task.value) return
  error.value = null
  loading.value = true
  try {
    await tasksStore.update(task.value.id, task.value)
    router.push('/dashboard')
  } catch {
    error.value = 'Failed to save task.'
  } finally {
    loading.value = false
  }
}

async function remove() {
  if (!task.value) return
  loading.value = true
  try {
    await tasksStore.remove(task.value.id)
    router.push('/dashboard')
  } catch {
    error.value = 'Failed to delete task.'
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="form-container">
    <h2>Edit Task</h2>
    <p v-if="error" class="error">{{ error }}</p>
    <div v-if="task">
      <form @submit.prevent="save">
        <label>Task Name<input v-model="task.taskName" type="text" required /></label>
        <label>Due Date<input v-model="task.dueDt" type="date" /></label>
        <label>
          Category
          <select v-model="task.todoCategoryId">
            <option value="">— none —</option>
            <option v-for="c in categoryStore.items" :key="c.id" :value="c.id">
              {{ c.categoryName }}
            </option>
          </select>
        </label>
        <label>
          Priority
          <select v-model="task.todoPriorityId">
            <option value="">— none —</option>
            <option v-for="p in priorityStore.items" :key="p.id" :value="p.id">
              {{ p.priorityName }}
            </option>
          </select>
        </label>
        <label>Sort Order<input v-model.number="task.taskSort" type="number" /></label>
        <label class="checkbox-row">
          <input v-model="task.isCompleted" type="checkbox" />
          Completed
        </label>
        <label class="checkbox-row">
          <input v-model="task.isArchived" type="checkbox" />
          Archived
        </label>
        <div class="actions">
          <button type="submit" :disabled="loading">{{ loading ? 'Saving…' : 'Save' }}</button>
          <button type="button" class="btn-del" :disabled="loading" @click="remove">
            Delete
          </button>
          <RouterLink to="/dashboard">Cancel</RouterLink>
        </div>
      </form>
    </div>
    <p v-else-if="!error">Loading…</p>
  </div>
</template>

<style scoped>
.form-container {
  max-width: 480px;
  margin: 2rem auto;
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
.checkbox-row {
  flex-direction: row;
  align-items: center;
  gap: 0.5rem;
}
input[type='text'],
input[type='date'],
input[type='number'],
select {
  padding: 0.5rem;
  border: 1px solid #555;
  border-radius: 4px;
  background: #222;
  color: inherit;
}
.actions {
  display: flex;
  gap: 1rem;
  align-items: center;
}
button {
  padding: 0.6rem 1.2rem;
  background: #1565c0;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
.btn-del {
  background: #c62828;
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
