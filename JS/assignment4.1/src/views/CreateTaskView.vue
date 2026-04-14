<template>
  <section class="create-task-page">
    <div class="page-header">
      <div>
        <h1>Create task</h1>
      </div>
      <div class="page-actions">
        <RouterLink class="button-secondary button-link" to="/dashboard">Back to dashboard</RouterLink>
      </div>
    </div>

    <form class="task-form" @submit.prevent="submit">
      <div class="field">
        <label for="taskName">Task name</label>
        <input id="taskName" v-model.trim="taskName" type="text" required />
      </div>

      <div class="field">
        <label for="dueDate">Due date</label>
        <input id="dueDate" v-model="dueDt" type="date" />
      </div>

      <div class="field">
        <label for="categorySelect">Category</label>
        <div class="picker-row">
          <select id="categorySelect" v-model="todoCategoryId">
            <option value="">— none —</option>
            <option v-for="c in categoryStore.items" :key="c.id" :value="c.id">
              {{ c.categoryName }}
            </option>
          </select>
        </div>
      </div>

      <div class="field">
        <label for="prioritySelect">Priority</label>
        <div class="picker-row">
          <select id="prioritySelect" v-model="todoPriorityId">
            <option value="">— none —</option>
            <option v-for="p in priorityStore.items" :key="p.id" :value="p.id">
              {{ p.priorityName }}
            </option>
          </select>
        </div>
      </div>

      <p v-if="error || tasksStore.error" class="error">
        {{ error || tasksStore.error }}
      </p>

      <div class="form-actions">
        <button type="submit" :disabled="tasksStore.loading">
          {{ tasksStore.loading ? 'Saving…' : 'Create task' }}
        </button>
        <RouterLink class="text-link" to="/dashboard">Cancel</RouterLink>
      </div>
    </form>
  </section>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { useTodoTasksStore } from '@/stores/todoTasks'
import { useTodoCategoryStore } from '@/stores/todoCategory'
import { useTodoPriorityStore } from '@/stores/todoPriority'

const router = useRouter()
const tasksStore = useTodoTasksStore()
const categoryStore = useTodoCategoryStore()
const priorityStore = useTodoPriorityStore()

const taskName = ref('')
const dueDt = ref('')
const todoCategoryId = ref('')
const todoPriorityId = ref('')
const error = ref<string | null>(null)

onMounted(() => {
  if (!categoryStore.items.length) categoryStore.fetchAll()
  if (!priorityStore.items.length) priorityStore.fetchAll()
})

async function submit() {
  error.value = null
  try {
    await tasksStore.create({
      taskName: taskName.value,
      dueDt: dueDt.value || undefined,
      isCompleted: false,
      isArchived: false,
      taskSort: tasksStore.items.length,
      todoCategoryId: todoCategoryId.value || undefined,
      todoPriorityId: todoPriorityId.value || undefined,
    })
    router.push('/dashboard')
  } catch {
    error.value = 'Failed to create task.'
  }
}
</script>
