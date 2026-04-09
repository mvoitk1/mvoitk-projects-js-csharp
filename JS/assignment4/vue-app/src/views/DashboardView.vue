<template>
  <section class="dashboard">
    <div class="page-header">
      <div>
        <h1>Dashboard</h1>
      </div>
      <div class="page-actions">
        <RouterLink class="button-link" to="/tasks/new">Create task</RouterLink>
      </div>
    </div>

    <div class="stats-grid">
      <article class="stat-card">
        <span class="stat-label">Total tasks</span>
        <strong>{{ tasks.length }}</strong>
      </article>
      <article class="stat-card">
        <span class="stat-label">Due soon</span>
        <strong>{{ dueSoonCount }}</strong>
      </article>
      <article class="stat-card">
        <span class="stat-label">Completed</span>
        <strong>{{ completedCount }}</strong>
      </article>
    </div>

    <p v-if="taskStore.error" class="error">{{ taskStore.error }}</p>

    <div v-if="tasks.length" class="task-grid">
      <article
        v-for="task in tasks"
        :key="task.id"
        class="task-card"
        :class="{ 'task-card--completed': task.isCompleted }"
      >
        <div class="task-card__top">
          <label class="task-check">
            <input
              :checked="task.isCompleted"
              type="checkbox"
              @change="toggleComplete(task)"
            />
            <span>{{ task.isCompleted ? 'Completed' : 'Open' }}</span>
          </label>
          <button type="button" class="delete-btn" @click="handleDelete(task.id)">Delete</button>
        </div>

        <h2>{{ task.taskName }}</h2>

        <div class="task-meta">
          <span class="meta-pill">
            Due {{ task.dueDt ? formatDate(task.dueDt) : 'No due date' }}
          </span>
          <span class="meta-pill">
            Category {{ categoryName(task.todoCategoryId) }}
          </span>
          <span
            class="meta-pill"
            :class="priorityToneClass(priorityName(task.todoPriorityId))"
          >
            Priority {{ priorityName(task.todoPriorityId) }}
          </span>
        </div>
      </article>
    </div>

    <section v-else class="empty-state">
      <h2>No tasks yet.</h2>
      <RouterLink class="button-link" to="/tasks/new">Create your first task</RouterLink>
    </section>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted } from 'vue'
import { useTodoCategoryStore } from '../stores/todoCategory'
import { useTodoPriorityStore } from '../stores/todoPriority'
import { useTodoTasksStore } from '../stores/todoTasks'
import type { TodoTask } from '../types'

const taskStore = useTodoTasksStore()
const categoryStore = useTodoCategoryStore()
const priorityStore = useTodoPriorityStore()

const tasks = computed(() =>
  [...taskStore.items].sort((left, right) => {
    const leftDue = left.dueDt ? new Date(left.dueDt).getTime() : Number.MAX_SAFE_INTEGER
    const rightDue = right.dueDt ? new Date(right.dueDt).getTime() : Number.MAX_SAFE_INTEGER
    return leftDue - rightDue
  })
)

const completedCount = computed(() => tasks.value.filter((task) => task.isCompleted).length)

const dueSoonCount = computed(() => {
  const now = new Date()
  const inSevenDays = new Date()
  inSevenDays.setDate(now.getDate() + 7)

  return tasks.value.filter((task) => {
    if (!task.dueDt || task.isCompleted) return false
    const dueDate = new Date(task.dueDt)
    return dueDate >= now && dueDate <= inSevenDays
  }).length
})

onMounted(async () => {
  await Promise.all([
    taskStore.fetchAll(),
    categoryStore.fetchAll(),
    priorityStore.fetchAll(),
  ])
})

function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-US', {
    month: 'short',
    day: 'numeric',
    year: 'numeric',
  }).format(new Date(value))
}

function categoryName(categoryId?: string) {
  return categoryStore.items.find((category) => category.id === categoryId)?.categoryName ?? 'Uncategorized'
}

function priorityName(priorityId?: string) {
  return priorityStore.items.find((priority) => priority.id === priorityId)?.priorityName ?? 'Not set'
}

function priorityToneClass(priority: string) {
  const normalized = priority.toLowerCase()
  if (normalized === 'high') return 'meta-pill--high'
  if (normalized === 'medium') return 'meta-pill--medium'
  if (normalized === 'low') return 'meta-pill--low'
  return ''
}

async function toggleComplete(task: TodoTask) {
  await taskStore.update(task.id, { ...task, isCompleted: !task.isCompleted })
}

async function handleDelete(id: string) {
  await taskStore.remove(id)
}
</script>
