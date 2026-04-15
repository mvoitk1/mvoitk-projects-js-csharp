<script setup lang="ts">
import { ref, onMounted } from 'vue'
import {
  adminGetCollections,
  adminCreateCollection,
  adminUpdateCollection,
  adminDeleteCollection,
} from '@/api/admin'
import type { AdminCollectionDto, AdminCollectionWriteDto } from '@/types'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const { t } = useLocale()
const { show } = useToast()

const collections = ref<AdminCollectionDto[]>([])
const loading = ref(false)
const showModal = ref(false)
const editing = ref<AdminCollectionDto | null>(null)
const saving = ref(false)

const emptyForm = (): AdminCollectionWriteDto => ({
  nameEn: '',
  nameEt: null,
  descriptionEn: null,
  descriptionEt: null,
  launchDate: null,
  isActive: true,
})

const form = ref<AdminCollectionWriteDto>(emptyForm())

async function load() {
  loading.value = true
  try {
    collections.value = await adminGetCollections()
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    loading.value = false
  }
}

function openCreate() {
  editing.value = null
  form.value = emptyForm()
  showModal.value = true
}

function openEdit(col: AdminCollectionDto) {
  editing.value = col
  form.value = {
    nameEn: col.nameEn ?? '',
    nameEt: col.nameEt ?? null,
    descriptionEn: col.descriptionEn ?? null,
    descriptionEt: col.descriptionEt ?? null,
    launchDate: col.launchDate ? col.launchDate.substring(0, 10) : null,
    isActive: col.isActive,
  }
  showModal.value = true
}

async function save() {
  saving.value = true
  try {
    if (editing.value) {
      await adminUpdateCollection(editing.value.id, form.value)
      show(t('Collection updated.', 'Kollektsioon uuendatud.'), 'success')
    } else {
      await adminCreateCollection(form.value)
      show(t('Collection created.', 'Kollektsioon loodud.'), 'success')
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
  if (!confirm(t('Delete this collection?', 'Kustuta see kollektsioon?'))) return
  try {
    await adminDeleteCollection(id)
    show(t('Deleted.', 'Kustutatud.'), 'success')
    await load()
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1>{{ t('Collections', 'Kollektsioonid') }}</h1>
      <button class="btn-primary" @click="openCreate">+ {{ t('New', 'Uus') }}</button>
    </div>

    <div v-if="loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>{{ t('Name (EN)', 'Nimi (EN)') }}</th>
          <th>{{ t('Name (ET)', 'Nimi (ET)') }}</th>
          <th>{{ t('Launch Date', 'Avamiskuupäev') }}</th>
          <th>{{ t('Active', 'Aktiivne') }}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="col in collections" :key="col.id">
          <td>{{ col.nameEn }}</td>
          <td>{{ col.nameEt ?? '—' }}</td>
          <td>{{ col.launchDate ? new Date(col.launchDate).toLocaleDateString() : '—' }}</td>
          <td>
            <span :class="col.isActive ? 'badge-active' : 'badge-inactive'">
              {{ col.isActive ? t('Yes', 'Jah') : t('No', 'Ei') }}
            </span>
          </td>
          <td class="actions">
            <button class="btn-edit" @click="openEdit(col)">{{ t('Edit', 'Muuda') }}</button>
            <button class="btn-delete" @click="remove(col.id)">{{ t('Delete', 'Kustuta') }}</button>
          </td>
        </tr>
        <tr v-if="collections.length === 0">
          <td colspan="5" class="empty-cell">{{ t('No collections.', 'Kollektsioone pole.') }}</td>
        </tr>
      </tbody>
    </table>

    <!-- Modal -->
    <div v-if="showModal" class="modal-overlay" @click.self="showModal = false">
      <div class="modal">
        <h2>{{ editing ? t('Edit Collection', 'Muuda kollektsiooni') : t('New Collection', 'Uus kollektsioon') }}</h2>
        <form @submit.prevent="save">
          <div class="form-row">
            <div class="form-group">
              <label>{{ t('Name (EN)', 'Nimi (EN)') }} *</label>
              <input v-model="form.nameEn" type="text" required />
            </div>
            <div class="form-group">
              <label>{{ t('Name (ET)', 'Nimi (ET)') }}</label>
              <input v-model="form.nameEt" type="text" />
            </div>
          </div>
          <div class="form-group">
            <label>{{ t('Description (EN)', 'Kirjeldus (EN)') }}</label>
            <textarea v-model="form.descriptionEn" rows="2" />
          </div>
          <div class="form-group">
            <label>{{ t('Description (ET)', 'Kirjeldus (ET)') }}</label>
            <textarea v-model="form.descriptionEt" rows="2" />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>{{ t('Launch Date', 'Avamiskuupäev') }}</label>
              <input v-model="form.launchDate" type="date" />
            </div>
            <div class="form-group form-group--checkbox">
              <label>
                <input v-model="form.isActive" type="checkbox" />
                {{ t('Active', 'Aktiivne') }}
              </label>
            </div>
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

.badge-active {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  background: #d4edda;
  color: #2d7a4f;
  border-radius: 12px;
  font-size: 0.78rem;
  font-weight: 600;
}

.badge-inactive {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  background: #f0f0ec;
  color: #888;
  border-radius: 12px;
  font-size: 0.78rem;
  font-weight: 600;
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
  max-width: 520px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal h2 {
  font-size: 1.1rem;
  margin-bottom: 1.25rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
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
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.5rem 0.7rem;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 0.9rem;
  font-family: inherit;
}

.form-group--checkbox label {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  margin-top: 1.5rem;
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
