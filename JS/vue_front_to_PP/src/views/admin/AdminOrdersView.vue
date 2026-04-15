<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { adminGetOrders, adminUpdateOrderStatus } from '@/api/admin'
import type { AdminOrderDto } from '@/types'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const { t } = useLocale()
const { show } = useToast()

const orders = ref<AdminOrderDto[]>([])
const loading = ref(false)
const selectedOrder = ref<AdminOrderDto | null>(null)
const statusFilter = ref('')

const ORDER_STATUSES = ['Pending', 'Processing', 'Shipped', 'Delivered', 'Cancelled']

async function load() {
  loading.value = true
  try {
    orders.value = await adminGetOrders(statusFilter.value || undefined)
  } catch (e) {
    show((e as Error).message, 'error')
  } finally {
    loading.value = false
  }
}

async function updateStatus(orderId: string, status: string) {
  try {
    await adminUpdateOrderStatus(orderId, { status })
    show(t('Status updated.', 'Staatus uuendatud.'), 'success')
    await load()
    if (selectedOrder.value?.id === orderId) {
      selectedOrder.value = orders.value.find((o) => o.id === orderId) ?? null
    }
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

onMounted(load)
</script>

<template>
  <div>
    <div class="page-header">
      <h1>{{ t('Orders', 'Tellimused') }}</h1>
      <div class="filters">
        <select v-model="statusFilter" @change="load">
          <option value="">{{ t('All Statuses', 'Kõik staatused') }}</option>
          <option v-for="s in ORDER_STATUSES" :key="s" :value="s">{{ s }}</option>
        </select>
      </div>
    </div>

    <div class="orders-layout">
      <div>
        <div v-if="loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

        <table v-else class="admin-table">
          <thead>
            <tr>
              <th>{{ t('Order #', 'Tellimus #') }}</th>
              <th>{{ t('Customer', 'Klient') }}</th>
              <th>{{ t('Date', 'Kuupäev') }}</th>
              <th>{{ t('Total', 'Kokku') }}</th>
              <th>{{ t('Status', 'Staatus') }}</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            <tr
              v-for="o in orders"
              :key="o.id"
              class="clickable-row"
              :class="{ 'row-selected': selectedOrder?.id === o.id }"
              @click="selectedOrder = o"
            >
              <td class="mono">{{ o.orderNumber }}</td>
              <td>{{ o.customerFirstName }} {{ o.customerLastName }}</td>
              <td>{{ new Date(o.createdAt).toLocaleDateString() }}</td>
              <td>€{{ o.totalAmount.toFixed(2) }}</td>
              <td><span class="status-badge">{{ o.status }}</span></td>
              <td>
                <select
                  :value="o.status ?? ''"
                  class="status-select"
                  @click.stop
                  @change="updateStatus(o.id, ($event.target as HTMLSelectElement).value)"
                >
                  <option v-for="s in ORDER_STATUSES" :key="s" :value="s">{{ s }}</option>
                </select>
              </td>
            </tr>
            <tr v-if="orders.length === 0">
              <td colspan="6" class="empty-cell">{{ t('No orders.', 'Tellimusi pole.') }}</td>
            </tr>
          </tbody>
        </table>
      </div>

      <!-- Detail panel -->
      <div v-if="selectedOrder" class="detail-panel">
        <button class="close-btn" @click="selectedOrder = null">✕</button>
        <h2>{{ t('Order', 'Tellimus') }} #{{ selectedOrder.orderNumber }}</h2>
        <p class="detail-meta">{{ new Date(selectedOrder.createdAt).toLocaleString() }}</p>

        <div class="detail-section">
          <h3>{{ t('Customer', 'Klient') }}</h3>
          <p>{{ selectedOrder.customerFirstName }} {{ selectedOrder.customerLastName }}</p>
          <p>{{ selectedOrder.customerEmail }}</p>
        </div>

        <div class="detail-section">
          <h3>{{ t('Shipping', 'Tarneaadress') }}</h3>
          <p>{{ selectedOrder.shippingFirstName }} {{ selectedOrder.shippingLastName }}</p>
          <p>{{ selectedOrder.shippingStreet }}</p>
          <p>{{ selectedOrder.shippingPostalCode }} {{ selectedOrder.shippingCity }}</p>
          <p>{{ selectedOrder.shippingCountry }}</p>
          <p>{{ selectedOrder.shippingPhone }}</p>
        </div>

        <div class="detail-section">
          <h3>{{ t('Items', 'Kaubad') }}</h3>
          <div v-for="item in selectedOrder.items" :key="item.id" class="detail-item">
            <span>{{ item.productName }}</span>
            <span>× {{ item.quantity }}</span>
            <span>€{{ item.lineTotal.toFixed(2) }}</span>
          </div>
          <div class="detail-total">
            <span>{{ t('Total', 'Kokku') }}</span>
            <span>€{{ selectedOrder.totalAmount.toFixed(2) }}</span>
          </div>
        </div>
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

.filters select {
  padding: 0.4rem 0.6rem;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 0.9rem;
}

.orders-layout {
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 1.5rem;
  align-items: start;
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
}

.clickable-row:hover {
  background: #fafaf8;
}

.row-selected {
  background: #f0f4ff !important;
}

.mono {
  font-family: monospace;
  font-size: 0.85rem;
}

.status-badge {
  display: inline-block;
  padding: 0.15rem 0.5rem;
  background: #f0f0ec;
  border-radius: 12px;
  font-size: 0.78rem;
  font-weight: 600;
  color: #555;
}

.status-select {
  padding: 0.25rem 0.4rem;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.82rem;
}

.empty-cell {
  text-align: center;
  color: #aaa;
}

.detail-panel {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1.5rem;
  width: 300px;
  position: sticky;
  top: 80px;
}

.detail-panel h2 {
  font-size: 1rem;
  font-weight: 700;
  margin-bottom: 0.25rem;
}

.detail-meta {
  font-size: 0.8rem;
  color: #888;
  margin-bottom: 1rem;
}

.detail-section {
  margin-bottom: 1rem;
  border-top: 1px solid #f0f0ec;
  padding-top: 0.75rem;
}

.detail-section h3 {
  font-size: 0.78rem;
  text-transform: uppercase;
  letter-spacing: 0.05em;
  color: #aaa;
  margin-bottom: 0.4rem;
}

.detail-section p {
  font-size: 0.85rem;
  color: #444;
  line-height: 1.5;
}

.detail-item {
  display: flex;
  gap: 0.5rem;
  font-size: 0.85rem;
  padding: 0.3rem 0;
}

.detail-item span:first-child {
  flex: 1;
}

.detail-item span:last-child {
  font-weight: 600;
}

.detail-total {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
  font-weight: 700;
  border-top: 1px solid #eee;
  margin-top: 0.5rem;
  padding-top: 0.5rem;
}

.close-btn {
  float: right;
  background: none;
  border: none;
  font-size: 1rem;
  color: #aaa;
  margin-top: -0.25rem;
}

.close-btn:hover {
  color: #555;
}

.loading {
  color: #888;
  padding: 2rem;
}
</style>
