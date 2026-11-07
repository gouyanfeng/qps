import {
  request,
  setToken,
  clearToken,
  isLoggedIn,
  resolveFileUrl
} from '@/utils/http'
import type { ApiResp } from '@/utils/http'
import type { Category, Product, ProductTag, ProductPromotion, Banner, CartItem, BannerLinkType, ProductReview, ProductReviewPage, MallTagItem } from './types'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000'
const MEMBER_TOKEN_STORAGE_KEY = 'qps_member_token'
const MEMBER_ID_STORAGE_KEY = 'qps_member_id'
const deselectedCartSkuIds = new Set<string>()
export { isLoggedIn }

interface ApiSku {
  id: string
  specName: string
  price: number
  marketPrice: number
  stock: number
  weightGram: number
  discountedPrice?: number | null
  discountLabel?: string
}

interface ApiProduct {
  id: string
  code: string
  name: string
  categoryId: string | null
  categoryName: string
  storeName?: string
  description: string
  detailHtml?: string
  mainImageUrl: string
  albumImages: string[]
  images: string[]
  skus: ApiSku[]
  sales: number
  isHot: boolean
  tags?: ProductTag[]
  promotions?: ProductPromotion[]
  promotionBadgeText?: string
  promotionBadgeColor?: string
}

interface ApiPage<T> {
  items?: T[]
  list?: T[]
  total?: number
  totalCount?: number
  page?: number
  currentPage?: number
  pageSize?: number
  hasMore?: boolean
}

interface MallBanner {
  id: string
  imageUrl: string
  title: string
  linkValue: string
  linkType: BannerLinkType
}

function mapBanner(b: MallBanner): Banner {
  return {
    id: b.id,
    image: resolveFileUrl(b.imageUrl),
    title: b.title,
    link: b.linkValue || '',
    linkType: b.linkType
  }
}

