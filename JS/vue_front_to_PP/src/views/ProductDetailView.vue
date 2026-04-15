<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useProductsStore } from '@/stores/products'
import { useCartStore } from '@/stores/cart'
import { useAuthStore } from '@/stores/auth'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'
import type { ProductVariantDto } from '@/types'

const route = useRoute()
const router = useRouter()
const productsStore = useProductsStore()
const cartStore = useCartStore()
const auth = useAuthStore()
const { t } = useLocale()
const { show } = useToast()

const selectedVariantId = ref<string | null>(null)
const quantity = ref(1)
const activeImageIndex = ref(0)
const addingToCart = ref(false)

const product = computed(() => productsStore.current)

const sortedImages = computed(() =>
  [...(product.value?.images ?? [])].sort((a, b) => a.sortOrder - b.sortOrder),
)

// Group variants by color for display
const colorGroups = computed(() => {
  const variants = product.value?.variants?.filter((v) => v.isActive) ?? []
  const map = new Map<string, { colorId: string; colorName: string | null; colorHex: string | null; sizes: ProductVariantDto[] }>()
  for (const v of variants) {
    if (!map.has(v.colorId)) {
      map.set(v.colorId, { colorId: v.colorId, colorName: v.colorName, colorHex: v.colorHex, sizes: [] })
    }
    map.get(v.colorId)!.sizes.push(v)
  }
  return [...map.values()]
})

function selectVariant(v: ProductVariantDto) {
  selectedVariantId.value = v.id
}

const selectedVariant = computed(() =>
  product.value?.variants?.find((v) => v.id === selectedVariantId.value) ?? null,
)

async function addToCart() {
  if (!selectedVariantId.value) {
    show(t('Please select a size.', 'Palun vali suurus.'), 'error')
    return
  }
  if (!auth.isLoggedIn) {
    router.push({ name: 'login', query: { redirect: route.fullPath } })
    return
  }
  addingToCart.value = true
  try {
    await cartStore.addItem({ productVariantId: selectedVariantId.value, quantity: quantity.value })
    show(t('Added to cart!', 'Lisatud ostukorvi!'), 'success')
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    addingToCart.value = false
  }
}

onMounted(() => productsStore.fetchOne(route.params.id as string))
</script>

<template>
  <div v-if="productsStore.loading" class="loading">
    {{ t('Loading…', 'Laen…') }}
  </div>

  <div v-else-if="!product" class="empty">
    {{ t('Product not found.', 'Toodet ei leitud.') }}
  </div>

  <div v-else class="product-detail">
    <!-- Gallery -->
    <div class="gallery">
      <div class="gallery__main">
        <template v-if="sortedImages[activeImageIndex]">
          <img
            :src="sortedImages[activeImageIndex]!.url ?? ''"
            :alt="sortedImages[activeImageIndex]!.altText ?? product.name ?? ''"
          />
        </template>
        <div v-else class="gallery__placeholder" />
      </div>
      <div v-if="sortedImages.length > 1" class="gallery__thumbs">
        <button
          v-for="(img, i) in sortedImages"
          :key="img.id"
          class="gallery__thumb"
          :class="{ active: i === activeImageIndex }"
          @click="activeImageIndex = i"
        >
          <img :src="img.url ?? ''" :alt="img.altText ?? ''" />
        </button>
      </div>
    </div>

    <!-- Info -->
    <div class="info">
      <p v-if="product.collectionName" class="info__collection">{{ product.collectionName }}</p>
      <h1 class="info__name">{{ product.name }}</h1>

      <p v-if="selectedVariant" class="info__price">€{{ selectedVariant.price.toFixed(2) }}</p>
      <p v-else-if="product.variants?.length" class="info__price info__price--from">
        {{ t('From', 'Alates') }} €{{
          Math.min(...product.variants.map((v) => v.price)).toFixed(2)
        }}
      </p>

      <!-- Variant selector -->
      <div v-for="group in colorGroups" :key="group.colorId" class="variant-group">
        <div class="variant-group__color">
          <span
            v-if="group.colorHex"
            class="color-swatch"
            :style="{ background: group.colorHex }"
          />
          <span>{{ group.colorName ?? group.colorId }}</span>
        </div>
        <div class="variant-group__sizes">
          <button
            v-for="v in group.sizes"
            :key="v.id"
            class="size-btn"
            :class="{
              active: selectedVariantId === v.id,
              'out-of-stock': v.stockQty === 0,
            }"
            :disabled="v.stockQty === 0"
            @click="selectVariant(v)"
          >
            {{ v.sizeDisplayName ?? v.sizeCode }}
          </button>
        </div>
      </div>

      <!-- Quantity -->
      <div class="quantity-row">
        <label>{{ t('Qty', 'Kogus') }}</label>
        <input v-model.number="quantity" type="number" min="1" max="100" />
      </div>

      <button class="btn-add-to-cart" :disabled="addingToCart" @click="addToCart">
        <template v-if="addingToCart">{{ t('Adding…', 'Lisan…') }}</template>
        <template v-else>{{ t('Add to Cart', 'Lisa ostukorvi') }}</template>
      </button>

      <!-- Details -->
      <div v-if="product.description" class="info__section">
        <h3>{{ t('Description', 'Kirjeldus') }}</h3>
        <p>{{ product.description }}</p>
      </div>

      <div v-if="product.material" class="info__section">
        <h3>{{ t('Material', 'Materjal') }}</h3>
        <p>{{ product.material }}</p>
      </div>

      <div v-if="product.categoryNames?.length" class="info__section">
        <h3>{{ t('Categories', 'Kategooriad') }}</h3>
        <p>{{ product.categoryNames.join(', ') }}</p>
      </div>
    </div>
  </div>
