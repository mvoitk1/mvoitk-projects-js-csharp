<template>
  <div>
    <h2>My Todos</h2>

    <form @submit.prevent="handleCreate" class="create-form">
      <input v-model="newTaskName" type="text" placeholder="New task name" required />
      <button type="submit">Add</button>
    </form>
    <p v-if="store.error" class="error">{{ store.error }}</p>

    <ul class="todo-list">
      <li v-for="task in store.items" :key="task.id" :class="{ completed: task.isCompleted }">
        <input type="checkbox" :checked="task.isCompleted" @change="toggleComplete(task)" />
        <RouterLink :to="`/todos/${task.id}`">{{ task.taskName }}</RouterLink>
        <button class="delete-btn" @click="handleDelete(task.id)">✕</button>
      </li>
    </ul>

    <p v-if="!store.loading && store.items.length === 0">No tasks yet. Add one above.</p>
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useTodoTasksStore } from '../stores/todoTasks'
import type { TodoTask } from '../types'

const store = useTodoTasksStore()
const newTaskName = ref('')

onMounted(() => store.fetchAll())

async function handleCreate() {
  if (!newTaskName.value.trim()) return
  await store.create({
    taskName: newTaskName.value.trim(),
    isCompleted: false,
    isArchived: false,
    taskSort: 0,
  })
  newTaskName.value = ''
}

async function toggleComplete(task: TodoTask) {
  await store.update(task.id, { ...task, isCompleted: !task.isCompleted })
}

async function handleDelete(id: string) {
  await store.remove(id)
}
</script>
