import { describe, it, expect, vi, beforeEach } from 'vitest'

vi.mock('@/api/client', () => ({ apiFetch: vi.fn().mockResolvedValue(undefined) }))

import { apiFetch } from '@/api/client'
import * as admin from '@/api/admin'
import { getProducts, getProduct } from '@/api/products'
import { login, logout, renewToken } from '@/api/auth'
import { getCart, addToCart, updateCartItem, removeCartItem } from '@/api/cart'
import { getCategories } from '@/api/categories'
import { getCollections } from '@/api/collections'
import { getOrders, getOrder, createOrder } from '@/api/orders'
import type { AdminProductWriteDto, AdminCategoryWriteDto, CreateOrderDto } from '@/types'

const fetchMock = vi.mocked(apiFetch)
const lastCall = () => fetchMock.mock.calls.at(-1)!

beforeEach(() => fetchMock.mockResolvedValue(undefined))

describe('products api — query string building', () => {
  it('builds no query string when no filters are given', async () => {
    await getProducts()
    expect(lastCall()[0]).toBe('/Products')
  })

  it('includes only the provided filters', async () => {
    await getProducts({ categoryId: 'c1', gender: 'M' })
    expect(lastCall()[0]).toBe('/Products?categoryId=c1&gender=M')
  })

  it('getProduct targets the detail path', async () => {
    await getProduct('p9')
    expect(lastCall()[0]).toBe('/Products/p9')
  })
})

describe('auth api — paths and methods', () => {
  it('login POSTs to /Account/Login with the payload', async () => {
    const payload = { email: 'a@b.c', password: 'pw' }
    await login(payload)
    expect(lastCall().slice(0, 3)).toEqual(['/Account/Login', 'POST', payload])
  })

  it('logout POSTs to /Account/Logout', async () => {
    await logout({ refreshToken: 'r' })
    expect(lastCall().slice(0, 2)).toEqual(['/Account/Logout', 'POST'])
  })

  it('renewToken POSTs to /Account/RenewRefreshToken', async () => {
    await renewToken({ jwt: 'j', refreshToken: 'r' })
    expect(lastCall()[0]).toBe('/Account/RenewRefreshToken')
  })
})

describe('cart api — paths and methods', () => {
  it('getCart GETs /Cart', async () => {
    await getCart()
    expect(lastCall()[0]).toBe('/Cart')
  })

  it('addToCart POSTs /Cart/items', async () => {
    await addToCart({ productVariantId: 'v', quantity: 2 })
    expect(lastCall().slice(0, 2)).toEqual(['/Cart/items', 'POST'])
  })

  it('updateCartItem PUTs /Cart/items/{id}', async () => {
    await updateCartItem('i1', { quantity: 3 })
    expect(lastCall().slice(0, 3)).toEqual(['/Cart/items/i1', 'PUT', { quantity: 3 }])
  })

  it('removeCartItem DELETEs /Cart/items/{id}', async () => {
    await removeCartItem('i1')
    expect(lastCall().slice(0, 2)).toEqual(['/Cart/items/i1', 'DELETE'])
  })
})

describe('catalog & orders api — paths and methods', () => {
  it('getCategories GETs /Categories', async () => {
    await getCategories()
    expect(lastCall()[0]).toBe('/Categories')
  })

  it('getCollections GETs /Collections', async () => {
    await getCollections()
    expect(lastCall()[0]).toBe('/Collections')
  })

  it('getOrders GETs /Orders', async () => {
    await getOrders()
    expect(lastCall()[0]).toBe('/Orders')
  })

  it('getOrder GETs /Orders/{id}', async () => {
    await getOrder('o1')
    expect(lastCall()[0]).toBe('/Orders/o1')
  })

  it('createOrder POSTs /Orders with the payload', async () => {
    const payload = { shippingCity: 'Tallinn' } as CreateOrderDto
    await createOrder(payload)
    expect(lastCall().slice(0, 3)).toEqual(['/Orders', 'POST', payload])
  })
})

