import { request, setToken, clearToken, isLoggedIn, resolveFileUrl } from '@/utils/http'
import type { Category, Product, ProductTag, Banner, CartItem } from './types'

const CART_STORAGE_KEY = 'qps_cart_items'
const STORE_ID_STORAGE_KEY = 'qps_mall_store_id'
const MEMBER_ID_STORAGE_KEY = 'qps_member_id'
export { isLoggedIn }

interface ApiSku {
  id: string
  specName: string
  price: number
  marketPrice: number
  stock: number
  weightGram: number
}

interface ApiProduct {
  id: string
  code: string
  name: string
  categoryId: string | null
  categoryName: string
  description: string
  mainImageUrl: string
  albumImages: string[]
  images: string[]
  skus: ApiSku[]
  sales: number
  isHot: boolean
  tags?: ProductTag[]
}

interface MallStore {
  id: string
}

interface ApiPage<T> {
  items?: T[]
  list?: T[]
}

interface MallBanner {
  id: string
  imageUrl: string
  title: string
  linkValue: string
}

export async function getStoreId(): Promise<string> {
  const cachedId = uni.getStorageSync(STORE_ID_STORAGE_KEY)
  if (cachedId) return cachedId

  const store = await request<MallStore>('/api/mall/store/default')
  if (!store?.id) throw new Error('商城尚未配置可用门店')
  uni.setStorageSync(STORE_ID_STORAGE_KEY, store.id)
  return store.id
}

function mapBanner(b: MallBanner): Banner {
  return {
    id: b.id,
    image: b.imageUrl,
    title: b.title,
    link: b.linkValue || ''
  }
}

export async function getCategories(): Promise<Category[]> {
  const storeId = await getStoreId()
  const tree = await request<Category[]>(`/api/mall/categories/tree?storeId=${storeId}`)
  return tree.map(c => ({
    id: c.id,
    name: c.name,
    icon: resolveFileUrl(c.icon),
    children: (c.children || []).map(ch => ({
      id: ch.id,
      name: ch.name,
      icon: resolveFileUrl(ch.icon)
    }))
  }))
}

export async function getProducts(categoryId?: string, tagCode?: string): Promise<Product[]> {
  const params: string[] = []
  if (categoryId) params.push(`categoryId=${encodeURIComponent(categoryId)}`)
  if (tagCode) params.push(`tagCode=${encodeURIComponent(tagCode)}`)
  params.push('pageSize=100')
  const data = await request<ApiPage<ApiProduct>>(`/api/mall/products?${params.join('&')}`)
  const items = data.items || data.list || []
  return items.map(mapProduct)
}

export async function getProductById(id: string): Promise<Product | undefined> {
  const p = await request<ApiProduct>(`/api/mall/products/${encodeURIComponent(id)}`)
  return p ? mapProduct(p) : undefined
}

export async function getHotProducts(): Promise<Product[]> {
  const data = await request<ApiPage<ApiProduct>>('/api/mall/products?isHot=true&pageSize=50')
  return (data.items || data.list || []).map(mapProduct)
}

export async function searchProducts(keyword: string): Promise<Product[]> {
  const data = await request<ApiPage<ApiProduct>>(
    `/api/mall/products?keyword=${encodeURIComponent(keyword)}&pageSize=50`
  )
  return (data.items || data.list || []).map(mapProduct)
}

function mapProduct(p: ApiProduct): Product {
  return {
    id: p.id,
    code: p.code,
    name: p.name,
    categoryId: p.categoryId,
    categoryName: p.categoryName,
    description: p.description,
    images: p.images || [p.mainImageUrl, ...(p.albumImages || [])].filter(Boolean),
    skus: (p.skus || []).map(s => ({
      id: s.id,
      specName: s.specName,
      price: s.price,
      marketPrice: s.marketPrice || 0,
      stock: s.stock,
      weightGram: s.weightGram
    })),
    sales: p.sales || 0,
    isHot: p.isHot || false,
    tags: (p.tags || []).filter(tag => tag.isActive)
  }
}

export async function getBanners(): Promise<Banner[]> {
  const storeId = await getStoreId()
  const list = await request<MallBanner[]>(`/api/mall/banners?storeId=${storeId}&position=home`)
  return list.map(mapBanner)
}

function getLocalCartItems(): CartItem[] {
  try {
    const items = uni.getStorageSync(CART_STORAGE_KEY)
    return items ? JSON.parse(items) : []
  } catch {
    return []
  }
}

