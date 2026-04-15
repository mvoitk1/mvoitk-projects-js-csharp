<script setup lang="ts">
import { onMounted } from 'vue'
import { useRoute } from 'vue-router'
import { useOrdersStore } from '@/stores/orders'
import { useLocale } from '@/stores/locale'

const route = useRoute()
const orders = useOrdersStore()
const { t } = useLocale()

onMounted(() => orders.fetchOne(route.params.id as string))
</script>

<template>
  <div v-if="orders.loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

  <div v-else-if="!orders.current" class="empty">
    {{ t('Order not found.', 'Tellimust ei leitud.') }}
  </div>

  <div v-else class="order-detail">
    <div class="order-header">
      <h1>{{ t('Order', 'Tellimus') }} #{{ orders.current.orderNumber }}</h1>
      <span class="status-badge">{{ orders.current.status }}</span>
    </div>

    <div class="order-grid">
      <!-- Shipping -->
      <section class="card">
        <h2>{{ t('Shipping Details', 'Tarneandmed') }}</h2>
        <p>{{ orders.current.shippingFirstName }} {{ orders.current.shippingLastName }}</p>
        <p>{{ orders.current.shippingEmail }}</p>
        <p>{{ orders.current.shippingPhone }}</p>
        <p>{{ orders.current.shippingStreet }}</p>
        <p>{{ orders.current.shippingPostalCode }} {{ orders.current.shippingCity }}</p>
        <p>{{ orders.current.shippingCountry }}</p>
      </section>

      <!-- Summary -->
      <section class="card">
        <h2>{{ t('Summary', 'Kokkuvõte') }}</h2>
        <div class="summary-row">
          <span>{{ t('Date', 'Kuupäev') }}</span>
          <span>{{ new Date(orders.current.createdAt).toLocaleDateString() }}</span>
        </div>
        <div class="summary-row">
          <span>{{ t('Total', 'Kokku') }}</span>
          <span>€{{ orders.current.totalAmount.toFixed(2) }}</span>
        </div>
      </section>
    </div>

    <!-- Line items -->
    <section class="items-section">
      <h2>{{ t('Items', 'Kaubad') }}</h2>
      <table class="items-table">
        <thead>
          <tr>
            <th>{{ t('Product', 'Toode') }}</th>
            <th>{{ t('SKU', 'SKU') }}</th>
            <th>{{ t('Color', 'Värv') }}</th>
            <th>{{ t('Size', 'Suurus') }}</th>
            <th>{{ t('Qty', 'Kogus') }}</th>
            <th>{{ t('Unit Price', 'Ühiku hind') }}</th>
            <th>{{ t('Total', 'Kokku') }}</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in orders.current.items" :key="item.id">
            <td>{{ item.productName }}</td>
            <td class="mono">{{ item.sku }}</td>
            <td>{{ item.colorName }}</td>
            <td>{{ item.sizeCode }}</td>
            <td>{{ item.quantity }}</td>
            <td>€{{ item.unitPrice.toFixed(2) }}</td>
            <td>€{{ item.lineTotal.toFixed(2) }}</td>
          </tr>
        </tbody>
      </table>
    </section>
  </div>
</template>

<style scoped>
.order-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.order-header h1 {
  font-size: 1.75rem;
}

.status-badge {
  display: inline-block;
  padding: 0.3rem 0.8rem;
  border-radius: 20px;
  background: #f0f0ec;
  font-size: 0.85rem;
  font-weight: 600;
  color: #555;
}

.order-grid {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 1.5rem;
  margin-bottom: 2rem;
}

@media (max-width: 600px) {
  .order-grid {
    grid-template-columns: 1fr;
  }
}

.card {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1.25rem;
}

.card h2 {
  font-size: 0.85rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #888;
  margin-bottom: 0.75rem;
}

.card p {
  font-size: 0.9rem;
  color: #444;
  line-height: 1.6;
}

.summary-row {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
  padding: 0.35rem 0;
  color: #555;
}

.items-section h2 {
  font-size: 1.1rem;
  margin-bottom: 1rem;
}

.items-table {
  width: 100%;
  border-collapse: collapse;
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  overflow: hidden;
}

.items-table th,
.items-table td {
  padding: 0.75rem 1rem;
  text-align: left;
  font-size: 0.9rem;
  border-bottom: 1px solid #f0f0ec;
}

.items-table th {
  background: #f8f8f6;
  font-weight: 600;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #888;
}

.items-table tr:last-child td {
  border-bottom: none;
}

.mono {
  font-family: monospace;
  font-size: 0.85rem;
}

.loading,
.empty {
  text-align: center;
  color: #888;
  padding: 3rem;
}
</style>
