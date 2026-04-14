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
const taskSort = ref(0)
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
      taskSort: taskSort.value,
      todoCategoryId: todoCategoryId.value || undefined,
      todoPriorityId: todoPriorityId.value || undefined,
    })
    router.push('/dashboard')
  } catch {
    error.value = 'Failed to create task.'
  }
}
</script>

<template>
  <div class="form-container">
    <h2>New Task</h2>
    <p v-if="error" class="error">{{ error }}</p>
    <form @submit.prevent="submit">
      <label>Task Name<input v-model="taskName" type="text" required /></label>
      <label>Due Date<input v-model="dueDt" type="date" /></label>
      <label>
        Category
        <select v-model="todoCategoryId">
          <option value="">— none —</option>
          <option v-for="c in categoryStore.items" :key="c.id" :value="c.id">
            {{ c.categoryName }}
          </option>
        </select>
      </label>
      <label>
        Priority
        <select v-model="todoPriorityId">
          <option value="">— none —</option>
          <option v-for="p in priorityStore.items" :key="p.id" :value="p.id">
            {{ p.priorityName }}
          </option>
        </select>
      </label>
      <label>Sort Order<input v-model.number="taskSort" type="number" /></label>
      <div class="actions">
        <button type="submit" :disabled="tasksStore.loading">
          {{ tasksStore.loading ? 'Saving…' : 'Create' }}
        </button>
        <RouterLink to="/dashboard">Cancel</RouterLink>
      </div>
    </form>
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
input,
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
button:disabled {
  opacity: 0.6;
  cursor: default;
}
.error {
  color: #ef5350;
  margin-bottom: 0.5rem;
}
</style>
