import { describe, it, expect } from 'vitest'
import { mount } from '@vue/test-utils'
import { RouterLinkStub } from '@vue/test-utils'
import ProductCard from '@/components/ProductCard.vue'
import type { ProductListItemDto } from '@/types'

function makeProduct(over: Partial<ProductListItemDto> = {}): ProductListItemDto {
  return {
    id: 'p1',
    name: 'Linen Shirt',
    gender: 'M',
    isActive: true,
    lowestPrice: 49.9,
    primaryImageUrl: 'http://img/1.jpg',
    collectionName: 'Summer',
    ...over,
  }
}

function mountCard(product: ProductListItemDto) {
  return mount(ProductCard, {
    props: { product },
    global: { stubs: { RouterLink: RouterLinkStub } },
  })
}

describe('ProductCard', () => {
  it('renders the name, collection and formatted lowest price', () => {
    const wrapper = mountCard(makeProduct())
    expect(wrapper.text()).toContain('Linen Shirt')
    expect(wrapper.text()).toContain('Summer')
    expect(wrapper.text()).toContain('From €49.90')
  })

  it('links to the product detail route with the product id', () => {
    const wrapper = mountCard(makeProduct({ id: 'abc' }))
    const link = wrapper.findComponent(RouterLinkStub)
    expect(link.props('to')).toEqual({ name: 'product-detail', params: { id: 'abc' } })
  })

  it('renders the image with alt text when present', () => {
    const wrapper = mountCard(makeProduct())
    const img = wrapper.find('img')
    expect(img.exists()).toBe(true)
    expect(img.attributes('src')).toBe('http://img/1.jpg')
    expect(img.attributes('alt')).toBe('Linen Shirt')
  })

  it('shows a placeholder instead of an image when there is no primary image', () => {
    const wrapper = mountCard(makeProduct({ primaryImageUrl: null }))
    expect(wrapper.find('img').exists()).toBe(false)
    expect(wrapper.find('.card__image-placeholder').exists()).toBe(true)
  })

  it('omits the price line when lowestPrice is null', () => {
    const wrapper = mountCard(makeProduct({ lowestPrice: null }))
    expect(wrapper.find('.card__price').exists()).toBe(false)
  })
})