describe('admin api — read & delete passthroughs', () => {
  it('GET collection by id and DELETE collection target the right paths', async () => {
    await admin.adminGetCollection('k1')
    expect(lastCall()[0]).toBe('/admin/collections/k1')
    await admin.adminDeleteCollection('k1')
    expect(lastCall().slice(0, 2)).toEqual(['/admin/collections/k1', 'DELETE'])
  })

  it('list/get/delete products', async () => {
    await admin.adminGetProducts()
    expect(lastCall()[0]).toBe('/admin/products')
    await admin.adminGetProduct('p1')
    expect(lastCall()[0]).toBe('/admin/products/p1')
    await admin.adminDeleteProduct('p1')
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1', 'DELETE'])
  })

  it('list categories, get order, delete category', async () => {
    await admin.adminGetCategories()
    expect(lastCall()[0]).toBe('/admin/categories')
    await admin.adminGetOrder('o1')
    expect(lastCall()[0]).toBe('/admin/orders/o1')
    await admin.adminDeleteCategory('c1')
    expect(lastCall().slice(0, 2)).toEqual(['/admin/categories/c1', 'DELETE'])
  })

  it('variant create/update/delete nest under the product', async () => {
    const v = { sku: 's', price: 1, unitPrice: 1, stockQty: 1, isActive: true, colorId: 'cl', sizeId: 'sz' }
    await admin.adminAddVariant('p1', v)
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1/variants', 'POST'])
    await admin.adminUpdateVariant('p1', 'v1', v)
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1/variants/v1', 'PUT'])
    await admin.adminDeleteVariant('p1', 'v1')
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1/variants/v1', 'DELETE'])
  })

  it('image add/delete nest under the product', async () => {
    await admin.adminAddImage('p1', { url: 'u', altTextEn: null, altTextEt: null, sortOrder: 0 })
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1/images', 'POST'])
    await admin.adminDeleteImage('p1', 'img1')
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1/images/img1', 'DELETE'])
  })

  it('collection create/update normalize and use the right method', async () => {
    const payload = { nameEn: ' C ', nameEt: null, descriptionEn: null, descriptionEt: null, launchDate: null, isActive: true }
    await admin.adminCreateCollection(payload)
    expect(lastCall().slice(0, 2)).toEqual(['/admin/collections', 'POST'])
    expect((lastCall()[2] as typeof payload).nameEn).toBe('C')
    await admin.adminUpdateCollection('k1', payload)
    expect(lastCall().slice(0, 2)).toEqual(['/admin/collections/k1', 'PUT'])
  })

  it('adminUpdateProduct PUTs to the product path', async () => {
    await admin.adminUpdateProduct('p1', { nameEn: 'N' } as AdminProductWriteDto)
    expect(lastCall().slice(0, 2)).toEqual(['/admin/products/p1', 'PUT'])
  })
})

describe('admin api — normalizeAdminPayload', () => {
  it('trims strings and converts empty/whitespace strings to null', async () => {
    const payload: AdminCategoryWriteDto = {
      nameEn: '  Tops  ',
      nameEt: '   ',
      parentCategoryId: null,
    }
    await admin.adminCreateCategory(payload)
    const [path, method, body] = lastCall()
    expect(path).toBe('/admin/categories')
    expect(method).toBe('POST')
    expect(body).toEqual({ nameEn: 'Tops', nameEt: null, parentCategoryId: null })
  })

  it('trims object-level string values but leaves array string elements as-is', async () => {
    const payload = {
      nameEn: 'P',
      nameEt: null,
      descriptionEn: null,
      descriptionEt: null,
      materialEn: null,
      materialEt: null,
      gender: ' M ',
      isActive: true,
      collectionId: null,
      categoryIds: ['  a  ', 'b'],
    } as unknown as AdminProductWriteDto
    await admin.adminCreateProduct(payload)
    const body = lastCall()[2] as AdminProductWriteDto
    expect(body.gender).toBe('M')
    // Array elements recurse as bare strings, which the trim branch (object
    // values only) does not touch — so they pass through unchanged.
    expect(body.categoryIds).toEqual(['  a  ', 'b'])
  })

  it('updates use PUT with the id in the path', async () => {
    await admin.adminUpdateCategory('cat1', {
      nameEn: 'X',
      nameEt: null,
      parentCategoryId: null,
    })
    expect(lastCall().slice(0, 2)).toEqual(['/admin/categories/cat1', 'PUT'])
  })
})

describe('admin api — orders query', () => {
  it('omits the query string when no status is given', async () => {
    await admin.adminGetOrders()
    expect(lastCall()[0]).toBe('/admin/orders')
  })

  it('url-encodes the status filter', async () => {
    await admin.adminGetOrders('In Progress')
    expect(lastCall()[0]).toBe('/admin/orders?status=In%20Progress')
  })

  it('adminUpdateOrderStatus PUTs to the status path', async () => {
    await admin.adminUpdateOrderStatus('o1', { status: 'Shipped' })
    expect(lastCall().slice(0, 3)).toEqual([
      '/admin/orders/o1/status',
      'PUT',
      { status: 'Shipped' },
    ])
  })

  it('adminUpdateStock PUTs to /admin/stock/{variantId}', async () => {
    await admin.adminUpdateStock('v1', { stockQty: 10 })
    expect(lastCall().slice(0, 3)).toEqual(['/admin/stock/v1', 'PUT', { stockQty: 10 }])
  })
})
