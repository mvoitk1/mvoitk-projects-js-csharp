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

    <form class="task-form" @submit.prevent="handleCreate">
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
          <select id="categorySelect" v-model="selectedCategoryId" :disabled="categoryOptions.length === 0" required>
            <option disabled value="">Select a category</option>
            <option v-for="category in categoryOptions" :key="category.id" :value="category.id">
              {{ category.categoryName }}
            </option>
          </select>
          <button type="button" class="icon-button" @click="showCategoryCreator = !showCategoryCreator">+</button>
        </div>
      </div>

      <div v-if="showCategoryCreator" class="inline-create">
        <div class="field">
          <label for="customCategory">New category</label>
          <input id="customCategory" v-model.trim="newCategoryName" type="text" placeholder="Category name" />
        </div>
        <button type="button" class="button-secondary" @click="handleCreateCategory">Save category</button>
      </div>

      <div class="field">
        <label for="prioritySelect">Priority</label>
        <div class="picker-row">
          <select id="prioritySelect" v-model="selectedPriorityId" :disabled="priorityOptions.length === 0" required>
            <option disabled value="">Select a priority</option>
            <option v-for="priority in priorityOptions" :key="priority.id" :value="priority.id">
              {{ priority.priorityName }}
            </option>
          </select>
          <button type="button" class="icon-button" @click="showPriorityCreator = !showPriorityCreator">+</button>
        </div>
      </div>

      <div v-if="showPriorityCreator" class="inline-create">
        <div class="field">
          <label for="customPriority">New priority</label>
          <input id="customPriority" v-model.trim="newPriorityName" type="text" placeholder="Priority name" />
        </div>
        <button type="button" class="button-secondary" @click="handleCreatePriority">Save priority</button>
      </div>

      <p v-if="formError || taskStore.error || categoryStore.error || priorityStore.error" class="error">
        {{ formError || taskStore.error || categoryStore.error || priorityStore.error }}
      </p>

      <div class="form-actions">
        <button type="submit" :disabled="taskStore.loading">Create task</button>
        <RouterLink class="text-link" to="/dashboard">Cancel</RouterLink>
      </div>
    </form>
  </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { defaultCategories, defaultPriorities } from '../constants/taskDefaults'
import { useTodoCategoryStore } from '../stores/todoCategory'
import { useTodoPriorityStore } from '../stores/todoPriority'
import { useTodoTasksStore } from '../stores/todoTasks'

const router = useRouter()
const taskStore = useTodoTasksStore()
const categoryStore = useTodoCategoryStore()
const priorityStore = useTodoPriorityStore()

const taskName = ref('')
const dueDt = ref('')
const selectedCategoryId = ref('')
const selectedPriorityId = ref('')
const showCategoryCreator = ref(false)
const showPriorityCreator = ref(false)
const newCategoryName = ref('')
const newPriorityName = ref('')
const formError = ref<string | null>(null)

const categoryOptions = computed(() =>
  [...categoryStore.items].sort((left, right) => left.categorySort - right.categorySort)
)

const priorityOptions = computed(() =>
  [...priorityStore.items].sort((left, right) => left.prioritySort - right.prioritySort)
)

onMounted(async () => {
  await Promise.all([categoryStore.fetchAll(), priorityStore.fetchAll()])
  await ensureStarterOptions()
  setDefaultSelections()
})

async function ensureStarterOptions() {
  if (categoryStore.items.length === 0) {
    for (const category of defaultCategories) {
      await categoryStore.create(category)
    }
  }

  if (priorityStore.items.length === 0) {
    for (const priority of defaultPriorities) {
      await priorityStore.create(priority)
    }
  }
}

function setDefaultSelections() {
  if (!selectedCategoryId.value && categoryOptions.value.length > 0) {
    selectedCategoryId.value = categoryOptions.value[0].id
  }

  if (!selectedPriorityId.value && priorityOptions.value.length > 0) {
    selectedPriorityId.value = priorityOptions.value[0].id
  }
}

async function handleCreateCategory() {
  if (!newCategoryName.value) {
    formError.value = 'Enter a category name first.'
    return
  }

  formError.value = null
  const created = await categoryStore.create({
    categoryName: newCategoryName.value,
    categorySort: categoryStore.items.length,
  })

  selectedCategoryId.value = created.id
  newCategoryName.value = ''
  showCategoryCreator.value = false
}

async function handleCreatePriority() {
  if (!newPriorityName.value) {
    formError.value = 'Enter a priority name first.'
    return
  }

  formError.value = null
  const created = await priorityStore.create({
    priorityName: newPriorityName.value,
    prioritySort: priorityStore.items.length,
  })

  selectedPriorityId.value = created.id
  newPriorityName.value = ''
  showPriorityCreator.value = false
}

async function handleCreate() {
  if (!taskName.value || !selectedCategoryId.value || !selectedPriorityId.value) {
    formError.value = 'Fill in the task name, category, and priority.'
    return
  }

  formError.value = null

  await taskStore.create({
    taskName: taskName.value,
    dueDt: dueDt.value || undefined,
    isCompleted: false,
    isArchived: false,
    taskSort: taskStore.items.length,
    todoCategoryId: selectedCategoryId.value,
    todoPriorityId: selectedPriorityId.value,
  })

  router.push('/dashboard')
}
</script>