export async function getCategories(): Promise<Category[]> {
  const tree = await request<Category[]>('/api/mall/categories/tree')
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

export async function getProducts(
  categoryId?: string,
  tagCode?: string,
  page: number = 1,
  pageSize: number = 20,
  sort?: string
): Promise<{ items: Product[]; hasMore: boolean }> {
  const params: string[] = [`page=${page}`, `pageSize=${pageSize}`]
  if (categoryId) params.push(`categoryId=${encodeURIComponent(categoryId)}`)
  if (tagCode) params.push(`tagCode=${encodeURIComponent(tagCode)}`)
  if (sort && sort !== 'default') params.push(`sort=${encodeURIComponent(sort)}`)
  const data = await request<ApiPage<ApiProduct>>(`/api/mall/products?${params.join('&')}`)
  const items = (data.items || data.list || []).map(mapProduct)
  const total = data.totalCount ?? data.total ?? 0
  const hasMore = data.hasMore ?? (total > page * pageSize && items.length === pageSize)
  return { items, hasMore }
}

export async function getProductById(id: string): Promise<Product | undefined> {
  const p = await request<ApiProduct>(`/api/mall/products/${encodeURIComponent(id)}`)
  return p ? mapProduct(p) : undefined
}

export async function getProductReviews(
  productId: string,
  page: number = 1,
  pageSize: number = 5,
  tag?: string,
  featuredOnly?: boolean
): Promise<ProductReviewPage> {
  const params: string[] = [`page=${page}`, `pageSize=${pageSize}`]
  if (tag) params.push(`tag=${encodeURIComponent(tag)}`)
  if (featuredOnly) params.push('featuredOnly=true')

  const payload = await request<ProductReviewPage>(
    `/api/mall/products/${encodeURIComponent(productId)}/reviews?${params.join('&')}`
  )
  return {
    items: Array.isArray(payload?.items)
      ? payload.items.map(item => ({
          ...item,
          images: Array.isArray(item.images) ? item.images : [],
          replyContent: item.replyContent ?? ''
        }))
      : [],
    totalCount: payload?.totalCount ?? 0,
    positiveRate: payload?.positiveRate ?? 0,
    allTotalCount: payload?.allTotalCount ?? payload?.totalCount ?? 0,
    allPositiveRate: payload?.allPositiveRate ?? payload?.positiveRate ?? 0,
    tagStats: Array.isArray(payload?.tagStats) ? payload.tagStats : []
  }
}

export async function getReviewTags(): Promise<MallTagItem[]> {
  const payload = await request<MallTagItem[]>(
    '/api/mall/tags?type=Review&isActive=true'
  )
  return Array.isArray(payload) ? payload : []
}

export interface ProductReviewCreatePayload {
  orderNo: string
  orderItemId: string
  rating: number
  content: string
  tags: string[]
  images: string[]
}

export function createProductReview(
  productId: string,
  payload: ProductReviewCreatePayload
): Promise<ProductReview> {
  return request<ProductReview>(
    `/api/mall/products/${encodeURIComponent(productId)}/reviews`,
    'POST',
    payload
  )
}

export function uploadReviewImage(filePath: string): Promise<string> {
  return uploadImage(filePath, 'review')
}

/** 售后凭证图片上传，走同一图片上传接口的 aftersale 场景 */
export function uploadAfterSaleImage(filePath: string): Promise<string> {
  return uploadImage(filePath, 'aftersale')
}

function uploadImage(filePath: string, scene: 'review' | 'aftersale'): Promise<string> {
  return new Promise((resolve, reject) => {
    const token = uni.getStorageSync(MEMBER_TOKEN_STORAGE_KEY) || ''
    uni.uploadFile({
      url: `${API_BASE_URL}/api/mall/file/upload/img?scene=${scene}`,
      filePath,
      name: 'file',
      header: token ? { Authorization: `Bearer ${token}` } : {},
      success(res) {
        let body: ApiResp<{ fileUrl?: string }> | null = null
        try {
          body = typeof res.data === 'string'
            ? (JSON.parse(res.data) as ApiResp<{ fileUrl?: string }>)
            : (res.data as unknown as ApiResp<{ fileUrl?: string }>)
        } catch {
          body = null
        }
        const fileUrl = body?.data?.fileUrl
        if (res.statusCode === 200 && body?.code === 200 && fileUrl) {
          resolve(fileUrl)
          return
        }
        reject(new Error(body?.msg || `图片上传失败(${res.statusCode})`))
      },
      fail(err) {
        reject(new Error(err.errMsg || '图片上传失败'))
      }
    })
  })
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
  const rawImages: string[] = p.images && p.images.length
    ? p.images
    : [p.mainImageUrl, ...(p.albumImages || [])].filter(Boolean)
  return {
    id: p.id,
    code: p.code,
    name: p.name,
    categoryId: p.categoryId,
    categoryName: p.categoryName,
    storeName: p.storeName || '',
    description: p.description,
    detailHtml: p.detailHtml || '',
    images: rawImages.map(resolveFileUrl),
    skus: (p.skus || []).map(s => ({
      id: s.id,
      specName: s.specName,
      price: s.price,
      marketPrice: s.marketPrice || 0,
      stock: s.stock,
      weightGram: s.weightGram,
      discountedPrice: s.discountedPrice ?? null,
      discountLabel: s.discountLabel || ''
    })),
    sales: p.sales || 0,
    isHot: p.isHot || false,
    tags: (p.tags || []).filter(tag => tag.isActive).map(tag => ({
      id: tag.id,
      type: tag.type || '',
      code: tag.code,
      name: tag.name,
      color: tag.color,
      sortOrder: tag.sortOrder,
      isActive: tag.isActive
    })),
    promotions: (p.promotions || []),
    promotionBadgeText: p.promotionBadgeText || '',
    promotionBadgeColor: p.promotionBadgeColor || ''
  }
}

export async function getBanners(position = 'home'): Promise<Banner[]> {
  const list = await request<MallBanner[]>(`/api/mall/banners?position=${encodeURIComponent(position)}`)
  return list.map(mapBanner)
}

/** 活动专题（商品推荐 / 领券） */
export interface CampaignCoupon {
  id: string
  couponId: string
  name: string
  code: string
  couponType: string
  thresholdAmount: number
  discountAmount: number
  maxDiscount: number | null
  totalCount: number
  remainCount: number
  perLimit: number
  validDays: number
  startAt: string | null
  endAt: string | null
  scopeType: string
  isActive: boolean
  isClaimable: boolean
  sortOrder: number
}

export interface CampaignSummary {
  id: string
  type: number
  name: string
  coverImageUrl: string
  coverImage: string
  startAt: string
  endAt: string
  status: 'Upcoming' | 'Ongoing' | 'Ended'
  sortOrder: number
}

export interface CampaignDetail {
  id: string
  type: number
  name: string
  coverImageUrl: string
  coverImage: string
  contentHtml: string
  startAt: string
  endAt: string
  status: 'Upcoming' | 'Ongoing' | 'Ended'
  coupons: CampaignCoupon[]
}

export interface CampaignProductItem {
  campaignProductId: string
  groupName: string
  sortOrder: number
  product: Product
}

interface ApiCampaign {
  id: string
  type: number
  name: string
  coverImageUrl: string
  startAt: string
  endAt: string
  status: string
  sortOrder?: number
  contentHtml?: string
  coupons?: CampaignCoupon[]
}

function mapCampaignSummary(campaign: ApiCampaign): CampaignSummary {
  return {
    id: campaign.id,
    type: campaign.type,
    name: campaign.name,
    coverImageUrl: campaign.coverImageUrl || '',
    coverImage: resolveFileUrl(campaign.coverImageUrl || ''),
    startAt: campaign.startAt,
    endAt: campaign.endAt,
    status: (campaign.status || 'Ongoing') as CampaignSummary['status'],
    sortOrder: campaign.sortOrder || 0
  }
}

export async function getCampaigns(): Promise<CampaignSummary[]> {
  const list = await request<ApiCampaign[]>('/api/mall/campaigns')
  return list.map(mapCampaignSummary)
}

export async function getCampaignDetail(id: string): Promise<CampaignDetail> {
  const campaign = await request<ApiCampaign>(`/api/mall/campaigns/${encodeURIComponent(id)}`)
  return {
    ...mapCampaignSummary(campaign),
    contentHtml: campaign.contentHtml || '',
    coupons: campaign.coupons || []
  }
}

export async function getCampaignProducts(
  id: string,
  page = 1,
  pageSize = 10
): Promise<{ items: CampaignProductItem[]; hasMore: boolean; total: number }> {
  const data = await request<ApiPage<{ campaignProductId: string; groupName: string; sortOrder: number; product: ApiProduct }>>(
    `/api/mall/campaigns/${encodeURIComponent(id)}/products?page=${page}&pageSize=${pageSize}`
  )
  const rows = data.items || data.list || []
  const total = data.totalCount ?? data.total ?? 0
  return {
    items: rows.map(row => ({
      campaignProductId: row.campaignProductId,
      groupName: row.groupName || '',
      sortOrder: row.sortOrder,
      product: mapProduct(row.product)
    })),
    hasMore: data.hasMore ?? page * pageSize < total,
    total
  }
}

export async function getCartItems(): Promise<CartItem[]> {
  return getRemoteCart()
}

export interface CartPromotionDetail {
  promotionId: string
  name: string
  discountAmount: number
}

/** 赠品展示项：0 元计价，不参与金额，仅用于下单前展示 */
export interface CartGiftItem {
  skuId: string
  productId: string
  productName: string
  productImage: string
  specName: string
  quantity: number
  promotionId: string
  promotionName: string
}

export interface CartSummary {
  items: CartItem[]
  /** 选中商品的原价合计 */
  selectedAmount: number
  /** 预计促销优惠金额（按选中商品试算，最终以下单结果为准） */
  promotionDiscountAmount: number
  /** 其中折后优惠金额 */
  rateDiscountAmount: number
  /** 其中满减优惠金额 */
  amountDiscountAmount: number
  /** 预计应付金额 */
  payableAmount: number
  /** 预计促销优惠明细 */
  promotionDetails: CartPromotionDetail[]
  /** 按选中商品试算命中的赠品 */
  gifts: CartGiftItem[]
}

export async function getCartOverview(selectedSkuIds?: string[]): Promise<CartSummary> {
  return mapCartSummary(await fetchRemoteCart(selectedSkuIds))
}

function mapCartSummary(cart: MallCartResponse): CartSummary {
  return {
    items: mapCartItems(cart),
    selectedAmount: Number(cart.selectedAmount || 0),
    promotionDiscountAmount: Number(cart.promotionDiscountAmount || 0),
    rateDiscountAmount: Number(cart.rateDiscountAmount || 0),
    amountDiscountAmount: Number(cart.amountDiscountAmount || 0),
    payableAmount: Number(cart.payableAmount || 0),
    promotionDetails: (cart.promotionDetails || []).map(detail => ({
      promotionId: detail.promotionId,
      name: detail.name,
      discountAmount: Number(detail.discountAmount || 0)
    })),
    gifts: (cart.gifts || []).map(gift => ({
      skuId: gift.skuId,
      productId: gift.productId,
      productName: gift.productName,
      productImage: resolveFileUrl(gift.productImage),
      specName: gift.specName,
      quantity: Number(gift.quantity || 0),
      promotionId: gift.promotionId,
      promotionName: gift.promotionName
    }))
  }
}

interface MallCartResponse {
  items: Array<{
    skuId: string
    productId: string
    productName: string
    productImage: string
    specName: string
    price: number
    discountedPrice?: number | null
    discountLabel?: string | null
    promotionBadgeText?: string | null
    promotionBadgeColor?: string | null
    quantity: number
    weightGram: number
    stock: number
    availableStock: number
    isValid: boolean
    invalidReason: string | null
  }>
  promotionDiscountAmount?: number
  rateDiscountAmount?: number
  amountDiscountAmount?: number
  payableAmount?: number
  selectedAmount?: number
  promotionDetails?: Array<{
    promotionId: string
    name: string
    discountAmount: number
  }>
  gifts?: Array<{
    skuId: string
    productId: string
    productName: string
    productImage: string
    specName: string
    quantity: number
    promotionId: string
    promotionName: string
  }>
}

function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value)
}

