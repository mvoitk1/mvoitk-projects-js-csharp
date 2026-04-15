import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getOrders, getOrder, createOrder } from '@/api/orders'
import type { OrderListItemDto, OrderDto, CreateOrderDto } from '@/types'

export const useOrdersStore = defineStore('orders', () => {
  const list = ref<OrderListItemDto[]>([])
  const current = ref<OrderDto | null>(null)
  const loading = ref(false)

  async function fetchOrders() {
    loading.value = true
    try {
      list.value = await getOrders()
    } finally {
      loading.value = false
    }
  }

  async function fetchOne(id: string) {
    loading.value = true
    try {
      current.value = await getOrder(id)
    } finally {
      loading.value = false
    }
  }

  async function placeOrder(payload: CreateOrderDto): Promise<OrderDto> {
    loading.value = true
    try {
      const order = await createOrder(payload)
      current.value = order
      return order
    } finally {
      loading.value = false
    }
  }

  return { list, current, loading, fetchOrders, fetchOne, placeOrder }
})
