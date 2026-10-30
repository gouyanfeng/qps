export interface Category {
  id: string
  name: string
  icon: string
  children: CategoryChild[]
}

export interface CategoryChild {
  id: string
  name: string
  icon: string
}

export interface ProductTag {
  id: string
  code: string
  name: string
  color: string
  sortOrder: number
  isActive: boolean
}

export interface Product {
  id: string
  code: string
  name: string
  categoryId: string | null
  categoryName: string
  description: string
  images: string[]
  skus: Sku[]
  sales: number
  isHot: boolean
  tags: ProductTag[]
}

export interface Sku {
  id: string
  specName: string
  price: number
  marketPrice: number
  stock: number
  weightGram: number
}

export interface Banner {
  id: string
  image: string
  title: string
  link: string
  linkType: BannerLinkType
}

export enum BannerLinkType {
  InternalProduct = 0,
  InternalCategory = 1,
  InternalPage = 2,
  ExternalUrl = 3,
  InternalActivity = 4
}

export interface MallActivityPromotion {
  id: string
  promotionId: string
  name: string
  description: string
  triggerType: string
  actionType: string
  startDate: string | null
  endDate: string | null
  isActive: boolean
}

export interface MallActivity {
  id: string
  name: string
  subtitle: string
  coverImage: string
  description: string
  detailHtml: string
  startDate: string | null
  endDate: string | null
  promotions: MallActivityPromotion[]
}

export interface CartItem {
  productId: string
  productName: string
  productImage: string
  skuId: string
  specName: string
  price: number
  quantity: number
  weightGram: number
  selected: boolean
  stock?: number
  availableStock?: number
  isValid?: boolean
  invalidReason?: string | null
  /** 商品图左上角斜角标文案，如「道地药材」 */
  badge?: string
  /** 绿色认证 chip 文案，如「吉林长白山道地认证·一物一码」 */
  certText?: string
  /** 库存紧张红色提示，如「仅剩最后2件，请尽快结算」 */
  stockTip?: string
}