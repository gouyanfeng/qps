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
  type: string
  code: string
  name: string
  color: string
  sortOrder: number
  isActive: boolean
}

export interface MallTagItem {
  id: string
  type: string
  code: string
  name: string
  color: string
  sortOrder: number
  isActive: boolean
}

export interface ProductPromotion {
  id: string
  name: string
  description: string
  promotionType: number
  thresholdAmount: number
  actionType: string
  actionConfigJson: string
  scopeType: string
  ruleText: string
}

export interface Product {
  id: string
  code: string
  name: string
  categoryId: string | null
  categoryName: string
  storeName: string
  description: string
  detailHtml: string
  images: string[]
  skus: Sku[]
  sales: number
  isHot: boolean
  tags: ProductTag[]
  promotions: ProductPromotion[]
  promotionBadgeText: string
  promotionBadgeColor: string
}

export interface Sku {
  id: string
  specName: string
  price: number
  marketPrice: number
  stock: number
  weightGram: number
  discountedPrice: number | null
  discountLabel: string
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
  InternalCampaign = 6,
  InternalPromotion = 7
}

export interface CartItem {
  productId: string
  productName: string
  productImage: string
  skuId: string
  specName: string
  price: number
  /** 折后价（后端按最优折扣计算，无折扣时等于 price） */
  discountedPrice?: number
  /** 折扣标签，如「8折」；无折扣为空 */
  discountLabel?: string | null
  /** 满减促销角标文案（后端判定，与商品列表同一套口径） */
  promotionBadgeText?: string | null
  /** 满减促销角标背景色 */
  promotionBadgeColor?: string | null
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

export interface ProductReview {
  id: string
  memberNickname: string
  memberLevelName: string
  rating: number
  content: string
  specText: string
  tags: string[]
  images: string[]
  replyContent: string
  reviewTime: string
}

export interface ReviewTagStat {
  tag: string
  count: number
}

export interface ProductReviewPage {
  items: ProductReview[]
  totalCount: number
  positiveRate: number
  allTotalCount: number
  allPositiveRate: number
  tagStats: ReviewTagStat[]
}