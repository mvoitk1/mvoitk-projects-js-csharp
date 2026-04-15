<script setup lang="ts">
import { useLocale } from '@/stores/locale'

const { t } = useLocale()
</script>

<template>
  <div class="admin-layout">
    <aside class="admin-sidebar">
      <p class="admin-sidebar__label">{{ t('Admin', 'Admin') }}</p>
      <nav>
        <RouterLink :to="{ name: 'admin-products' }">
          {{ t('Products', 'Tooted') }}
        </RouterLink>
        <RouterLink :to="{ name: 'admin-categories' }">
          {{ t('Categories', 'Kategooriad') }}
        </RouterLink>
        <RouterLink :to="{ name: 'admin-collections' }">
          {{ t('Collections', 'Kollektsioonid') }}
        </RouterLink>
        <RouterLink :to="{ name: 'admin-orders' }">
          {{ t('Orders', 'Tellimused') }}
        </RouterLink>
      </nav>
    </aside>

    <div class="admin-content">
      <RouterView />
    </div>
  </div>
</template>

<style scoped>
.admin-layout {
  display: grid;
  grid-template-columns: 180px 1fr;
  gap: 2rem;
  align-items: start;
  min-height: 60vh;
}

.admin-sidebar {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1rem;
  position: sticky;
  top: 80px;
}

.admin-sidebar__label {
  font-size: 0.75rem;
  text-transform: uppercase;
  letter-spacing: 0.06em;
  color: #aaa;
  margin-bottom: 0.75rem;
  font-weight: 600;
}

.admin-sidebar nav {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.admin-sidebar nav a {
  display: block;
  padding: 0.45rem 0.6rem;
  border-radius: 5px;
  font-size: 0.9rem;
  color: #555;
  transition: all 0.15s;
}

.admin-sidebar nav a:hover,
.admin-sidebar nav a.router-link-active {
  background: #f0f0ec;
  color: #1a1a1a;
  font-weight: 600;
}
</style>
