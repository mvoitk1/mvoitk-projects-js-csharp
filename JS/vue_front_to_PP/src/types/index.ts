// ─── Auth ────────────────────────────────────────────────────────────────────

export interface LoginPayload {
  email: string
  password: string
}

export interface RegisterPayload {
  firstName: string
  lastName: string
  email: string
  password: string
}

export interface JWTResponse {
  jwt: string | null
  refreshToken: string | null
}

export interface RefreshTokenModel {
  jwt: string | null
  refreshToken: string | null
}

export interface LogoutInfo {
  refreshToken: string
}

// ─── Categories ──────────────────────────────────────────────────────────────

export interface CategoryDto {
  id: string
  name: string | null
  parentCategoryId: string | null
  subCategories: CategoryDto[] | null
}

// ─── Collections ─────────────────────────────────────────────────────────────

export interface CollectionDto {
  id: string
  name: string | null
  description: string | null
  launchDate: string | null
  isActive: boolean
}

// ─── Products ────────────────────────────────────────────────────────────────

export interface ProductImageDto {
  id: string
  url: string | null
  altText: string | null
  sortOrder: number
}

export interface ProductVariantDto {
  id: string
  sku: string | null
  price: number
  stockQty: number
  isActive: boolean
  colorId: string
  colorName: string | null
  colorHex: string | null
  sizeId: string
  sizeCode: string | null
  sizeDisplayName: string | null
}

export interface ProductListItemDto {
  id: string
  name: string | null
  gender: string | null
  isActive: boolean
  lowestPrice: number | null
  primaryImageUrl: string | null
  collectionName: string | null
}

export interface ProductDto {
  id: string
  name: string | null
  description: string | null
  material: string | null
  gender: string | null
  isActive: boolean
  createdAt: string
  collectionId: string | null
  collectionName: string | null
  images: ProductImageDto[] | null
  variants: ProductVariantDto[] | null
  categoryNames: string[] | null
}

// ─── Cart ────────────────────────────────────────────────────────────────────

export interface CartItemDto {
  id: string
  quantity: number
  unitPrice: number
  lineTotal: number
  productVariantId: string
  productName: string | null
  sku: string | null
  colorName: string | null
  sizeCode: string | null
  imageUrl: string | null
}

export interface CartDto {
  id: string
  status: string | null
  items: CartItemDto[] | null
  total: number
  itemCount: number
}

export interface AddToCartDto {
  productVariantId: string
  quantity: number
}

export interface UpdateCartItemDto {
  quantity: number
}

// ─── Orders ──────────────────────────────────────────────────────────────────

export interface OrderItemDto {
  id: string
  quantity: number
  unitPrice: number
  lineTotal: number
  productVariantId: string
  productName: string | null
  sku: string | null
  colorName: string | null
  sizeCode: string | null
}

export interface OrderListItemDto {
  id: string
  orderNumber: string | null
  status: string | null
  totalAmount: number
  createdAt: string
  itemCount: number
}

export interface OrderDto {
  id: string
  orderNumber: string | null
  status: string | null
  totalAmount: number
  createdAt: string
  shippingFirstName: string | null
  shippingLastName: string | null
  shippingEmail: string | null
  shippingPhone: string | null
  shippingCountry: string | null
  shippingCity: string | null
  shippingStreet: string | null
  shippingPostalCode: string | null
  items: OrderItemDto[] | null
}

export interface CreateOrderDto {
  shippingFirstName: string
  shippingLastName: string
  shippingEmail: string
  shippingPhone: string
  shippingCountry: string
  shippingCity: string
  shippingStreet: string
  shippingPostalCode: string
}

// ─── Admin DTOs ──────────────────────────────────────────────────────────────

export interface AdminCategoryWriteDto {
  nameEn: string
  nameEt: string | null
  parentCategoryId: string | null
}

export interface AdminCategoryDto {
  id: string
  nameEn: string | null
  nameEt: string | null
  parentCategoryId: string | null
  parentCategoryName: string | null
}

export interface AdminCollectionWriteDto {
  nameEn: string
  nameEt: string | null
  descriptionEn: string | null
  descriptionEt: string | null
  launchDate: string | null
  isActive: boolean
}

export interface AdminCollectionDto {
  id: string
  nameEn: string | null
  nameEt: string | null
  descriptionEn: string | null
  descriptionEt: string | null
  launchDate: string | null
  isActive: boolean
}

export interface AdminProductWriteDto {
  nameEn: string
  nameEt: string | null
  descriptionEn: string | null
  descriptionEt: string | null
  materialEn: string | null
  materialEt: string | null
  gender: string | null
  isActive: boolean
  collectionId: string | null
  categoryIds: string[] | null
}

export interface AdminProductDto {
  id: string
  nameEn: string | null
  nameEt: string | null
  descriptionEn: string | null
  descriptionEt: string | null
  materialEn: string | null
  materialEt: string | null
  gender: string | null
  isActive: boolean
  createdAt: string
  collectionId: string | null
  collectionName: string | null
  variants: AdminVariantDto[] | null
  images: AdminProductImageDto[] | null
  categoryIds: string[] | null
}

export interface AdminVariantWriteDto {
  sku: string
  price: number
  unitPrice: number
  stockQty: number
  isActive: boolean
  colorId: string
  sizeId: string
}

export interface AdminVariantDto {
  id: string
  sku: string | null
  price: number
  unitPrice: number
  stockQty: number
  isActive: boolean
  colorId: string
  colorName: string | null
  sizeId: string
  sizeCode: string | null
}

export interface AdminProductImageDto {
  id: string
  url: string | null
  altTextEn: string | null
  altTextEt: string | null
  sortOrder: number
}

export interface AdminProductImageWriteDto {
  url: string
  altTextEn: string | null
  altTextEt: string | null
  sortOrder: number
}

export interface AdminOrderDto {
  id: string
  orderNumber: string | null
  status: string | null
  totalAmount: number
  createdAt: string
  customerFirstName: string | null
  customerLastName: string | null
  customerEmail: string | null
  shippingFirstName: string | null
  shippingLastName: string | null
  shippingEmail: string | null
  shippingPhone: string | null
  shippingCountry: string | null
  shippingCity: string | null
  shippingStreet: string | null
  shippingPostalCode: string | null
  items: OrderItemDto[] | null
}

export interface AdminOrderStatusDto {
  status: string
}

export interface AdminStockUpdateDto {
  stockQty: number
}
