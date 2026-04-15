<script setup lang="ts">
import { ref, reactive, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  adminGetProduct,
  adminCreateProduct,
  adminUpdateProduct,
  adminAddVariant,
  adminUpdateVariant,
  adminDeleteVariant,
  adminAddImage,
  adminDeleteImage,
  adminUpdateStock,
} from '@/api/admin'
import { adminGetCollections, adminGetCategories } from '@/api/admin'
import type {
  AdminProductDto,
  AdminProductWriteDto,
  AdminVariantDto,
  AdminVariantWriteDto,
  AdminProductImageDto,
  AdminProductImageWriteDto,
  AdminCollectionDto,
  AdminCategoryDto,
} from '@/types'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const route = useRoute()
const router = useRouter()
const { t } = useLocale()
const { show } = useToast()

const isNew = computed(() => route.params.id === 'new')
const productId = computed(() => (isNew.value ? null : (route.params.id as string)))

const product = ref<AdminProductDto | null>(null)
const collections = ref<AdminCollectionDto[]>([])
const categories = ref<AdminCategoryDto[]>([])
const loading = ref(false)
const saving = ref(false)

const GENDERS = ['Male', 'Female', 'Unisex']

const form = reactive<AdminProductWriteDto>({
  nameEn: '',
  nameEt: null,
  descriptionEn: null,
  descriptionEt: null,
  materialEn: null,
  materialEt: null,
  gender: null,
  isActive: true,
  collectionId: null,
  categoryIds: [],
})

// ── Variant modal ──────────────────────────────────────────────────────────

const showVariantModal = ref(false)
const editingVariant = ref<AdminVariantDto | null>(null)
const variantForm = reactive<AdminVariantWriteDto>({
  sku: '',
  price: 0,
  unitPrice: 0,
  stockQty: 0,
  isActive: true,
  colorId: '',
  sizeId: '',
})
const savingVariant = ref(false)

function openAddVariant() {
  editingVariant.value = null
  Object.assign(variantForm, { sku: '', price: 0, unitPrice: 0, stockQty: 0, isActive: true, colorId: '', sizeId: '' })
  showVariantModal.value = true
}

function openEditVariant(v: AdminVariantDto) {
  editingVariant.value = v
  Object.assign(variantForm, { sku: v.sku ?? '', price: v.price, unitPrice: v.unitPrice, stockQty: v.stockQty, isActive: v.isActive, colorId: v.colorId, sizeId: v.sizeId })
  showVariantModal.value = true
}

async function saveVariant() {
  if (!productId.value) return
  savingVariant.value = true
  try {
    if (editingVariant.value) {
      await adminUpdateVariant(productId.value, editingVariant.value.id, { ...variantForm })
    } else {
      await adminAddVariant(productId.value, { ...variantForm })
    }
    show(t('Variant saved.', 'Variant salvestatud.'), 'success')
    showVariantModal.value = false
    await reloadProduct()
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    savingVariant.value = false
  }
}

