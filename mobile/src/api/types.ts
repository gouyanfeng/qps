export interface Category {
  id: string
  name: string
  icon: string
  children: CategoryChild[]
}

export interface CategoryChild {
  id: string
  name: string
}

export interface Product {
  id: string
  name: string
  categoryId: string
  categoryName: string
  description: string
  images: string[]
  skus: Sku[]
  sales: number
  isHot: boolean
}

export interface Sku {
  id: string
  specName: string
  price: number
  stock: number
  weightGram: number
}

export interface Banner {
  id: string
  image: string
  title: string
  link: string
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
  /** 商品图左上角斜角标文案，如「道地药材」 */
  badge?: string
  /** 绿色认证 chip 文案，如「吉林长白山道地认证·一物一码」 */
  certText?: string
  /** 库存紧张红色提示，如「仅剩最后2件，请尽快结算」 */
  stockTip?: string
}