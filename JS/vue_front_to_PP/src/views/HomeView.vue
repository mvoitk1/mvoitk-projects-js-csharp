<script setup lang="ts">
import { onMounted } from 'vue'
import { useCollectionsStore } from '@/stores/collections'
import { useLocale } from '@/stores/locale'

const collectionsStore = useCollectionsStore()
const { t } = useLocale()

onMounted(() => collectionsStore.fetchCollections())
</script>

<template>
  <div class="home">
    <section class="hero">
      <h1>{{ t('New Season', 'Uus Hooaeg') }}</h1>
      <p>{{ t('Explore our latest collections.', 'Tutvu meie uusimate kollektsioonidega.') }}</p>
      <RouterLink :to="{ name: 'products' }" class="btn-primary">
        {{ t('Shop Now', 'Osta Kohe') }}
      </RouterLink>
    </section>

    <section class="collections">
      <h2>{{ t('Collections', 'Kollektsioonid') }}</h2>

      <div v-if="collectionsStore.loading" class="loading">
        {{ t('Loading…', 'Laen…') }}
      </div>

      <div v-else-if="collectionsStore.list.length === 0" class="empty">
        {{ t('No collections available.', 'Kollektsioone pole saadaval.') }}
      </div>

      <div v-else class="collections__grid">
        <RouterLink
          v-for="col in collectionsStore.list"
          :key="col.id"
          :to="{ name: 'products', query: { collectionId: col.id } }"
          class="collection-card"
        >
          <h3>{{ col.name }}</h3>
          <p v-if="col.description">{{ col.description }}</p>
          <span class="collection-card__cta">{{ t('Shop Now →', 'Osta Kohe →') }}</span>
        </RouterLink>
      </div>
    </section>
  </div>
</template>

<style scoped>
.hero {
  text-align: center;
  padding: 4rem 1rem;
  background: #1a1a1a;
  color: #fff;
  border-radius: 12px;
  margin-bottom: 3rem;
}

.hero h1 {
  font-size: 3rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
}

.hero p {
  font-size: 1.1rem;
  color: #ccc;
  margin-bottom: 1.5rem;
}

.btn-primary {
  display: inline-block;
  background: #fff;
  color: #1a1a1a;
  padding: 0.75rem 1.75rem;
  border-radius: 6px;
  font-weight: 600;
  transition: background 0.2s;
}

.btn-primary:hover {
  background: #e8e8e4;
}

.collections h2 {
  font-size: 1.5rem;
  margin-bottom: 1.5rem;
}

.collections__grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1.5rem;
}

.collection-card {
  display: block;
  padding: 1.5rem;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  background: #fff;
  transition: box-shadow 0.2s;
}

.collection-card:hover {
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
}

.collection-card h3 {
  font-size: 1.1rem;
  margin-bottom: 0.4rem;
}

.collection-card p {
  font-size: 0.9rem;
  color: #666;
  margin-bottom: 0.75rem;
}

.collection-card__cta {
  font-size: 0.85rem;
  font-weight: 600;
}

.loading,
.empty {
  color: #888;
  text-align: center;
  padding: 2rem;
}
</style>
