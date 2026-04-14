<template>
  <section class="dashboard">
    <div class="page-header">
      <div>
        <h1>Priority</h1>
      </div>
    </div>

    <form class="task-form" @submit.prevent="handleCreate">
      <div class="field">
        <label for="priorityName">Priority name</label>
        <input id="priorityName" v-model.trim="newName" type="text" required />
      </div>
      <button type="submit">Add</button>
    </form>

    <p v-if="store.error" class="error">{{ store.error }}</p>

    <div v-if="store.items.length" class="task-grid">
      <article v-for="priority in sortedPriorities" :key="priority.id" class="task-card">
        <div class="task-card__top">
          <h2>{{ priority.priorityName }}</h2>
          <div class="row-actions">
            <button type="button" class="button-secondary" @click="toggleEdit(priority.id)">Edit</button>
            <button type="button" class="button-secondary" @click="toggleDetails(priority.id)">Details</button>
            <button type="button" class="delete-btn" @click="store.remove(priority.id)">Delete</button>
          </div>
        </div>

        <div v-if="openDetailsId === priority.id" class="details-box">
          <p><strong>Name:</strong> {{ priority.priorityName }}</p>
          <p><strong>Sort:</strong> {{ priority.prioritySort }}</p>
          <p><strong>Id:</strong> {{ priority.id }}</p>
        </div>

        <form v-if="editId === priority.id" class="inline-edit" @submit.prevent="handleUpdate(priority.id)">
          <div class="field">
            <label :for="`edit-priority-${priority.id}`">Name</label>
            <input :id="`edit-priority-${priority.id}`" v-model.trim="editName" type="text" required />
          </div>
          <div class="row-actions">
            <button type="submit">Save</button>
            <button type="button" class="button-secondary" @click="cancelEdit">Cancel</button>
          </div>
        </form>
      </article>
    </div>

    <p v-else-if="!store.loading">No priorities yet.</p>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTodoPriorityStore } from '@/stores/todoPriority'

const store = useTodoPriorityStore()
const newName = ref('')
const editId = ref<string | null>(null)
const openDetailsId = ref<string | null>(null)
const editName = ref('')

const sortedPriorities = computed(() =>
  [...store.items].sort((left, right) => left.prioritySort - right.prioritySort)
)

onMounted(() => store.fetchAll())

async function handleCreate() {
  if (!newName.value) return
  await store.create({
    priorityName: newName.value,
    prioritySort: store.items.length,
  })
  newName.value = ''
}

function toggleDetails(id: string) {
  openDetailsId.value = openDetailsId.value === id ? null : id
}

function toggleEdit(id: string) {
  if (editId.value === id) {
    cancelEdit()
    return
  }
  const priority = store.items.find((item) => item.id === id)
  if (!priority) return
  editId.value = id
  editName.value = priority.priorityName
}

function cancelEdit() {
  editId.value = null
  editName.value = ''
}

async function handleUpdate(id: string) {
  const priority = store.items.find((item) => item.id === id)
  if (!priority) return
  await store.update(id, {
    ...priority,
    priorityName: editName.value,
  })
  cancelEdit()
}
</script>
