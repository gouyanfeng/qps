import type { Category, Product, Banner, CartItem } from './types'
import categoriesData from '@/mock/categories.json'
import productsData from '@/mock/products.json'
import bannersData from '@/mock/banners.json'

const CART_STORAGE_KEY = 'qps_cart_items'
const SEED_FLAG_KEY = 'qps_cart_seeded_v2'

/** 演示用初始购物车数据，与设计稿一致（2 件已选 + 1 件未选，合计 ¥434.00） */
function createSeedCart(): CartItem[] {
  return [
    {
      productId: '1',
      productName: '长白山特级十五年野生生晒人参礼盒',
      productImage: '/static/images/herb-ginseng-box.jpg',
      skuId: '1-600',
      specName: '精选特级 600g礼盒装',
      price: 298,
      quantity: 1,
      weightGram: 600,
      selected: true,
      badge: '道地药材',
      certText: '吉林长白山道地认证·一物一码'
    },
    {
      productId: '2',
      productName: '岷县原生态特级头采当归片 500g',
      productImage: '/static/images/herb-danggui.jpg',
      skuId: '2-500',
      specName: '无硫熏蒸 500g 罐装',
      price: 68,
      quantity: 2,
      weightGram: 500,
      selected: true,
      badge: '头茬原产',
      stockTip: '仅剩最后2件，请尽快结算'
    },
    {
      productId: '3',
      productName: '新会核心产区十年老树陈皮 250g',
      productImage: '/static/images/herb-chenpi.jpg',
      skuId: '3-250',
      specName: '干仓陈化 250g 密封礼盒',
      price: 188,
      quantity: 1,
      weightGram: 250,
      selected: false,
      badge: '十年陈化',
      stockTip: '库存紧张（仅剩1件）'
    }
  ]
}

export function getCategories(): Category[] {
  return categoriesData.categories
}

export function getProducts(categoryId?: string): Product[] {
  if (categoryId) {
    return productsData.products.filter(p => p.categoryId === categoryId)
  }
  return productsData.products
}

export function getProductById(id: string): Product | undefined {
  return productsData.products.find(p => p.id === id)
}

export function getHotProducts(): Product[] {
  return productsData.products.filter(p => p.isHot)
}

export function searchProducts(keyword: string): Product[] {
  const lowerKeyword = keyword.toLowerCase()
  return productsData.products.filter(p =>
    p.name.toLowerCase().includes(lowerKeyword) ||
    p.description.toLowerCase().includes(lowerKeyword) ||
    p.categoryName.toLowerCase().includes(lowerKeyword)
  )
}

export function getBanners(): Banner[] {
  return bannersData.banners
}

export function getCartItems(): CartItem[] {
  try {
    // 演示数据只播种一次，之后以用户操作为准
    const seeded = uni.getStorageSync(SEED_FLAG_KEY)
    if (!seeded) {
      const seed = createSeedCart()
      uni.setStorageSync(CART_STORAGE_KEY, JSON.stringify(seed))
      uni.setStorageSync(SEED_FLAG_KEY, '1')
      return seed
    }
    const items = uni.getStorageSync(CART_STORAGE_KEY)
    return items ? JSON.parse(items) : []
  } catch {
    return []
  }
}

export function saveCartItems(items: CartItem[]): void {
  uni.setStorageSync(CART_STORAGE_KEY, JSON.stringify(items))
}

export function addToCart(item: CartItem): CartItem[] {
  const items = getCartItems()
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

export function updateCartItemQuantity(skuId: string, quantity: number): CartItem[] {
  const items = getCartItems()
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

export function removeCartItem(skuId: string): CartItem[] {
  const items = getCartItems().filter(i => i.skuId !== skuId)
  saveCartItems(items)
  return items
}

export function toggleCartItemSelection(skuId: string): CartItem[] {
  const items = getCartItems()
  const item = items.find(i => i.skuId === skuId)
  if (item) {
    item.selected = !item.selected
    saveCartItems(items)
  }
  return items
}

export function clearCart(): void {
  saveCartItems([])
}