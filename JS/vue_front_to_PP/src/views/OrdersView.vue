<script setup lang="ts">
import { onMounted } from 'vue'
import { useOrdersStore } from '@/stores/orders'
import { useLocale } from '@/stores/locale'

const orders = useOrdersStore()
const { t } = useLocale()

onMounted(() => orders.fetchOrders())
</script>

<template>
  <div>
    <h1>{{ t('My Orders', 'Minu tellimused') }}</h1>

    <div v-if="orders.loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

    <div v-else-if="orders.list.length === 0" class="empty">
      {{ t('You have no orders yet.', 'Tellimusi pole veel.') }}
    </div>

    <table v-else class="orders-table">
      <thead>
        <tr>
          <th>{{ t('Order #', 'Tellimus #') }}</th>
          <th>{{ t('Date', 'Kuupäev') }}</th>
          <th>{{ t('Items', 'Kaupu') }}</th>
          <th>{{ t('Total', 'Kokku') }}</th>
          <th>{{ t('Status', 'Staatus') }}</th>
          <th></th>
        </tr>
      </thead>
      <tbody>
        <tr v-for="o in orders.list" :key="o.id">
          <td class="mono">{{ o.orderNumber }}</td>
          <td>{{ new Date(o.createdAt).toLocaleDateString() }}</td>
          <td>{{ o.itemCount }}</td>
          <td>€{{ o.totalAmount.toFixed(2) }}</td>
          <td><span class="status-badge">{{ o.status }}</span></td>
          <td>
            <RouterLink :to="{ name: 'order-detail', params: { id: o.id } }" class="view-link">
              {{ t('View', 'Vaata') }}
            </RouterLink>
          </td>
        </tr>
      </tbody>
    </table>
  </div>
</template>

<style scoped>
h1 {
  font-size: 1.75rem;
  margin-bottom: 1.5rem;
}

.orders-table {
  width: 100%;
  border-collapse: collapse;
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  overflow: hidden;
}

.orders-table th,
.orders-table td {
  padding: 0.85rem 1rem;
  text-align: left;
  font-size: 0.9rem;
  border-bottom: 1px solid #f0f0ec;
}

.orders-table th {
  background: #f8f8f6;
  font-weight: 600;
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 0.04em;
  color: #888;
}

.orders-table tr:last-child td {
  border-bottom: none;
}

.mono {
  font-family: monospace;
  font-size: 0.85rem;
}

.status-badge {
  display: inline-block;
  padding: 0.2rem 0.6rem;
  border-radius: 20px;
  background: #f0f0ec;
  font-size: 0.8rem;
  font-weight: 600;
  color: #555;
}

.view-link {
  font-size: 0.85rem;
  font-weight: 600;
  color: #1a1a1a;
  text-decoration: underline;
}

.loading,
.empty {
  text-align: center;
  color: #888;
  padding: 3rem;
}
</style>
