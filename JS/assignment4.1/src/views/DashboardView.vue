<script setup lang="ts">
import { onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useTodoTasksStore } from '@/stores/todoTasks'
import { useTodoCategoryStore } from '@/stores/todoCategory'
import { useTodoPriorityStore } from '@/stores/todoPriority'

const route = useRoute()
const router = useRouter()
const tasksStore = useTodoTasksStore()
const categoryStore = useTodoCategoryStore()
const priorityStore = useTodoPriorityStore()

const forbiddenError = computed(() => route.query.error === 'forbidden')

onMounted(() => {
  tasksStore.fetchAll()
  categoryStore.fetchAll()
  priorityStore.fetchAll()
})

function categoryName(id?: string) {
  if (!id) return ''
  return categoryStore.items.find((c) => c.id === id)?.categoryName ?? ''
}

async function toggleComplete(task: (typeof tasksStore.items)[number]) {
  await tasksStore.update(task.id, { ...task, isCompleted: !task.isCompleted })
}

async function removeTask(id: string) {
  await tasksStore.remove(id)
}
</script>

<template>
  <div class="page">
    <p v-if="forbiddenError" class="warning">You do not have permission to access that resource.</p>

    <div class="header">
      <h2>Tasks</h2>
      <RouterLink to="/tasks/new" class="btn-new">+ New Task</RouterLink>
    </div>

    <p v-if="tasksStore.error" class="error">{{ tasksStore.error }}</p>
    <p v-if="tasksStore.loading">Loading…</p>

    <ul v-else class="task-list">
      <li v-for="task in tasksStore.items" :key="task.id" class="task-item">
        <input
          type="checkbox"
          :checked="task.isCompleted"
          @change="toggleComplete(task)"
        />
        <RouterLink :to="`/todos/${task.id}`" :class="{ done: task.isCompleted }">
          {{ task.taskName }}
        </RouterLink>
        <span v-if="categoryName(task.todoCategoryId)" class="tag">
          {{ categoryName(task.todoCategoryId) }}
        </span>
        <span v-if="task.dueDt" class="due">{{ task.dueDt.slice(0, 10) }}</span>
        <button class="btn-del" @click="removeTask(task.id)">Delete</button>
      </li>
      <li v-if="tasksStore.items.length === 0">No tasks yet.</li>
    </ul>
  </div>
</template>

<style scoped>
.page {
  max-width: 700px;
  margin: 2rem auto;
  padding: 0 1rem;
}
.header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1rem;
}
.btn-new {
  padding: 0.35rem 0.9rem;
  background: #1565c0;
  color: white;
  text-decoration: none;
  border-radius: 4px;
}
.task-list {
  list-style: none;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.task-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  padding: 0.6rem 0.8rem;
  border: 1px solid #333;
  border-radius: 6px;
}
.task-item a {
  flex: 1;
  color: #90caf9;
  text-decoration: none;
}
.task-item a.done {
  text-decoration: line-through;
  opacity: 0.6;
}
.tag {
  font-size: 0.75rem;
  background: #263238;
  padding: 0.1rem 0.5rem;
  border-radius: 10px;
}
.due {
  font-size: 0.8rem;
  color: #aaa;
}
.btn-del {
  padding: 0.2rem 0.6rem;
  background: #c62828;
  color: white;
  border: none;
  border-radius: 4px;
  cursor: pointer;
}
.error {
  color: #ef5350;
}
.warning {
  background: #4a1010;
  color: #ffcdd2;
  padding: 0.75rem 1rem;
  border-radius: 6px;
  margin-bottom: 1rem;
}
</style>
