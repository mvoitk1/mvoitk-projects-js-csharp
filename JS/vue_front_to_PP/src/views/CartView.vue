<script setup lang="ts">
import { onMounted } from 'vue'
import { useCartStore } from '@/stores/cart'
import { useLocale } from '@/stores/locale'
import { useToast } from '@/composables/useToast'

const cart = useCartStore()
const { t } = useLocale()
const { show } = useToast()

async function updateQty(itemId: string, qty: number) {
  try {
    await cart.updateItem(itemId, qty)
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

async function remove(itemId: string) {
  try {
    await cart.removeItem(itemId)
  } catch (e) {
    show((e as Error).message, 'error')
  }
}

onMounted(() => cart.fetchCart())
</script>

<template>
  <div>
    <h1>{{ t('Your Cart', 'Sinu ostukorv') }}</h1>

    <div v-if="cart.loading" class="loading">{{ t('Loading…', 'Laen…') }}</div>

    <div v-else-if="!cart.cart || cart.cart.items?.length === 0" class="empty">
      <p>{{ t('Your cart is empty.', 'Ostukorv on tühi.') }}</p>
      <RouterLink :to="{ name: 'products' }" class="btn-primary">
        {{ t('Browse Products', 'Sirvi tooteid') }}
      </RouterLink>
    </div>

    <div v-else class="cart-layout">
      <div class="cart-items">
        <div v-for="item in cart.cart.items" :key="item.id" class="cart-row">
          <img
            v-if="item.imageUrl"
            :src="item.imageUrl"
            :alt="item.productName ?? ''"
            class="cart-row__img"
          />
          <div v-else class="cart-row__img-placeholder" />

          <div class="cart-row__info">
            <p class="cart-row__name">{{ item.productName }}</p>
            <p class="cart-row__meta">{{ item.colorName }} · {{ item.sizeCode }}</p>
            <p class="cart-row__sku">SKU: {{ item.sku }}</p>
          </div>

          <div class="cart-row__qty">
            <button class="qty-btn" :disabled="item.quantity <= 1" @click="updateQty(item.id, item.quantity - 1)">−</button>
            <span>{{ item.quantity }}</span>
            <button class="qty-btn" :disabled="item.quantity >= 100" @click="updateQty(item.id, item.quantity + 1)">+</button>
          </div>

          <div class="cart-row__price">
            €{{ item.lineTotal.toFixed(2) }}
          </div>

          <button class="remove-btn" :title="t('Remove', 'Eemalda')" @click="remove(item.id)">✕</button>
        </div>
      </div>

      <div class="cart-summary">
        <h2>{{ t('Summary', 'Kokkuvõte') }}</h2>
        <div class="summary-row">
          <span>{{ t('Items', 'Kaupu') }}</span>
          <span>{{ cart.cart.itemCount }}</span>
        </div>
        <div class="summary-row summary-row--total">
          <span>{{ t('Total', 'Kokku') }}</span>
          <span>€{{ cart.cart.total.toFixed(2) }}</span>
        </div>
        <RouterLink :to="{ name: 'checkout' }" class="btn-checkout">
          {{ t('Proceed to Checkout', 'Mine kassasse') }}
        </RouterLink>
      </div>
    </div>
  </div>
</template>

<style scoped>
h1 {
  font-size: 1.75rem;
  margin-bottom: 1.5rem;
}

.cart-layout {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 2rem;
  align-items: start;
}

@media (max-width: 700px) {
  .cart-layout {
    grid-template-columns: 1fr;
  }
}

.cart-items {
  display: flex;
  flex-direction: column;
  gap: 1px;
  background: #e8e8e4;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  overflow: hidden;
}

.cart-row {
  display: flex;
  align-items: center;
  gap: 1rem;
  padding: 1rem;
  background: #fff;
}

.cart-row__img {
  width: 72px;
  height: 90px;
  object-fit: cover;
  border-radius: 4px;
  flex-shrink: 0;
}

.cart-row__img-placeholder {
  width: 72px;
  height: 90px;
  background: #f0f0ec;
  border-radius: 4px;
  flex-shrink: 0;
}

.cart-row__info {
  flex: 1;
  min-width: 0;
}

.cart-row__name {
  font-weight: 600;
  font-size: 0.95rem;
}

.cart-row__meta {
  font-size: 0.85rem;
  color: #666;
}

.cart-row__sku {
  font-size: 0.75rem;
  color: #999;
}

.cart-row__qty {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  font-size: 0.9rem;
}

.qty-btn {
  width: 28px;
  height: 28px;
  border: 1px solid #ddd;
  border-radius: 4px;
  background: #fff;
  font-size: 1rem;
  display: flex;
  align-items: center;
  justify-content: center;
}

.qty-btn:hover:not(:disabled) {
  border-color: #aaa;
}

.qty-btn:disabled {
  opacity: 0.3;
  cursor: not-allowed;
}

.cart-row__price {
  font-weight: 600;
  min-width: 70px;
  text-align: right;
}

.remove-btn {
  background: none;
  border: none;
  font-size: 0.9rem;
  color: #aaa;
  padding: 0.25rem;
}

.remove-btn:hover {
  color: #c0392b;
}

.cart-summary {
  background: #fff;
  border: 1px solid #e8e8e4;
  border-radius: 8px;
  padding: 1.5rem;
  position: sticky;
  top: 80px;
}

.cart-summary h2 {
  font-size: 1.1rem;
  margin-bottom: 1rem;
}

.summary-row {
  display: flex;
  justify-content: space-between;
  font-size: 0.9rem;
  padding: 0.4rem 0;
  color: #555;
}

.summary-row--total {
  font-size: 1.1rem;
  font-weight: 700;
  color: #1a1a1a;
  border-top: 1px solid #eee;
  margin-top: 0.5rem;
  padding-top: 0.75rem;
}

.btn-checkout {
  display: block;
  text-align: center;
  background: #1a1a1a;
  color: #fff;
  padding: 0.75rem;
  border-radius: 6px;
  font-weight: 600;
  margin-top: 1.25rem;
}

.btn-checkout:hover {
  background: #333;
}

.btn-primary {
  display: inline-block;
  background: #1a1a1a;
  color: #fff;
  padding: 0.75rem 1.5rem;
  border-radius: 6px;
  font-weight: 600;
  margin-top: 1rem;
}

.loading,
.empty {
  text-align: center;
  color: #888;
  padding: 3rem;
}
</style>