async function fetchRemoteCart(selectedSkuIds?: string[]): Promise<MallCartResponse> {
  const memberId = getMemberId() || '00000000-0000-0000-0000-000000000000'
  // 未传表示全部选中；传空数组表示未选中任何商品
  const query = selectedSkuIds === undefined ? '' : `?skuIds=${selectedSkuIds.join(',')}`
  return request<MallCartResponse>(`/api/mall/carts/${memberId}${query}`)
}

function mapCartItems(cart: MallCartResponse): CartItem[] {
  return (cart.items || []).map(item => ({
    productId: item.productId,
    productName: item.productName,
    productImage: resolveFileUrl(item.productImage),
    skuId: item.skuId,
    specName: item.specName,
    price: item.price,
    discountedPrice: item.discountedPrice ?? item.price,
    discountLabel: item.discountLabel || null,
    promotionBadgeText: item.promotionBadgeText || null,
    promotionBadgeColor: item.promotionBadgeColor || null,
    quantity: item.quantity,
    weightGram: item.weightGram,
    selected: !deselectedCartSkuIds.has(item.skuId),
    stock: item.stock,
    availableStock: item.availableStock,
    isValid: item.isValid,
    invalidReason: item.invalidReason
  }))
}

async function getRemoteCart(): Promise<CartItem[]> {
  return mapCartItems(await fetchRemoteCart())
}