</template>

<style scoped>
.product-detail {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 3rem;
  align-items: start;
}

@media (max-width: 700px) {
  .product-detail {
    grid-template-columns: 1fr;
  }
}

.gallery__main {
  aspect-ratio: 3 / 4;
  background: #f0f0ec;
  border-radius: 8px;
  overflow: hidden;
  margin-bottom: 0.75rem;
}

.gallery__main img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.gallery__placeholder {
  width: 100%;
  height: 100%;
  background: #e8e8e4;
}

.gallery__thumbs {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.gallery__thumb {
  width: 64px;
  height: 80px;
  border: 2px solid transparent;
  border-radius: 4px;
  overflow: hidden;
  padding: 0;
  background: none;
}

.gallery__thumb.active {
  border-color: #1a1a1a;
}

.gallery__thumb img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.info__collection {
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #888;
  margin-bottom: 0.4rem;
}

.info__name {
  font-size: 1.75rem;
  font-weight: 700;
  margin-bottom: 0.75rem;
}

.info__price {
  font-size: 1.4rem;
  font-weight: 600;
  margin-bottom: 1.25rem;
}

.info__price--from {
  color: #666;
}

.variant-group {
  margin-bottom: 1rem;
}

.variant-group__color {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
  font-weight: 600;
  margin-bottom: 0.4rem;
}

.color-swatch {
  width: 16px;
  height: 16px;
  border-radius: 50%;
  border: 1px solid #ddd;
  display: inline-block;
}

.variant-group__sizes {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

.size-btn {
  padding: 0.4rem 0.85rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: #fff;
  font-size: 0.85rem;
  transition: all 0.15s;
}

.size-btn:hover:not(:disabled) {
  border-color: #1a1a1a;
}

.size-btn.active {
  background: #1a1a1a;
  color: #fff;
  border-color: #1a1a1a;
}

.size-btn.out-of-stock {
  opacity: 0.35;
  cursor: not-allowed;
  text-decoration: line-through;
}

.quantity-row {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin: 1.25rem 0;
}

.quantity-row label {
  font-size: 0.9rem;
  font-weight: 600;
}

.quantity-row input {
  width: 70px;
  padding: 0.4rem 0.6rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.9rem;
}

.btn-add-to-cart {
  width: 100%;
  background: #1a1a1a;
  color: #fff;
  border: none;
  padding: 0.85rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  margin-bottom: 1.5rem;
  transition: background 0.2s;
}

.btn-add-to-cart:hover:not(:disabled) {
  background: #333;
}

.btn-add-to-cart:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.info__section {
  margin-top: 1.25rem;
  border-top: 1px solid #eee;
  padding-top: 1rem;
}

.info__section h3 {
  font-size: 0.85rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #888;
  margin-bottom: 0.4rem;
}

.loading,
.empty {
  text-align: center;
  color: #888;
  padding: 3rem;
}
</style>
