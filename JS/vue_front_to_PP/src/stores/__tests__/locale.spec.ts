import { describe, it, expect } from 'vitest'
import { useLocaleStore, useLocale } from '@/stores/locale'

describe('locale store', () => {
  it('defaults to en when nothing is stored', () => {
    expect(useLocaleStore().locale).toBe('en')
  })

  it('initialises from localStorage when set to et', () => {
    localStorage.setItem('locale', 'et')
    expect(useLocaleStore().locale).toBe('et')
  })

  it('setLocale persists to localStorage', () => {
    const store = useLocaleStore()
    store.setLocale('et')
    expect(store.locale).toBe('et')
    expect(localStorage.getItem('locale')).toBe('et')
  })

  it('toggle flips between en and et', () => {
    const store = useLocaleStore()
    expect(store.locale).toBe('en')
    store.toggle()
    expect(store.locale).toBe('et')
    store.toggle()
    expect(store.locale).toBe('en')
  })
})

describe('useLocale().t — ET fallback to EN', () => {
  it('returns EN in en locale', () => {
    const { t } = useLocale()
    expect(t('Hello', 'Tere')).toBe('Hello')
  })

  it('returns ET in et locale when ET is present', () => {
    useLocaleStore().setLocale('et')
    const { t } = useLocale()
    expect(t('Hello', 'Tere')).toBe('Tere')
  })

  it('falls back to EN in et locale when ET is null or empty', () => {
    useLocaleStore().setLocale('et')
    const { t } = useLocale()
    expect(t('Hello', null)).toBe('Hello')
    expect(t('Hello', '')).toBe('Hello')
    expect(t('Hello')).toBe('Hello')
  })
})
