<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { adminGetProducts, adminDeleteProduct } from '@/api/admin'
import type { AdminProductDto } from '@/types'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const router = useRouter()
const { t } = useLocale()
const { show } = useToast()

const products = ref<AdminProductDto[]>([])
const loading = ref(false)

async function load() {
  loading.value = true
  try {
    products.value = await adminGetProducts()
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    loading.value = false
  }
}

async function remove(id: string, e: MouseEvent) {
  e.stopPropagation()
  if (!confirm(t('Delete this product?', 'Kustuta see toode?'))) return
  try {
    await adminDeleteProduct(id)
    show(t('Product deleted.', 'Toode kustutatud.'), 'success')
    await load()
  } catch (err) {
    show((err as Error).message, 'error')
  }
}

function goToProduct(id: string) {
  router.push({ name: 'admin-product-detail', params: { id } })
}

function newProduct() {
  router.push({ name: 'admin-product-detail', params: { id: 'new' } })
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1>{{ t('Products', 'Tooted') }}</h1>
      <button class="btn-primary" @click="newProduct">+ {{ t('New Product', 'Uus toode') }}</button>
    </div>

    <div v-if="loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

    <table v-else class="admin-table">
      <thead>
        <tr>
          <th>{{ t('Name (EN)', 'Nimi (EN)') }}</th>
          <th>{{ t('Collection', 'Kollektsioon') }}</th>
          <th>{{ t('Gender', 'Sugu') }}</th>
          <th>{{ t('Active', 'Aktiivne') }}</th>
          <th>{{ t('Variants', 'Variandid') }}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr
          v-for="p in products"
          :key="p.id"
          class="clickable-row"
          @click="goToProduct(p.id)"
        >
          <td>{{ p.nameEn }}</td>
          <td>{{ p.collectionName ?? '—' }}</td>
          <td>{{ p.gender ?? '—' }}</td>
          <td>
            <span :class="p.isActive ? 'badge-active' : 'badge-inactive'">
              {{ p.isActive ? t('Yes', 'Jah') : t('No', 'Ei') }}
            </span>
          </td>
          <td>{{ p.variants?.length ?? 0 }}</td>
          <td class="actions" @click.stop>
            <button class="btn-edit" @click="goToProduct(p.id)">{{ t('Edit', 'Muuda') }}</button>
            <button class="btn-delete" @click="remove(p.id, $event)">{{ t('Delete', 'Kustuta') }}</button>
          </td>
        </tr>
        <tr v-if="products.length === 0">
          <td colspan="6" class="empty-cell">{{ t('No products.', 'Tooteid pole.') }}</td>
        </tr>
      </tbody>
    </table>
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

.btn-primary:hover {
  background: #333;
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

.clickable-row {
  cursor: pointer;
  transition: background 0.1s;
}

.clickable-row:hover {
  background: #fafaf8;
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

.loading {
  color: #888;
  padding: 2rem;
}
</style>