async function deleteVariant(variantId: string) {
  if (!productId.value) return
  if (!confirm(t('Delete this variant?', 'Kustuta see variant?'))) return
  try {
    await adminDeleteVariant(productId.value, variantId)
    show(t('Variant deleted.', 'Variant kustutatud.'), 'success')
    await reloadProduct()
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

const stockEdits = ref<Record<string, number>>({})

async function saveStock(variantId: string) {
  const qty = stockEdits.value[variantId]
  if (qty === undefined) return
  try {
    await adminUpdateStock(variantId, { stockQty: qty })
    show(t('Stock updated.', 'Laoseis uuendatud.'), 'success')
    await reloadProduct()
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

// ── Image modal ────────────────────────────────────────────────────────────

const showImageModal = ref(false)
const imageForm = reactive<AdminProductImageWriteDto>({ url: '', altTextEn: null, altTextEt: null, sortOrder: 0 })
const savingImage = ref(false)

async function saveImage() {
  if (!productId.value) return
  savingImage.value = true
  try {
    await adminAddImage(productId.value, { ...imageForm })
    show(t('Image added.', 'Pilt lisatud.'), 'success')
    showImageModal.value = false
    await reloadProduct()
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    savingImage.value = false
  }
}

async function deleteImage(imageId: string) {
  if (!productId.value) return
  if (!confirm(t('Delete this image?', 'Kustuta see pilt?'))) return
  try {
    await adminDeleteImage(productId.value, imageId)
    show(t('Image deleted.', 'Pilt kustutatud.'), 'success')
    await reloadProduct()
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

// ── Product save ────────────────────────────────────────────────────────────

async function saveProduct() {
  saving.value = true
  try {
    if (isNew.value) {
      const created = await adminCreateProduct({ ...form })
      show(t('Product created.', 'Toode loodud.'), 'success')
      router.replace({ name: 'admin-product-detail', params: { id: created.id } })
    } else {
      await adminUpdateProduct(productId.value!, { ...form })
      show(t('Product updated.', 'Toode uuendatud.'), 'success')
    }
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    saving.value = false
  }
}

async function reloadProduct() {
  if (!productId.value) return
  product.value = await adminGetProduct(productId.value)
  if (product.value) {
    stockEdits.value = Object.fromEntries(
      (product.value.variants ?? []).map((v) => [v.id, v.stockQty]),
    )
  }
}

function populateForm(p: AdminProductDto) {
  form.nameEn = p.nameEn ?? ''
  form.nameEt = p.nameEt ?? null
  form.descriptionEn = p.descriptionEn ?? null
  form.descriptionEt = p.descriptionEt ?? null
  form.materialEn = p.materialEn ?? null
  form.materialEt = p.materialEt ?? null
  form.gender = p.gender ?? null
  form.isActive = p.isActive
  form.collectionId = p.collectionId ?? null
  form.categoryIds = p.categoryIds ?? []
}

onMounted(async () => {
  loading.value = true
  try {
    const [cols, cats] = await Promise.all([adminGetCollections(), adminGetCategories()])
    collections.value = cols
    categories.value = cats
    if (!isNew.value) {
      product.value = await adminGetProduct(productId.value!)
      populateForm(product.value)
      stockEdits.value = Object.fromEntries(
        (product.value.variants ?? []).map((v) => [v.id, v.stockQty]),
      )
    }
  } finally {
    loading.value = false
  }
})

function toggleCategory(id: string) {
  const ids = form.categoryIds ?? []
  const idx = ids.indexOf(id)
  if (idx === -1) ids.push(id)
  else ids.splice(idx, 1)
  form.categoryIds = ids
}
</script>

<template>
  <div v-if="loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

  <div v-else>
    <div class="page-header">
      <h1>{{ isNew ? t('New Product', 'Uus toode') : (form.nameEn || t('Edit Product', 'Muuda toodet')) }}</h1>
      <button class="btn-primary" :disabled="saving" @click="saveProduct">
        {{ saving ? t('Saving…', 'Salvestan…') : t('Save', 'Salvesta') }}
      </button>
    </div>

    <!-- Product form -->
    <div class="card section">
      <h2>{{ t('Product Details', 'Toote andmed') }}</h2>
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
      <div class="form-row">
        <div class="form-group">
          <label>{{ t('Description (EN)', 'Kirjeldus (EN)') }}</label>
          <textarea v-model="form.descriptionEn" rows="3" />
        </div>
        <div class="form-group">
          <label>{{ t('Description (ET)', 'Kirjeldus (ET)') }}</label>
          <textarea v-model="form.descriptionEt" rows="3" />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>{{ t('Material (EN)', 'Materjal (EN)') }}</label>
          <input v-model="form.materialEn" type="text" />
        </div>
        <div class="form-group">
          <label>{{ t('Material (ET)', 'Materjal (ET)') }}</label>
          <input v-model="form.materialEt" type="text" />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>{{ t('Gender', 'Sugu') }}</label>
          <select v-model="form.gender">
            <option :value="null">{{ t('— None —', '— Puudub —') }}</option>
            <option v-for="g in GENDERS" :key="g" :value="g">{{ g }}</option>
          </select>
        </div>
        <div class="form-group">
          <label>{{ t('Collection', 'Kollektsioon') }}</label>
          <select v-model="form.collectionId">
            <option :value="null">{{ t('— None —', '— Puudub —') }}</option>
            <option v-for="col in collections" :key="col.id" :value="col.id">{{ col.nameEn }}</option>
          </select>
        </div>
      </div>
      <div class="form-group">
        <label>{{ t('Categories', 'Kategooriad') }}</label>
        <div class="checkbox-group">
          <label v-for="cat in categories" :key="cat.id" class="checkbox-item">
            <input
              type="checkbox"
              :checked="form.categoryIds?.includes(cat.id)"
              @change="toggleCategory(cat.id)"
            />
            {{ cat.nameEn }}
          </label>
        </div>
      </div>
      <div class="form-group form-group--checkbox">
        <label>
          <input v-model="form.isActive" type="checkbox" />
          {{ t('Active', 'Aktiivne') }}
        </label>
      </div>
    </div>

    <!-- Variants (only for saved products) -->
    <div v-if="!isNew" class="card section">
      <div class="section-header">
        <h2>{{ t('Variants', 'Variandid') }}</h2>
        <button class="btn-sm" @click="openAddVariant">+ {{ t('Add Variant', 'Lisa variant') }}</button>
      </div>
      <table class="admin-table">
        <thead>
          <tr>
            <th>SKU</th>
            <th>{{ t('Color', 'Värv') }}</th>
            <th>{{ t('Size', 'Suurus') }}</th>
            <th>{{ t('Price', 'Hind') }}</th>
            <th>{{ t('Stock', 'Laoseis') }}</th>
            <th>{{ t('Active', 'Aktiivne') }}</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="v in product?.variants ?? []" :key="v.id">
            <td class="mono">{{ v.sku }}</td>
            <td>{{ v.colorName }}</td>
            <td>{{ v.sizeCode }}</td>
            <td>€{{ v.price.toFixed(2) }}</td>
            <td>
              <div class="stock-edit">
                <input
                  v-model.number="stockEdits[v.id]"
                  type="number"
                  min="0"
                  class="stock-input"
                />
                <button class="btn-sm" @click="saveStock(v.id)">{{ t('Set', 'Sea') }}</button>
              </div>
            </td>
            <td>
              <span :class="v.isActive ? 'badge-active' : 'badge-inactive'">
                {{ v.isActive ? t('Yes', 'Jah') : t('No', 'Ei') }}
              </span>
            </td>
            <td class="actions">
              <button class="btn-edit" @click="openEditVariant(v)">{{ t('Edit', 'Muuda') }}</button>
              <button class="btn-delete" @click="deleteVariant(v.id)">{{ t('Del', 'Kustuta') }}</button>
            </td>
          </tr>
          <tr v-if="!product?.variants?.length">
            <td colspan="7" class="empty-cell">{{ t('No variants.', 'Variante pole.') }}</td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Images (only for saved products) -->
    <div v-if="!isNew" class="card section">
      <div class="section-header">
        <h2>{{ t('Images', 'Pildid') }}</h2>
        <button class="btn-sm" @click="showImageModal = true">+ {{ t('Add Image', 'Lisa pilt') }}</button>
      </div>
      <div class="images-grid">
        <div v-for="img in product?.images ?? []" :key="img.id" class="image-item">
          <img :src="img.url ?? ''" :alt="img.altTextEn ?? ''" />
          <p class="image-item__alt">{{ img.altTextEn || '—' }}</p>
          <p class="image-item__sort">Order: {{ img.sortOrder }}</p>
          <button class="btn-delete-img" @click="deleteImage(img.id)">✕</button>
        </div>
        <div v-if="!product?.images?.length" class="empty-cell">
          {{ t('No images.', 'Pilte pole.') }}
        </div>
      </div>
    </div>

    <!-- Variant Modal -->
    <div v-if="showVariantModal" class="modal-overlay" @click.self="showVariantModal = false">
      <div class="modal">
        <h2>{{ editingVariant ? t('Edit Variant', 'Muuda varianti') : t('Add Variant', 'Lisa variant') }}</h2>
        <form @submit.prevent="saveVariant">
          <div class="form-group">
            <label>SKU *</label>
            <input v-model="variantForm.sku" type="text" required />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>{{ t('Price', 'Hind') }} *</label>
              <input v-model.number="variantForm.price" type="number" step="0.01" min="0" required />
            </div>
            <div class="form-group">
              <label>{{ t('Unit Price', 'Ühiku hind') }} *</label>
              <input v-model.number="variantForm.unitPrice" type="number" step="0.01" min="0" required />
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>{{ t('Stock Qty', 'Laoseis') }} *</label>
              <input v-model.number="variantForm.stockQty" type="number" min="0" required />
            </div>
            <div class="form-group form-group--checkbox">
              <label style="margin-top: 1.5rem;">
                <input v-model="variantForm.isActive" type="checkbox" />
                {{ t('Active', 'Aktiivne') }}
              </label>
            </div>
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>{{ t('Color ID', 'Värvi ID') }} *</label>
              <input v-model="variantForm.colorId" type="text" required placeholder="UUID" />
            </div>
            <div class="form-group">
              <label>{{ t('Size ID', 'Suuruse ID') }} *</label>
              <input v-model="variantForm.sizeId" type="text" required placeholder="UUID" />
            </div>
          </div>
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="showVariantModal = false">{{ t('Cancel', 'Tühista') }}</button>
            <button type="submit" class="btn-primary" :disabled="savingVariant">
              {{ savingVariant ? t('Saving…', 'Salvestan…') : t('Save', 'Salvesta') }}
            </button>
          </div>
        </form>
      </div>
    </div>

    <!-- Image Modal -->
    <div v-if="showImageModal" class="modal-overlay" @click.self="showImageModal = false">
      <div class="modal">
        <h2>{{ t('Add Image', 'Lisa pilt') }}</h2>
        <form @submit.prevent="saveImage">
          <div class="form-group">
            <label>{{ t('Image URL', 'Pildi URL') }} *</label>
            <input v-model="imageForm.url" type="url" required />
          </div>
          <div class="form-row">
            <div class="form-group">
              <label>{{ t('Alt Text (EN)', 'Alt tekst (EN)') }}</label>
              <input v-model="imageForm.altTextEn" type="text" />
            </div>
            <div class="form-group">
              <label>{{ t('Alt Text (ET)', 'Alt tekst (ET)') }}</label>
              <input v-model="imageForm.altTextEt" type="text" />
            </div>
          </div>
          <div class="form-group">
            <label>{{ t('Sort Order', 'Järjekord') }}</label>
            <input v-model.number="imageForm.sortOrder" type="number" min="0" />
          </div>
          <div class="modal-actions">
            <button type="button" class="btn-cancel" @click="showImageModal = false">{{ t('Cancel', 'Tühista') }}</button>
            <button type="submit" class="btn-primary" :disabled="savingImage">
              {{ savingImage ? t('Adding…', 'Lisan…') : t('Add', 'Lisa') }}
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
  padding: 0.5rem 1.25rem;
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

.btn-sm {
  background: none;
  border: 1px solid #ddd;
  padding: 0.3rem 0.7rem;
  border-radius: 4px;
  font-size: 0.82rem;
}

.btn-sm:hover {
  border-color: #aaa;
}

.card {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1.5rem;
}

.section {
  margin-bottom: 1.5rem;
}

.section-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 1rem;
}

.card h2,
.section-header h2 {
  font-size: 1rem;
  font-weight: 700;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.form-group {
  margin-bottom: 0.85rem;
}

.form-group label {
  display: block;
  font-size: 0.82rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.form-group input,
.form-group select,
.form-group textarea {
  width: 100%;
  padding: 0.45rem 0.65rem;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 0.88rem;
  font-family: inherit;
}

.form-group--checkbox label {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.checkbox-group {
  display: flex;
  flex-wrap: wrap;
  gap: 0.5rem;
}

.checkbox-item {
  display: flex;
  align-items: center;
  gap: 0.3rem;
  font-size: 0.85rem;
  background: #f8f8f6;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
}

.admin-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.88rem;
}

.admin-table th,
.admin-table td {
  padding: 0.6rem 0.75rem;
  text-align: left;
  border-bottom: 1px solid #f0f0ec;
}

.admin-table th {
  background: #f8f8f6;
  font-weight: 600;
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #888;
}

.mono {
  font-family: monospace;
  font-size: 0.82rem;
}

.stock-edit {
  display: flex;
  align-items: center;
  gap: 0.4rem;
}

.stock-input {
  width: 64px;
  padding: 0.25rem 0.4rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.85rem;
}

.badge-active {
  display: inline-block;
  padding: 0.1rem 0.45rem;
  background: #d4edda;
  color: #2d7a4f;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
}

.badge-inactive {
  display: inline-block;
  padding: 0.1rem 0.45rem;
  background: #f0f0ec;
  color: #888;
  border-radius: 12px;
  font-size: 0.75rem;
  font-weight: 600;
}

.actions {
  display: flex;
  gap: 0.4rem;
}

.btn-edit {
  background: none;
  border: 1px solid #ddd;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.78rem;
}

.btn-delete {
  background: none;
  border: 1px solid #f5c6c6;
  color: #c0392b;
  padding: 0.2rem 0.5rem;
  border-radius: 4px;
  font-size: 0.78rem;
}

.empty-cell {
  text-align: center;
  color: #aaa;
  padding: 1rem;
}

.images-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
}

.image-item {
  position: relative;
  width: 120px;
}

.image-item img {
  width: 120px;
  height: 150px;
  object-fit: cover;
  border-radius: 6px;
  border: 1px solid #eee;
}

.image-item__alt,
.image-item__sort {
  font-size: 0.75rem;
  color: #888;
  text-align: center;
  margin-top: 0.2rem;
}

.btn-delete-img {
  position: absolute;
  top: 4px;
  right: 4px;
  background: rgba(0, 0, 0, 0.55);
  border: none;
  border-radius: 50%;
  color: #fff;
  width: 20px;
  height: 20px;
  font-size: 0.7rem;
  display: flex;
  align-items: center;
  justify-content: center;
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
  max-width: 480px;
  max-height: 90vh;
  overflow-y: auto;
}

.modal h2 {
  font-size: 1.1rem;
  margin-bottom: 1.25rem;
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