export async function addToCart(item: CartItem): Promise<CartItem[]> {
  if (!isGuid(item.skuId)) throw new Error('商品规格信息无效，请刷新后重试')
  const cart = await request<MallCartResponse>('/api/mall/carts/add', 'POST', {
    userId: getMemberId() || '00000000-0000-0000-0000-000000000000',
    skuId: item.skuId,
    quantity: item.quantity
  })
  return mapCartItems(cart)
}

export async function updateCartItemQuantity(skuId: string, quantity: number): Promise<CartSummary> {
  if (!isGuid(skuId)) throw new Error('商品规格信息无效，请刷新后重试')
  if (quantity <= 0) return removeCartItem(skuId)
  const cart = await request<MallCartResponse>('/api/mall/carts/update-item', 'PUT', {
    userId: getMemberId() || '00000000-0000-0000-0000-000000000000',
    skuId,
    quantity
  })
  return mapCartSummary(cart)
}

export async function removeCartItem(skuId: string): Promise<CartSummary> {
  if (!isGuid(skuId)) throw new Error('商品规格信息无效，请刷新后重试')
  const memberId = getMemberId() || '00000000-0000-0000-0000-000000000000'
  const cart = await request<MallCartResponse>(`/api/mall/carts/${memberId}/items/${skuId}`, 'DELETE')
  deselectedCartSkuIds.delete(skuId)
  return mapCartSummary(cart)
}

