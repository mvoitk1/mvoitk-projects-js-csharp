<script setup lang="ts">
import { reactive, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useCartStore } from '@/stores/cart'
import { useOrdersStore } from '@/stores/orders'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const router = useRouter()
const cart = useCartStore()
const orders = useOrdersStore()
const { t } = useLocale()
const { show } = useToast()

const form = reactive({
  shippingFirstName: '',
  shippingLastName: '',
  shippingEmail: '',
  shippingPhone: '',
  shippingCountry: '',
  shippingCity: '',
  shippingStreet: '',
  shippingPostalCode: '',
})

async function submit() {
  try {
    const order = await orders.placeOrder({ ...form })
    cart.clear()
    show(t('Order placed!', 'Tellimus esitatud!'), 'success')
    router.push({ name: 'order-detail', params: { id: order.id } })
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

onMounted(() => {
  if (!cart.cart) cart.fetchCart()
})
</script>

<template>
  <div>
    <h1>{{ t('Checkout', 'Maksmine') }}</h1>

    <div class="checkout-layout">
      <form class="shipping-form" @submit.prevent="submit">
        <h2>{{ t('Shipping Address', 'Tarneaadress') }}</h2>

        <div class="form-row">
          <div class="form-group">
            <label>{{ t('First Name', 'Eesnimi') }}</label>
            <input v-model="form.shippingFirstName" type="text" required />
          </div>
          <div class="form-group">
            <label>{{ t('Last Name', 'Perekonnanimi') }}</label>
            <input v-model="form.shippingLastName" type="text" required />
          </div>
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>{{ t('Email', 'E-post') }}</label>
            <input v-model="form.shippingEmail" type="email" required />
          </div>
          <div class="form-group">
            <label>{{ t('Phone', 'Telefon') }}</label>
            <input v-model="form.shippingPhone" type="tel" required />
          </div>
        </div>

        <div class="form-group">
          <label>{{ t('Country', 'Riik') }}</label>
          <input v-model="form.shippingCountry" type="text" required />
        </div>

        <div class="form-row">
          <div class="form-group">
            <label>{{ t('City', 'Linn') }}</label>
            <input v-model="form.shippingCity" type="text" required />
          </div>
          <div class="form-group">
            <label>{{ t('Postal Code', 'Sihtnumber') }}</label>
            <input v-model="form.shippingPostalCode" type="text" required />
          </div>
        </div>

        <div class="form-group">
          <label>{{ t('Street', 'Tänav') }}</label>
          <input v-model="form.shippingStreet" type="text" required />
        </div>

        <button type="submit" class="btn-place-order" :disabled="orders.loading">
          <template v-if="orders.loading">{{ t('Placing…', 'Esitan…') }}</template>
          <template v-else>{{ t('Place Order', 'Esita tellimus') }}</template>
        </button>
      </form>

      <div class="order-summary">
        <h2>{{ t('Order Summary', 'Tellimuse kokkuvõte') }}</h2>
        <div v-if="cart.cart">
          <div v-for="item in cart.cart.items" :key="item.id" class="summary-item">
            <span class="summary-item__name">{{ item.productName }}</span>
            <span class="summary-item__meta">× {{ item.quantity }}</span>
            <span class="summary-item__price">€{{ item.lineTotal.toFixed(2) }}</span>
          </div>
          <div class="summary-total">
            <span>{{ t('Total', 'Kokku') }}</span>
            <span>€{{ cart.cart.total.toFixed(2) }}</span>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
h1 {
  font-size: 1.75rem;
  margin-bottom: 1.5rem;
}

.checkout-layout {
  display: grid;
  grid-template-columns: 1fr 320px;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 700px) {
  .checkout-layout {
    grid-template-columns: 1fr;
  }
}

.shipping-form h2,
.order-summary h2 {
  font-size: 1.1rem;
  margin-bottom: 1.25rem;
}

.form-row {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 0.75rem;
}

.form-group {
  margin-bottom: 1rem;
}

.form-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 600;
  margin-bottom: 0.3rem;
}

.form-group input {
  width: 100%;
  padding: 0.55rem 0.75rem;
  border: 1px solid #ddd;
  border-radius: 5px;
  font-size: 0.95rem;
}

.form-group input:focus {
  outline: none;
  border-color: #1a1a1a;
}

.btn-place-order {
  width: 100%;
  background: #1a1a1a;
  color: #fff;
  border: none;
  padding: 0.85rem;
  border-radius: 6px;
  font-size: 1rem;
  font-weight: 600;
  margin-top: 0.5rem;
}

.btn-place-order:hover:not(:disabled) {
  background: #333;
}

.btn-place-order:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

.order-summary {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1.5rem;
  position: sticky;
  top: 80px;
}

.summary-item {
  display: flex;
  gap: 0.5rem;
  font-size: 0.9rem;
  padding: 0.4rem 0;
  border-bottom: 1px solid #f0f0ec;
}

.summary-item__name {
  flex: 1;
}

.summary-item__meta {
  color: #888;
}

.summary-item__price {
  font-weight: 600;
  min-width: 60px;
  text-align: right;
}

.summary-total {
  display: flex;
  justify-content: space-between;
  font-size: 1rem;
  font-weight: 700;
  margin-top: 0.75rem;
  padding-top: 0.5rem;
}
</style>
