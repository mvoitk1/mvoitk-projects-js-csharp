import { ref, computed } from 'vue'
import { defineStore } from 'pinia'
import { getCart, addToCart, updateCartItem, removeCartItem } from '@/api/cart'
import type { CartDto, AddToCartDto } from '@/types'

export const useCartStore = defineStore('cart', () => {
  const cart = ref<CartDto | null>(null)
  const loading = ref(false)

  const itemCount = computed(() => cart.value?.itemCount ?? 0)

  async function fetchCart() {
    loading.value = true
    try {
      cart.value = await getCart()
    } finally {
      loading.value = false
    }
  }

  async function addItem(payload: AddToCartDto) {
    cart.value = await addToCart(payload)
  }

  async function updateItem(cartItemId: string, quantity: number) {
    cart.value = await updateCartItem(cartItemId, { quantity })
  }

  async function removeItem(cartItemId: string) {
    await removeCartItem(cartItemId)
    await fetchCart()
  }

  function clear() {
    cart.value = null
  }

  return { cart, loading, itemCount, fetchCart, addItem, updateItem, removeItem, clear }
})
