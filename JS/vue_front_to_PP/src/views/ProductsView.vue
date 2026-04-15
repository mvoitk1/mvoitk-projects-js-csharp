<script setup lang="ts">
import { ref, watch, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProductsStore } from '@/stores/products'
import { useCategoriesStore } from '@/stores/categories'
import { useCollectionsStore } from '@/stores/collections'
import { useLocale } from '@/stores/locale'
import ProductCard from '@/components/ProductCard.vue'

const route = useRoute()
const router = useRouter()
const productsStore = useProductsStore()
const categoriesStore = useCategoriesStore()
const collectionsStore = useCollectionsStore()
const { t } = useLocale()

const GENDERS = ['Male', 'Female', 'Unisex']

const selectedCategory = ref<string>((route.query.categoryId as string) ?? '')
const selectedCollection = ref<string>((route.query.collectionId as string) ?? '')
const selectedGender = ref<string>((route.query.gender as string) ?? '')

function applyFilters() {
  const query: Record<string, string> = {}
  if (selectedCategory.value) query.categoryId = selectedCategory.value
  if (selectedCollection.value) query.collectionId = selectedCollection.value
  if (selectedGender.value) query.gender = selectedGender.value
  router.replace({ name: 'products', query })
  load()
}

function clearFilters() {
  selectedCategory.value = ''
  selectedCollection.value = ''
  selectedGender.value = ''
  applyFilters()
}

function load() {
  const filters: Record<string, string> = {}
  if (selectedCategory.value) filters.categoryId = selectedCategory.value
  if (selectedCollection.value) filters.collectionId = selectedCollection.value
  if (selectedGender.value) filters.gender = selectedGender.value
  productsStore.fetchList(filters)
}

const hasFilters = computed(
  () => !!(selectedCategory.value || selectedCollection.value || selectedGender.value),
)

// Flatten category tree for select
function flattenCategories(
  cats: typeof categoriesStore.tree,
  depth = 0,
): { id: string; label: string }[] {
  return cats.flatMap((c) => [
    { id: c.id, label: '  '.repeat(depth) + (c.name ?? c.id) },
    ...flattenCategories(c.subCategories ?? [], depth + 1),
  ])
}
const flatCategories = computed(() => flattenCategories(categoriesStore.tree))

onMounted(() => {
  if (categoriesStore.tree.length === 0) categoriesStore.fetchCategories()
  if (collectionsStore.list.length === 0) collectionsStore.fetchCollections()
  load()
})
</script>

<template>
  <div class="products-page">
    <aside class="sidebar">
      <h2>{{ t('Filters', 'Filtrid') }}</h2>

      <div class="filter-group">
        <label>{{ t('Category', 'Kategooria') }}</label>
        <select v-model="selectedCategory">
          <option value="">{{ t('All', 'Kõik') }}</option>
          <option v-for="cat in flatCategories" :key="cat.id" :value="cat.id">
            {{ cat.label }}
          </option>
        </select>
      </div>

      <div class="filter-group">
        <label>{{ t('Collection', 'Kollektsioon') }}</label>
        <select v-model="selectedCollection">
          <option value="">{{ t('All', 'Kõik') }}</option>
          <option v-for="col in collectionsStore.list" :key="col.id" :value="col.id">
            {{ col.name }}
          </option>
        </select>
      </div>

      <div class="filter-group">
        <label>{{ t('Gender', 'Sugu') }}</label>
        <select v-model="selectedGender">
          <option value="">{{ t('All', 'Kõik') }}</option>
          <option v-for="g in GENDERS" :key="g" :value="g">{{ g }}</option>
        </select>
      </div>

      <div class="filter-actions">
        <button class="btn-primary" @click="applyFilters">
          {{ t('Apply', 'Rakenda') }}
        </button>
        <button v-if="hasFilters" class="btn-ghost" @click="clearFilters">
          {{ t('Clear', 'Tühista') }}
        </button>
      </div>
    </aside>

    <section class="products-section">
      <div v-if="productsStore.loading" class="loading">
        {{ t('Loading products…', 'Laen tooteid…') }}
      </div>

      <div v-else-if="productsStore.list.length === 0" class="empty">
        {{ t('No products found.', 'Tooteid ei leitud.') }}
      </div>

      <div v-else class="products-grid">
        <ProductCard v-for="p in productsStore.list" :key="p.id" :product="p" />
      </div>
    </section>
  </div>
</template>

<style scoped>
.products-page {
  display: grid;
  grid-template-columns: 220px 1fr;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 700px) {
  .products-page {
    grid-template-columns: 1fr;
  }
}

.sidebar {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1.5rem;
  position: sticky;
  top: 80px;
}

.sidebar h2 {
  font-size: 1rem;
  margin-bottom: 1rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #888;
}

.filter-group {
  margin-bottom: 1rem;
}

.filter-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 0.3rem;
}

.filter-group select {
  width: 100%;
  padding: 0.4rem 0.6rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
  background: #fff;
}

.filter-actions {
  display: flex;
  gap: 0.5rem;
  margin-top: 1rem;
}

.btn-primary {
  flex: 1;
  background: #1a1a1a;
  color: #fff;
  border: none;
  padding: 0.5rem;
  border-radius: 4px;
  font-size: 0.9rem;
}

.btn-primary:hover {
  background: #333;
}

.btn-ghost {
  flex: 1;
  background: none;
  border: 1px solid #ddd;
  padding: 0.5rem;
  border-radius: 4px;
  font-size: 0.9rem;
}

.products-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
  gap: 1.25rem;
}

.loading,
.empty {
  text-align: center;
  color: #888;
  padding: 3rem;
}
</style>
