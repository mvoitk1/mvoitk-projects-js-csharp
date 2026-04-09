<template>
  <div>
    <RouterLink to="/todos">← Back</RouterLink>
    <h2>Edit Task</h2>

    <div v-if="task">
      <form @submit.prevent="handleSave">
        <div class="field">
          <label>Name</label>
          <input v-model="task.taskName" type="text" required />
        </div>
        <div class="field">
          <label>Due Date</label>
          <input v-model="task.dueDt" type="date" />
        </div>
        <div class="field">
          <label>
            <input type="checkbox" v-model="task.isCompleted" />
            Completed
          </label>
        </div>
        <div class="field">
          <label>
            <input type="checkbox" v-model="task.isArchived" />
            Archived
          </label>
        </div>
        <div class="field">
          <label>Priority</label>
          <select v-model="task.todoPriorityId">
            <option v-for="priority in priorityStore.items" :key="priority.id" :value="priority.id">
              {{ priority.priorityName }}
            </option>
          </select>
        </div>
        <div class="field">
          <label>Sort Order</label>
          <input v-model.number="task.taskSort" type="number" />
        </div>
        <div class="field">
          <label>Category</label>
          <select v-model="task.todoCategoryId">
            <option value="">None</option>
            <option v-for="cat in categoryStore.items" :key="cat.id" :value="cat.id">{{ cat.categoryName }}</option>
          </select>
        </div>
        <p v-if="error" class="error">{{ error }}</p>
        <button type="submit">Save</button>
      </form>
    </div>
    <p v-else>Loading...</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { todoTasksApi } from '../api/todoTasks'
import { useTodoTasksStore } from '../stores/todoTasks'
import { useTodoCategoryStore } from '../stores/todoCategory'
import { useTodoPriorityStore } from '../stores/todoPriority'
import type { TodoTask } from '../types'

const route = useRoute()
const router = useRouter()
const store = useTodoTasksStore()
const categoryStore = useTodoCategoryStore()
const priorityStore = useTodoPriorityStore()

const task = ref<TodoTask | null>(null)
const error = ref<string | null>(null)

onMounted(async () => {
  const id = route.params.id as string
  try {
    const { data } = await todoTasksApi.getById(id)
    task.value = data
  } catch {
    error.value = 'Task not found'
  }
  await Promise.all([categoryStore.fetchAll(), priorityStore.fetchAll()])
})

async function handleSave() {
  if (!task.value) return
  error.value = null
  try {
    await store.update(task.value.id, task.value)
    router.push('/todos')
  } catch (e: unknown) {
    error.value = e instanceof Error ? e.message : 'Save failed'
  }
}
</script>