export async function getCartItems(): Promise<CartItem[]> {
  if (isLoggedIn() && getMemberId()) return getRemoteCart()
  return getLocalCartItems()
}

export function saveCartItems(items: CartItem[]): void {
  uni.setStorageSync(CART_STORAGE_KEY, JSON.stringify(items))
}

interface MallCartResponse {
  items: Array<{
    skuId: string
    productId: string
    productName: string
    productImage: string
    specName: string
    price: number
    quantity: number
    weightGram: number
    stock: number
    availableStock: number
    isValid: boolean
    invalidReason: string | null
  }>
}

function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
}

async function getRemoteCart(): Promise<CartItem[]> {
  const memberId = getMemberId()
  const storeId = await getStoreId()
  const cart = await request<MallCartResponse>(`/api/mall/carts/${memberId}/store/${storeId}`)
  const selectedBySku = new Map(getLocalCartItems().map(item => [item.skuId, item.selected]))
  const items = (cart.items || []).map(item => ({
    productId: item.productId,
    productName: item.productName,
    productImage: item.productImage,
    skuId: item.skuId,
    specName: item.specName,
    price: item.price,
    quantity: item.quantity,
    weightGram: item.weightGram,
    selected: selectedBySku.get(item.skuId) ?? true,
    stock: item.stock,
    availableStock: item.availableStock,
    isValid: item.isValid,
    invalidReason: item.invalidReason
  }))
  saveCartItems(items)
  return items
}

export async function addToCart(item: CartItem): Promise<CartItem[]> {
  if (isLoggedIn() && getMemberId() && isGuid(item.skuId)) {
    const storeId = await getStoreId()
    await request<MallCartResponse>('/api/mall/carts/add', 'POST', {
      userId: getMemberId(),
      storeId,
      skuId: item.skuId,
      quantity: item.quantity
    })
    return getRemoteCart()
  }

  const items = getLocalCartItems()
  const existingIndex = items.findIndex(
    i => i.productId === item.productId && i.skuId === item.skuId
  )
  if (existingIndex >= 0) {
    items[existingIndex].quantity += item.quantity
  } else {
    items.push({ ...item, selected: true })
  }
  saveCartItems(items)
  return items
}

export async function updateCartItemQuantity(skuId: string, quantity: number): Promise<CartItem[]> {
  if (isLoggedIn() && getMemberId() && isGuid(skuId)) {
    if (quantity <= 0) return removeCartItem(skuId)
    const storeId = await getStoreId()
    await request<MallCartResponse>('/api/mall/carts/update-item', 'PUT', {
      userId: getMemberId(),
      storeId,
      skuId,
      quantity
    })
    return getRemoteCart()
  }

  const items = getLocalCartItems()
  const item = items.find(i => i.skuId === skuId)
  if (item) {
    if (quantity <= 0) {
      return removeCartItem(skuId)
    }
    item.quantity = quantity
    saveCartItems(items)
  }
  return items
}

export async function removeCartItem(skuId: string): Promise<CartItem[]> {
  if (isLoggedIn() && getMemberId() && isGuid(skuId)) {
    const storeId = await getStoreId()
    await request(
      `/api/mall/carts/${getMemberId()}/store/${storeId}/items/${skuId}`,
      'DELETE'
    )
    return getRemoteCart()
  }

  const items = getLocalCartItems().filter(i => i.skuId !== skuId)
  saveCartItems(items)
  return items
}

export function toggleCartItemSelection(skuId: string): CartItem[] {
  const items = getLocalCartItems()
  const item = items.find(i => i.skuId === skuId)
  if (item) {
    item.selected = !item.selected
    saveCartItems(items)
  }
  return items
}

export async function clearCart(): Promise<void> {
  if (isLoggedIn() && getMemberId()) {
    const storeId = await getStoreId()
    await request(`/api/mall/carts/${getMemberId()}/store/${storeId}`, 'DELETE')
  }
  saveCartItems([])
}

export interface LoginRequest { phone: string; password: string }
export interface RegisterRequest { phone: string; password: string; nickname?: string }
export interface AuthResponse { accessToken: string; tokenType: string; memberId: string; phone: string; nickname: string }

interface MallAuthResponse {
  token: string
  memberId: string
  phone: string
  nickname: string
}

