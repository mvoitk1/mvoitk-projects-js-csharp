<template>
  <section class="dashboard">
    <div class="page-header">
      <div>
        <h1>Category</h1>
      </div>
    </div>

    <form class="task-form" @submit.prevent="handleCreate">
      <div class="field">
        <label for="categoryName">Category name</label>
        <input id="categoryName" v-model.trim="newName" type="text" required />
      </div>
      <div class="field">
        <label for="categoryTag">Tag</label>
        <input id="categoryTag" v-model.trim="newTag" type="text" />
      </div>
      <button type="submit">Add</button>
    </form>

    <p v-if="store.error" class="error">{{ store.error }}</p>

    <div v-if="store.items.length" class="task-grid">
      <article v-for="category in sortedCategories" :key="category.id" class="task-card">
        <div class="task-card__top">
          <h2>{{ category.categoryName }}</h2>
          <div class="row-actions">
            <button type="button" class="button-secondary" @click="toggleEdit(category.id)">Edit</button>
            <button type="button" class="button-secondary" @click="toggleDetails(category.id)">Details</button>
            <button type="button" class="delete-btn" @click="store.remove(category.id)">Delete</button>
          </div>
        </div>

        <div v-if="openDetailsId === category.id" class="details-box">
          <p><strong>Name:</strong> {{ category.categoryName }}</p>
          <p><strong>Tag:</strong> {{ category.tag || '-' }}</p>
          <p><strong>Sort:</strong> {{ category.categorySort }}</p>
          <p><strong>Id:</strong> {{ category.id }}</p>
        </div>

        <form v-if="editId === category.id" class="inline-edit" @submit.prevent="handleUpdate(category.id)">
          <div class="field">
            <label :for="`edit-category-${category.id}`">Name</label>
            <input :id="`edit-category-${category.id}`" v-model.trim="editName" type="text" required />
          </div>
          <div class="field">
            <label :for="`edit-tag-${category.id}`">Tag</label>
            <input :id="`edit-tag-${category.id}`" v-model.trim="editTag" type="text" />
          </div>
          <div class="row-actions">
            <button type="submit">Save</button>
            <button type="button" class="button-secondary" @click="cancelEdit">Cancel</button>
          </div>
        </form>
      </article>
    </div>

    <p v-else-if="!store.loading">No categories yet.</p>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useTodoCategoryStore } from '../stores/todoCategory'

const store = useTodoCategoryStore()
const newName = ref('')
const newTag = ref('')
const editId = ref<string | null>(null)
const openDetailsId = ref<string | null>(null)
const editName = ref('')
const editTag = ref('')

const sortedCategories = computed(() =>
  [...store.items].sort((left, right) => left.categorySort - right.categorySort)
)

onMounted(() => store.fetchAll())

async function handleCreate() {
  if (!newName.value) return
  await store.create({
    categoryName: newName.value,
    categorySort: store.items.length,
    tag: newTag.value || undefined,
  })
  newName.value = ''
  newTag.value = ''
}

function toggleDetails(id: string) {
  openDetailsId.value = openDetailsId.value === id ? null : id
}

function toggleEdit(id: string) {
  if (editId.value === id) {
    cancelEdit()
    return
  }

  const category = store.items.find((item) => item.id === id)
  if (!category) return

  editId.value = id
  editName.value = category.categoryName
  editTag.value = category.tag ?? ''
}

function cancelEdit() {
  editId.value = null
  editName.value = ''
  editTag.value = ''
}

async function handleUpdate(id: string) {
  const category = store.items.find((item) => item.id === id)
  if (!category) return

  await store.update(id, {
    ...category,
    categoryName: editName.value,
    tag: editTag.value || undefined,
  })

  cancelEdit()
}
</script>