/** 下单成功后只移除本次提交的规格明细，未购买的商品保留在购物车。 */
export async function removeCartItems(skuIds: string[]): Promise<CartSummary> {
  const uniqueIds = [...new Set(skuIds.filter(id => isGuid(id)))]
  if (uniqueIds.length === 0) return getCartOverview()
  const cart = await request<MallCartResponse>('/api/mall/carts/remove-items', 'POST', {
    skuIds: uniqueIds
  })
  for (const skuId of uniqueIds) {
    deselectedCartSkuIds.delete(skuId)
  }
  return mapCartSummary(cart)
}

export function toggleCartItemSelection(items: CartItem[], skuId: string): CartItem[] {
  const item = items.find(cartItem => cartItem.skuId === skuId)
  if (!item) return items
  if (item.selected) deselectedCartSkuIds.add(skuId)
  else deselectedCartSkuIds.delete(skuId)
  return items.map(cartItem =>
    cartItem.skuId === skuId ? { ...cartItem, selected: !cartItem.selected } : cartItem
  )
}

export function setCartItemsSelection(items: CartItem[], selected: boolean): CartItem[] {
  for (const item of items) {
    if (selected) deselectedCartSkuIds.delete(item.skuId)
    else deselectedCartSkuIds.add(item.skuId)
  }
  return items.map(item => ({ ...item, selected }))
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
  const authResponse = await request<MallAuthResponse>('/api/mall/auth/register', 'POST', req)
  const response = saveAuthResponse(authResponse)
  return response
}

export async function login(req: LoginRequest): Promise<AuthResponse> {
  const authResponse = await request<MallAuthResponse>('/api/mall/auth/login', 'POST', req)
  const response = saveAuthResponse(authResponse)
  return response
}