export async function register(req: RegisterRequest): Promise<AuthResponse> {
  const response = saveAuthResponse(await request<MallAuthResponse>('/api/mall/auth/register', 'POST', req))
  await syncGuestCart()
  return response
}

export async function login(req: LoginRequest): Promise<AuthResponse> {
  const response = saveAuthResponse(await request<MallAuthResponse>('/api/mall/auth/login', 'POST', req))
  await syncGuestCart()
  return response
}

function saveAuthResponse(response: MallAuthResponse): AuthResponse {
  setToken(response.token)
  uni.setStorageSync(MEMBER_ID_STORAGE_KEY, response.memberId)
  return {
    accessToken: response.token,
    tokenType: 'Bearer',
    memberId: response.memberId,
    phone: response.phone,
    nickname: response.nickname
  }
}

async function syncGuestCart(): Promise<void> {
  const localItems = getLocalCartItems().filter(item => isGuid(item.skuId))
  const storeId = await getStoreId()
  for (const item of localItems) {
    await request('/api/mall/carts/add', 'POST', {
      userId: getMemberId(),
      storeId,
      skuId: item.skuId,
      quantity: item.quantity
    })
  }
  await getRemoteCart()
}

export function getMemberId(): string {
  return uni.getStorageSync(MEMBER_ID_STORAGE_KEY) || ''
}

export interface MemberProfile {
  id: string
  phone: string
  nickname: string
  avatar: string
  levelName: string
  availablePoints: number
  totalOrders: number
  totalSpent: number
}

export function getMemberProfile(): Promise<MemberProfile> {
  return request<MemberProfile>('/api/mall/member/me')
}

export interface MemberCoupon {
  id: string
  couponName: string
  thresholdAmount: number
  discountAmount: number
  status: string
}

export function getMyCoupons(): Promise<MemberCoupon[]> {
  return request<MemberCoupon[]>('/api/mall/member/me/coupons')
}

export function getMyOrders(status?: string): Promise<ApiPage<MallOrder>> {
  const statusQuery = status ? `&status=${encodeURIComponent(status)}` : ''
  return request<ApiPage<MallOrder>>(`/api/mall/orders?page=1&pageSize=50${statusQuery}`)
}

export interface MallOrder {
  orderNo: string
  status: string
  receiverName: string
  goodsAmount: number
  promotionDiscountAmount: number
  payableAmount: number
  createdAt: string
}

export function getAddresses(): Promise<MallAddress[]> {
  const memberId = getMemberId()
  return request<MallAddress[]>(`/api/mall/addresses?userId=${memberId}`)
}

export function saveAddress(address: Partial<MallAddress> & {
  receiverName: string
  receiverPhone: string
  provinceCode: string
  provinceName: string
  cityCode: string
  cityName: string
  districtCode: string
  districtName: string
  detailAddress: string
  isDefault: boolean
}): Promise<unknown> {
  return request('/api/mall/addresses', 'POST', { ...address, userId: getMemberId() })
}

export function updateAddress(
  addressId: string,
  address: Parameters<typeof saveAddress>[0]
): Promise<boolean> {
  return request<boolean>(`/api/mall/addresses/${addressId}`, 'PUT', address)
}

export function deleteAddress(addressId: string): Promise<boolean> {
  return request<boolean>(`/api/mall/addresses/${addressId}`, 'DELETE')
}

export interface MallAddress {
  id: string
  receiverName: string
  receiverPhone: string
  provinceCode: string
  provinceName: string
  cityCode: string
  cityName: string
  districtCode: string
  districtName: string
  detailAddress: string
  fullAddress: string
  isDefault: boolean
}

export function createOrder(data: CreateOrderRequest): Promise<MallOrderDetail> {
  return request<MallOrderDetail>('/api/mall/orders', 'POST', data)
}

export interface CreateOrderRequest {
  storeId: string
  receiverName: string
  receiverPhone: string
  provinceCode: string
  provinceName: string
  cityCode: string
  cityName: string
  districtCode: string
  districtName: string
  receiverAddress: string
  remark: string
  items: Array<{ skuId: string; quantity: number }>
}

export interface MallOrderDetail extends MallOrder {
  items: Array<{
    productName: string
    specName: string
    quantity: number
    unitPrice: number
    isGift: boolean
    promotionName: string
    originalUnitPrice: number | null
  }>
}

export function logout(): void {
  clearToken()
  uni.removeStorageSync(MEMBER_ID_STORAGE_KEY)
  saveCartItems([])
}