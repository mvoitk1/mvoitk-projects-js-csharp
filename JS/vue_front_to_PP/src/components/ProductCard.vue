<script setup lang="ts">
import type { ProductListItemDto } from '@/types'

defineProps<{ product: ProductListItemDto }>()
</script>

<template>
  <RouterLink :to="{ name: 'product-detail', params: { id: product.id } }" class="card">
    <div class="card__image">
      <img
        v-if="product.primaryImageUrl"
        :src="product.primaryImageUrl"
        :alt="product.name ?? ''"
        loading="lazy"
      />
      <div v-else class="card__image-placeholder" />
    </div>
    <div class="card__body">
      <p v-if="product.collectionName" class="card__collection">{{ product.collectionName }}</p>
      <h3 class="card__name">{{ product.name }}</h3>
      <p v-if="product.lowestPrice != null" class="card__price">
        From €{{ product.lowestPrice.toFixed(2) }}
      </p>
    </div>
  </RouterLink>
</template>

<style scoped>
.card {
  display: flex;
  flex-direction: column;
  border-radius: 8px;
  overflow: hidden;
  background: #fff;
  border: 1px solid #eee;
  transition: box-shadow 0.2s;
}

.card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
}

.card__image {
  aspect-ratio: 3 / 4;
  overflow: hidden;
  background: #f0f0ec;
}

.card__image img {
  width: 100%;
  height: 100%;
  object-fit: cover;
}

.card__image-placeholder {
  width: 100%;
  height: 100%;
  background: #e8e8e4;
}

.card__body {
  padding: 0.75rem;
}

.card__collection {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #888;
  margin-bottom: 0.25rem;
}

.card__name {
  font-size: 0.95rem;
  font-weight: 600;
  margin-bottom: 0.25rem;
}

.card__price {
  font-size: 0.9rem;
  color: #444;
}
</style>
