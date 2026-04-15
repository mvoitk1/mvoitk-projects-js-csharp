<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  adminGetCategories,
  adminCreateCategory,
  adminUpdateCategory,
  adminDeleteCategory,
} from '@/api/admin'
import type { AdminCategoryDto, AdminCategoryWriteDto } from '@/types'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const { t } = useLocale()
const { show } = useToast()

const categories = ref<AdminCategoryDto[]>([])
const loading = ref(false)
const showModal = ref(false)
const editing = ref<AdminCategoryDto | null>(null)
const saving = ref(false)

const form = ref<AdminCategoryWriteDto>({ nameEn: '', nameEt: null, parentCategoryId: null })

async function load() {
  loading.value = true
  try {
    categories.value = await adminGetCategories()
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editing.value = null
  form.value = { nameEn: '', nameEt: null, parentCategoryId: null }
  showModal.value = true
}

function openEdit(cat: AdminCategoryDto) {
  editing.value = cat
  form.value = { nameEn: cat.nameEn ?? '', nameEt: cat.nameEt ?? null, parentCategoryId: cat.parentCategoryId }
  showModal.value = true
}

async function save() {
  saving.value = true
  try {
    if (editing.value) {
      await adminUpdateCategory(editing.value.id, form.value)
      show(t('Category updated.', 'Kategooria uuendatud.'), 'success')
    } else {
      await adminCreateCategory(form.value)
      show(t('Category created.', 'Kategooria loodud.'), 'success')
    }
    showModal.value = false
    await load()
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    saving.value = false
  }
}

async function remove(id: string) {
  if (!confirm(t('Delete this category?', 'Kustuta see kategooria?'))) return
  try {
    await adminDeleteCategory(id)
    show(t('Deleted.', 'Kustutatud.'), 'success')
    await load()
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

function parentName(id: string | null) {
  if (!id) return '—'
  return categories.value.find((c) => c.id === id)?.nameEn ?? id
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1>{{ t('Categories', 'Kategooriad') }}</h1>
      <button class="btn-primary" @click="openCreate">+ {{ t('New', 'Uus') }}</button>
    </div>

    <div v-if="loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>{{ t('Name (EN)', 'Nimi (EN)') }}</th>
          <th>{{ t('Name (ET)', 'Nimi (ET)') }}</th>
          <th>{{ t('Parent', 'Vanem') }}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="cat in categories" :key="cat.id">
          <td>{{ cat.nameEn }}</td>
          <td>{{ cat.nameEt ?? '—' }}</td>
          <td>{{ parentName(cat.parentCategoryId) }}</td>
          <td class="actions">
            <button class="btn-edit" @click="openEdit(cat)">{{ t('Edit', 'Muuda') }}</button>
            <button class="btn-delete" @click="remove(cat.id)">{{ t('Delete', 'Kustuta') }}</button>
          </td>
        </tr>
        <tr v-if="categories.length === 0">
          <td colspan="4" class="empty-cell">{{ t('No categories.', 'Kategooriaid pole.') }}</td>
        </tr>
      </tbody>
    </table>

    <!-- Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2>{{ editing ? t('Edit Category', 'Muuda kategooriat') : t('New Category', 'Uus kategooria') }}</h2>
        <form @submit.prevent="save">
          <div class="form-group">
            <label>{{ t('Name (EN)', 'Nimi (EN)') }} *</label>
            <input v-model="form.nameEn" type="text" required />
          </div>
          <div class="form-group">
            <label>{{ t('Name (ET)', 'Nimi (ET)') }}</label>
            <input v-model="(form as AdminCategoryWriteDto).nameEt" type="text" />
          </div>
          <div class="form-group">
            <label>{{ t('Parent Category', 'Vanem-kategooria') }}</label>
            <select v-model="form.parentCategoryId">
              <option :value="null">{{ t('None', 'Puudub') }}</option>
              <option
                v-for="cat in categories.filter((c) => c.id !== editing?.id)"
                :key="cat.id"
                :value="cat.id"
              >{{ cat.nameEn }}</option>
            </select>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="showModal = false">{{ t('Cancel', 'Tühista') }}</button>
            <button type="submit" class="btn-primary" :disabled="saving">
              {{ saving ? t('Saving…', 'Salvestan…') : t('Save', 'Salvesta') }}
            </button>
          </div>
        </form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.page-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1.5rem;
}

.page-header h1 {
  font-size: 1.5rem;
}

.btn-primary {
  background: #1a1a1a;
  color: #fff;
  border: none;
  padding: 0.5rem 1rem;
  border-radius: 5px;
  font-size: 0.9rem;
}

.btn-primary:hover:not(:disabled) {
  background: #333;
}

.btn-primary:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.admin-table {
  width: 100%;
  border-collapse: collapse;
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  overflow: hidden;
}

.admin-table th,
.admin-table td {
  padding: 0.75rem 1rem;
  text-align: left;
  font-size: 0.9rem;
  border-bottom: 1px solid #f0f0ec;
}

.admin-table th {
  background: #f8f8f6;
  font-weight: 600;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #888;
}

.admin-table tr:last-child td {
  border-bottom: none;
}

.actions {
  display: flex;
  gap: 0.5rem;
}

.btn-edit {
  background: none;
  border: 1px solid #ddd;
  padding: 0.25rem 0.6rem;
  border-radius: 4px;
  font-size: 0.8rem;
}

.btn-edit:hover {
  border-color: #aaa;
}

.btn-delete {
  background: none;
  border: 1px solid #f5c6c6;
  color: #c0392b;
  padding: 0.25rem 0.6rem;
  border-radius: 4px;
  font-size: 0.8rem;
}

.btn-delete:hover {
  background: #fdf0f0;
}

.empty-cell {
  text-align: center;
  color: #aaa;
}

.modal-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 200;
}

.modal {
  background: #fff;
  border-radius: 10px;
  padding: 2rem;
  width: 100%;
  max-width: 440px;
}

.modal h2 {
  font-size: 1.1rem;
  margin-bottom: 1.25rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 0.3rem;
}

.form-group input,
.form-group select {
  width: 100%;
  padding: 0.5rem 0.7rem;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 0.9rem;
}

.modal-actions {
  display: flex;
  justify-content: flex-end;
  gap: 0.75rem;
  margin-top: 1.25rem;
}

.btn-cancel {
  background: none;
  border: 1px solid #ddd;
  padding: 0.5rem 1rem;
  border-radius: 5px;
  font-size: 0.9rem;
}

.loading {
  color: #888;
  padding: 2rem;
}
</style>
