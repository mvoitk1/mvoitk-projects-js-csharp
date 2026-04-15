import { ref } from 'vue'
import { defineStore } from 'pinia'
import { getCollections } from '@/api/collections'
import type { CollectionDto } from '@/types'

export const useCollectionsStore = defineStore('collections', () => {
  const list = ref<CollectionDto[]>([])
  const loading = ref(false)

  async function fetchCollections() {
    loading.value = true
    try {
      list.value = await getCollections()
    } finally {
      loading.value = false
    }
  }

  return { list, loading, fetchCollections }
})
