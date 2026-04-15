import { ref } from 'vue'
import { defineStore } from 'pinia'

type Locale = 'en' | 'et'

export const useLocaleStore = defineStore('locale', () => {
  const stored = localStorage.getItem('locale') as Locale | null
  const locale = ref<Locale>(stored === 'et' ? 'et' : 'en')

  function setLocale(l: Locale) {
    locale.value = l
    localStorage.setItem('locale', l)
  }

  function toggle() {
    setLocale(locale.value === 'en' ? 'et' : 'en')
  }

  return { locale, setLocale, toggle }
})

export function useLocale() {
  const store = useLocaleStore()

  function t(en: string, et?: string | null): string {
    if (store.locale === 'et' && et) return et
    return en
  }

  return { locale: store.locale, t, toggle: store.toggle }
}
