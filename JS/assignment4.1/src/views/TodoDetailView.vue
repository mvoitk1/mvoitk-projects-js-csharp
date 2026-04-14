<template>
  <section class="create-task-page">
    <div class="page-header">
      <div>
        <h1>Edit Task</h1>
      </div>
      <div class="page-actions">
        <RouterLink class="button-secondary button-link" to="/dashboard">Back to dashboard</RouterLink>
      </div>
    </div>

    <p v-if="error" class="error">{{ error }}</p>

    <form v-if="task" class="task-form" @submit.prevent="save">
      <div class="field">
        <label for="taskName">Task name</label>
        <input id="taskName" v-model="task.taskName" type="text" required />
      </div>
      <div class="field">
        <label for="dueDt">Due date</label>
        <input id="dueDt" v-model="task.dueDt" type="date" />
      </div>
      <div class="field">
        <label for="categorySelect">Category</label>
        <select id="categorySelect" v-model="task.todoCategoryId">
          <option value="">— none —</option>
          <option v-for="c in categoryStore.items" :key="c.id" :value="c.id">
            {{ c.categoryName }}
          </option>
        </select>
      </div>
      <div class="field">
        <label for="prioritySelect">Priority</label>
        <select id="prioritySelect" v-model="task.todoPriorityId">
          <option value="">— none —</option>
          <option v-for="p in priorityStore.items" :key="p.id" :value="p.id">
            {{ p.priorityName }}
          </option>
        </select>
      </div>
      <div class="field">
        <label for="taskSort">Sort order</label>
        <input id="taskSort" v-model.number="task.taskSort" type="number" />
      </div>
      <div class="field">
        <label class="task-check">
          <input v-model="task.isCompleted" type="checkbox" />
          <span>Completed</span>
        </label>
      </div>
      <div class="field">
        <label class="task-check">
          <input v-model="task.isArchived" type="checkbox" />
          <span>Archived</span>
        </label>
      </div>
      <div class="form-actions">
        <button type="submit" :disabled="loading">{{ loading ? 'Saving…' : 'Save' }}</button>
        <button type="button" class="delete-btn" :disabled="loading" @click="remove">Delete</button>
        <RouterLink class="text-link" to="/dashboard">Cancel</RouterLink>
      </div>
    </form>

    <p v-else-if="!error">Loading…</p>
  </section>
</template>

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