function saveAuthResponse(response: MallAuthResponse): AuthResponse {
  setToken(response.token)
  uni.setStorageSync(MEMBER_ID_STORAGE_KEY, response.memberId)
  deselectedCartSkuIds.clear()
  return {
    accessToken: response.token,
    tokenType: 'Bearer',
    memberId: response.memberId,
    phone: response.phone,
    nickname: response.nickname
  }
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

export interface MallMemberOrderBadge {
  pendingPayment: number
  paid: number
  shipped: number
  completed: number
  returning: number
}

export function getMemberOrderBadge(): Promise<MallMemberOrderBadge> {
  return request<MallMemberOrderBadge>('/api/mall/member/me/order-badge')
}

export function getMyCouponCount(): Promise<number> {
  return request<number>('/api/mall/member/me/coupon-count')
}

export interface MemberCoupon {
  id: string
  couponId: string
  couponCode: string
  couponName: string
  couponType: string
  thresholdAmount: number
  discountAmount: number
  status: string
  expireAt: string
  claimedAt: string | null
  usedAt: string | null
  usedOrderNo: string | null
}

export function getMyCoupons(status?: string): Promise<MemberCoupon[]> {
  const query = status ? `?status=${encodeURIComponent(status)}` : ''
  return request<MemberCoupon[]>(`/api/mall/member/me/coupons${query}`)
}

export interface AvailableCoupon {
  id: string
  code: string
  name: string
  couponType: string
  thresholdAmount: number
  discountAmount: number
  maxDiscount: number | null
  totalCount: number
  remainCount: number
  perLimit: number
  validDays: number
  startAt: string | null
  endAt: string | null
  scopeType: string
  scopeValue: string | null
  isActive: boolean
}

export function getAvailableCoupons(): Promise<AvailableCoupon[]> {
  return request<AvailableCoupon[]>('/api/mall/coupons/available')
}

export function claimCoupon(couponId: string): Promise<MemberCoupon> {
  return request<MemberCoupon>(`/api/mall/member/me/coupons/${couponId}/claim`, 'POST')
}

/** 确认页可用券：按当前结算商品返回，不可用券带原因。 */
export interface CheckoutCoupon {
  id: string
  couponId: string
  name: string
  couponType: string
  thresholdAmount: number
  faceValue: number
  maxDiscount: number | null
  scopeType: string
  expireAt: string
  isAvailable: boolean
  unavailableReason: string | null
  discountAmount: number
}

export function getCheckoutCoupons(
  items: Array<{ skuId: string; quantity: number }>
): Promise<CheckoutCoupon[]> {
  return request<CheckoutCoupon[]>('/api/mall/coupons/for-order', 'POST', { items })
}

export function getMyOrders(status?: string): Promise<ApiPage<MallOrder>> {
  const statusQuery = status ? `&status=${encodeURIComponent(status)}` : ''
  return request<ApiPage<MallOrder>>(`/api/mall/orders?page=1&pageSize=50${statusQuery}`)
}

export interface MallOrderItem {
  id: string
  productId: string
  productName: string
  productImage: string
  specName: string
  quantity: number
  unitPrice: number
  itemAmount: number
  isGift: boolean
  promotionName: string
  promotionSnapshot: string
  promotionDiscountAmount: number
  badge: string
  originalUnitPrice: number
  reviewed?: boolean
}

export interface MallOrder {
  id: string
  orderNo: string
  status: string
  receiverName: string
  goodsAmount: number
  promotionDiscountAmount: number
  couponDiscountAmount: number
  couponId: string | null
  couponName: string
  payableAmount: number
  paidAmount: number
  freightAmount: number
  createdAt: string
  shipment: MallShipment | null
  items: MallOrderItem[]
}

export interface MallShipment {
  id: string
  shipmentNo: string
  expressCompany: string
  expressPhone: string
  trackingNo: string
  status: string
  remark: string
  shippedAt: string | null
  deliveredAt: string | null
}

export function getAddresses(): Promise<MallAddress[]> {
  const memberId = getMemberId()
  return request<MallAddress[]>(`/api/mall/addresses?userId=${memberId}`)
}

export function saveAddress(address: Partial<MallAddress> & {
  receiverName: string
  receiverPhone: string
  provinceName: string
  cityName: string
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
  provinceName: string
  cityName: string
  districtName: string
  detailAddress: string
  fullAddress: string
  isDefault: boolean
}

export function createOrder(data: CreateOrderRequest): Promise<MallOrderDetail> {
  return request<MallOrderDetail>('/api/mall/orders', 'POST', data)
}

export interface OrderPromotionDetail {
  promotionId: string
  name: string
  discountAmount: number
}

export interface OrderPreview {
  goodsAmount: number
  promotionDiscountAmount: number
  /** 其中折后优惠金额 */
  rateDiscountAmount: number
  /** 其中满减优惠金额 */
  amountDiscountAmount: number
  promotionDetails: OrderPromotionDetail[]
  couponDiscountAmount: number
  couponName: string | null
  freightAmount: number
  payableAmount: number
  /** 赠品清单（0 元，不参与金额，用于提交前展示） */
  gifts: CartGiftItem[]
}

/** 订单确认页价格预览：必须先有收货地址，金额（含运费）以后端为准。 */
export async function previewOrder(
  items: Array<{ skuId: string; quantity: number }>,
  addressId: string,
  memberCouponId?: string
): Promise<OrderPreview> {
  const preview = await request<OrderPreview>('/api/mall/orders/preview', 'POST', { items, memberCouponId, addressId })
  return {
    ...preview,
    gifts: (preview.gifts || []).map(gift => ({
      ...gift,
      productImage: resolveFileUrl(gift.productImage)
    }))
  }
}

export interface CreateOrderRequest {
  addressId: string
  remark: string
  items: Array<{ skuId: string; quantity: number }>
  memberCouponId?: string
}

export interface MallOrderDetail extends MallOrder {
  receiverPhone: string
  receiverAddress: string
  remark: string
  refundedAmount: number
  paidAt: string | null
  shippedAt: string | null
  completedAt: string | null
  cancelledAt: string | null
  refundedAt: string | null
  items: MallOrderItem[]
  payments: Array<{
    id: string
    paymentNo: string
    amount: number
    method: string
    status: string
    transactionNo: string
    failureReason: string
    paidAt: string | null
    createdAt: string
  }>
  refunds: Array<{
    id: string
    refundNo: string
    amount: number
    reason: string
    description: string | null
    evidenceImages: string[]
    status: string
    refundedAt: string | null
    createdAt: string
  }>
  /** 退货/售后单，展示售后进度 */
  returns: MallReturnRecord[]
}

export interface MallReturnRecord {
  id: string
  returnNo: string
  amount: number
  reason: string
  description: string | null
  evidenceImages: string[]
  rejectReason: string | null
  status: string
  expressCompany: string | null
  trackingNo: string | null
  approvedAt: string | null
  rejectedAt: string | null
  shippedAt: string | null
  receivedAt: string | null
  completedAt: string | null
  createdAt: string
}

export interface MallAfterSalePayload {
  reason: string
  description?: string
  evidenceImages?: string[]
}

/** 待发货订单申请仅退款（整单退，金额取订单实付） */
export function createRefund(orderNo: string, payload: MallAfterSalePayload): Promise<unknown> {
  return request(`/api/mall/orders/${encodeURIComponent(orderNo)}/refunds`, 'POST', payload)
}

/** 已发货/已签收订单申请退货退款（整单退） */
export function createReturn(orderNo: string, payload: MallAfterSalePayload): Promise<MallReturnRecord> {
  return request<MallReturnRecord>(
    `/api/mall/orders/${encodeURIComponent(orderNo)}/returns`,
    'POST',
    payload
  )
}

/** 退货审核通过后填写寄回运单 */
export function submitReturnTracking(
  returnNo: string,
  payload: { expressCompany: string; trackingNo: string }
): Promise<MallReturnRecord> {
  return request<MallReturnRecord>(
    `/api/mall/returns/${encodeURIComponent(returnNo)}/ship`,
    'POST',
    payload
  )
}

export function getOrderDetail(orderNo: string): Promise<MallOrderDetail> {
  return request<MallOrderDetail>(`/api/mall/orders/${orderNo}`)
}

export async function cancelOrder(orderNo: string): Promise<boolean> {
  return request<boolean>(`/api/mall/orders/${orderNo}/cancel`, 'POST')
}

export interface MallPaymentResult {
  id: string
  paymentNo: string
  amount: number
  method: string
  status: string
}

export async function createPayment(orderNo: string, method: string = 'Mock'): Promise<MallPaymentResult> {
  return request<MallPaymentResult>(`/api/mall/orders/${orderNo}/payments`, 'POST', { method })
}

export async function simulatePayment(paymentNo: string, success: boolean = true): Promise<boolean> {
  return request<boolean>(`/api/mall/payments/${paymentNo}/simulate`, 'POST', { success })
}

export async function confirmReceipt(orderNo: string): Promise<boolean> {
  return request<boolean>(`/api/mall/orders/${orderNo}/confirm-receipt`, 'POST')
}

export interface ChinaRegionNode {
  code: string
  name: string
  level: number
  parentCode: string
}

let _chinaRegions: ChinaRegionNode[] | null = null
export async function getChinaRegions(): Promise<ChinaRegionNode[]> {
  if (_chinaRegions) return _chinaRegions
  // 该接口返回分页结构；pageSize=0 表示一次性返回全部层级，会员选地址时需要完整地区树
  const data = await request<{ list?: Array<{ code: string; name: string; level: number; parentCode: string }> }>(
    '/api/admin/china-regions?isActive=true&pageSize=0'
  )
  _chinaRegions = (data?.list || []).map(r => ({ code: r.code, name: r.name, level: r.level, parentCode: r.parentCode }))
  return _chinaRegions
}

export function logout(): void {
  clearToken()
  uni.removeStorageSync(MEMBER_ID_STORAGE_KEY)
  deselectedCartSkuIds.clear()
}