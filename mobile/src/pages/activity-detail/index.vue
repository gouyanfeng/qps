<template>
  <view class="page">
    <q-page-header-back :title="activity?.name || '活动详情'" />

    <scroll-view
      scroll-y
      class="page-body"
      enhanced
      @scrolltolower="loadMore"
    >
      <view v-if="activity" class="content">
        <!-- ====== 1. Campaign Hero Header ====== -->
        <view class="hero">
          <view class="hero-motif">
            <text class="hero-motif-text">道地</text>
          </view>
          <view class="hero-body">
            <view class="hero-badges">
              <view class="hero-badge-live">
                <view class="hero-badge-dot"></view>
                <text class="hero-badge-text">{{ activity.subtitle || '官方大促' }}</text>
              </view>
              <view class="hero-badge-cert">
                <q-icon name="certificate" size="15" color="#A3F69C" />
                <text class="hero-badge-cert-text">国医保真 · 产地直供</text>
              </view>
            </view>
            <view class="hero-title-area">
              <text class="hero-title">{{ activity.name }}</text>
            </view>
            <text v-if="activity.description" class="hero-desc">{{ activity.description }}</text>
            <view class="hero-countdown-row">
              <view class="countdown-box">
                <q-icon name="clock" size="18" color="#FFB870" />
                <text class="countdown-label">距活动结束仅剩</text>
                <view class="countdown-digits">
                  <text class="cd-num">{{ cdHours }}</text>
                  <text class="cd-sep">:</text>
                  <text class="cd-num">{{ cdMins }}</text>
                  <text class="cd-sep">:</text>
                  <text class="cd-num">{{ cdSecs }}</text>
                </view>
              </view>
              <text class="rules-link" @tap="showRules">规则说明</text>
            </view>
          </view>
        </view>

        <!-- ====== 2. Category Filter ====== -->
        <view class="filter-bar">
          <scroll-view scroll-x class="category-scroll" enhanced :show-scrollbar="false">
            <view class="category-pills">
              <view
                v-for="cat in categories"
                :key="cat.id"
                class="category-pill"
                :class="{ active: activeCategory === cat.id }"
                @tap="switchCategory(cat.id)"
              >
                <text class="category-pill-text">{{ cat.name }}</text>
              </view>
            </view>
          </scroll-view>
          <view class="sort-row">
            <view class="sort-options">
              <view
                v-for="sort in sortOptions"
                :key="sort.key"
                class="sort-item"
                :class="{ active: activeSort === sort.key }"
                @tap="switchSort(sort.key)"
              >
                <text class="sort-text">{{ sort.label }}</text>
              </view>
            </view>
          </view>
        </view>

        <!-- ====== 4. Product Grid ====== -->
        <view class="product-grid">
          <view
            v-for="item in products"
            :key="item.id"
            class="product-card"
            @tap="goToProductDetail(item.id)"
          >
            <view v-if="item.promoBadge" class="promo-corner" :style="{ backgroundColor: item.promoBadgeColor }">
              <text class="promo-corner-text">{{ item.promoBadge }}</text>
            </view>
            <q-cached-image :src="item.image" mode="aspectFill" class="product-image" />
            <view v-if="item.salesPercent" class="sales-overlay">
              <text class="sales-overlay-text">热销{{ item.salesPercent }}%</text>
            </view>
            <view class="product-info">
              <view class="product-tags">
                <view v-if="item.cert" class="mini-tag tag-primary">
                  <text>{{ item.cert }}</text>
                </view>
                <view v-if="item.certSub" class="mini-tag tag-accent">
                  <text>{{ item.certSub }}</text>
                </view>
              </view>
              <text class="product-name">{{ item.name }}</text>
              <view v-if="item.salesPercent" class="progress-area">
                <view class="progress-track">
                  <view class="progress-fill" :style="{ width: item.salesPercent + '%' }"></view>
                </view>
                <view class="progress-labels">
                  <text class="progress-sold">已抢 {{ item.salesPercent }}%</text>
                  <text class="progress-remain">{{ item.remainText }}</text>
                </view>
              </view>
              <view class="product-bottom">
                <view class="price-col">
                  <view class="price-row">
                    <text class="price-symbol">¥</text>
                    <text class="price-value">{{ item.price }}</text>
                  </view>
                  <text v-if="item.marketPrice > 0" class="price-original">¥{{ item.marketPrice }}</text>
                </view>
                <view class="add-cart-btn" @tap.stop="quickAdd(item)">
                  <q-icon name="add" size="22" color="#ffffff" />
                </view>
              </view>
            </view>
          </view>
        </view>

        <view v-if="!loadingProducts && !products.length" class="empty-products">
          <text class="empty-text">暂无活动商品</text>
        </view>

        <!-- ====== 5. Quality Assurance ====== -->
        <view class="assurance">
          <view class="assurance-items">
            <text class="assurance-item">· 核心产地直采</text>
            <text class="assurance-item">· 中检院全项质检</text>
            <text class="assurance-item">· 顺丰冷链直发</text>
          </view>
          <text class="assurance-note">本草国医馆提供正品品质保证</text>
        </view>
      </view>

      <view v-else-if="!loading" class="empty">
        <text class="empty-text">活动暂不可用</text>
      </view>
    </scroll-view>

    <!-- ====== 6. Floating Cart Bar ====== -->
    <view v-if="activity" class="cart-bar">
      <view class="cart-bar-inner">
        <view class="cart-left" @tap="goToCart">
          <view class="cart-icon-wrap">
            <q-icon name="cart" size="22" color="#2E7D32" />
            <view v-if="cartCount > 0" class="cart-badge">
              <text class="cart-badge-text">{{ cartCount }}</text>
            </view>
          </view>
          <view class="cart-amount-area">
            <view class="cart-amount-row">
              <text class="cart-amount-label">合计:</text>
              <text class="cart-amount-value">¥{{ cartTotal }}</text>
            </view>
          </view>
        </view>
        <view class="checkout-btn" @tap="goToCheckout">
          <text class="checkout-text">去结算</text>
          <q-icon name="chevron-right" size="16" color="#ffffff" />
        </view>
      </view>
    </view>

    <!-- ====== Rules Popup ====== -->
    <view v-if="showRulesPopup" class="popup-mask" @tap="showRulesPopup = false">
      <view class="popup-content" @tap.stop>
        <text class="popup-title">活动规则</text>
        <scroll-view scroll-y class="popup-body">
          <text class="popup-text">{{ activity?.description || '暂无详细规则说明' }}</text>
        </scroll-view>
        <view class="popup-close" @tap="showRulesPopup = false">
          <text class="popup-close-text">我知道了</text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getMallActivity, getCategories, getProducts, addToCart, getCartItems } from '@/api'
import type { MallActivity, Product } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'
import { resolveTagColor } from '@/utils/tagColor'

interface ProductItem {
  id: string
  skuId: string
  name: string
  image: string
  price: string
  marketPrice: number
  cert: string
  certSub: string
  salesPercent: number
  remainText: string
  promoBadge?: string
  promoBadgeColor: string
  weightGram: number
  skuSpecName: string
}

const activity = ref<MallActivity | null>(null)
const loading = ref(true)
const loadingProducts = ref(false)
const products = ref<ProductItem[]>([])
const productPage = ref(1)
const hasMore = ref(true)
const activeCategory = ref('all')
const activeSort = ref('default')
const cartCount = ref(0)
const cartTotal = ref('0.0')
const showRulesPopup = ref(false)

const categories = ref([
  { id: 'all', name: '全部推荐' }
])

const sortOptions = [
  { key: 'default', label: '综合' },
  { key: 'sales', label: '销量' },
  { key: 'price', label: '价格' }
]

let cdInterval: ReturnType<typeof setInterval> | null = null
const cdHours = ref('14')
const cdMins = ref('28')
const cdSecs = ref('45')

onLoad(async (options) => {
  if (!options?.id) {
    loading.value = false
    uni.showToast({ title: '活动链接无效', icon: 'none' })
    return
  }

  try {
    const [act, cats, cartItems] = await Promise.all([
      getMallActivity(options.id),
      getCategories().catch(() => []),
      loadCartSafely()
    ])
    activity.value = act
    if (cats.length) {
      categories.value = [
        { id: 'all', name: '全部推荐' },
        ...cats.map(c => ({ id: c.id, name: c.name }))
      ]
    }
    updateCartDisplay(cartItems)
    startCountdown(act.endDate)
    await loadProducts()
  } catch (error) {
    console.error('加载活动详情失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '活动暂不可用', icon: 'none' })
  } finally {
    loading.value = false
  }
})

onUnmounted(() => {
  if (cdInterval) clearInterval(cdInterval)
})

function startCountdown(endDate: string | null) {
  if (cdInterval) clearInterval(cdInterval)

  let target = endDate ? new Date(endDate).getTime() : Date.now() + 14 * 3600000 + 28 * 60000 + 45000
  let diff = Math.max(0, Math.floor((target - Date.now()) / 1000))

  function update() {
    diff = Math.max(0, Math.floor((target - Date.now()) / 1000))
    const h = Math.floor(diff / 3600)
    const m = Math.floor((diff % 3600) / 60)
    const s = diff % 60
    cdHours.value = String(h).padStart(2, '0')
    cdMins.value = String(m).padStart(2, '0')
    cdSecs.value = String(s).padStart(2, '0')
  }

  update()
  cdInterval = setInterval(update, 1000)
}

async function loadProducts() {
  if (loadingProducts.value) return
  loadingProducts.value = true
  try {
    const categoryId = activeCategory.value !== 'all' ? activeCategory.value : undefined
    const data = await getProducts(categoryId, undefined, productPage.value, 20, activeSort.value)
    const newItems = data.items.filter(p => p.skus.length > 0).map(mapProductItem)
    if (productPage.value === 1) {
      products.value = newItems
    } else {
      products.value.push(...newItems)
    }
    hasMore.value = data.hasMore
  } catch (error) {
    console.error('加载活动商品失败', error)
  } finally {
    loadingProducts.value = false
  }
}

function loadMore() {
  if (!hasMore.value || loadingProducts.value) return
  productPage.value++
  loadProducts()
}

function mapProductItem(product: Product): ProductItem {
  // 列表价取所有规格中的最低促销价
  const sku = product.skus.reduce((min, item) => (item.price < min.price ? item : min), product.skus[0])
  const displayTags = product.tags.filter(t => t.type !== 'Promotion')
  const promoTag = product.tags.find(t => t.type === 'Promotion')
  const salesPercent = product.sales > 0 ? Math.min(99, Math.round(product.sales / (product.sales + Math.max(1, sku.stock)) * 100)) : 0
  const remainText = sku.stock <= 5 ? `仅剩${sku.stock}件` : sku.stock <= 20 ? '库存紧张' : ''

  return {
    id: product.id,
    skuId: sku.id,
    name: product.name,
    image: product.images[0] || '',
    price: sku.price % 1 === 0 ? String(sku.price) : sku.price.toFixed(1),
    marketPrice: sku.marketPrice || 0,
    cert: displayTags[0]?.name || product.categoryName,
    certSub: displayTags[1]?.name || '',
    salesPercent,
    remainText,
    promoBadge: promoTag?.name,
    promoBadgeColor: resolveTagColor(promoTag?.color) || '#C2410C',
    weightGram: sku.weightGram,
    skuSpecName: sku.specName
  }
}

function switchCategory(id: string) {
  if (activeCategory.value === id) return
  activeCategory.value = id
  productPage.value = 1
  products.value = []
  loadProducts()
}

function switchSort(key: string) {
  if (activeSort.value === key) return
  activeSort.value = key
  productPage.value = 1
  products.value = []
  loadProducts()
}

async function quickAdd(item: ProductItem) {
  try {
    const cartItems = await addToCart({
      productId: item.id,
      productName: item.name,
      productImage: item.image,
      skuId: item.skuId,
      specName: item.skuSpecName,
      price: Number(item.price),
      quantity: 1,
      weightGram: item.weightGram,
      selected: true
    })
    updateCartDisplay(cartItems)
    uni.showToast({ title: '已加入购物车', icon: 'none' })
  } catch (error) {
    console.error('加入购物车失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '加入购物车失败', icon: 'none' })
  }
}

async function loadCartSafely() {
  try {
    return await getCartItems()
  } catch {
    return []
  }
}

function updateCartDisplay(items: any[]) {
  const valid = items.filter(i => i.selected && i.isValid !== false)
  cartCount.value = valid.reduce((sum, i) => sum + i.quantity, 0)
  const total = valid.reduce((sum, i) => sum + i.price * i.quantity, 0)
  cartTotal.value = total.toFixed(1)
}

function showRules() {
  showRulesPopup.value = true
}

function goToProductDetail(id: string) {
  uni.navigateTo({ url: `/pages/product-detail/index?id=${id}` })
}

function goToCart() {
  uni.switchTab({ url: '/pages/cart/index' })
}

function goToCheckout() {
  if (cartCount.value === 0) {
    uni.showToast({ title: '购物车是空的', icon: 'none' })
    return
  }
  uni.navigateTo({ url: '/pages/checkout/index' })
}
</script>

<style lang="scss" scoped>
$primary: #2E7D32;
$primary-dark: #1B5E20;
$primary-light: #E8F5E9;
$accent: #FF9800;
$accent-light: #FFF3E0;
$tertiary: #923357;
$bg: #FCF9F8;
$card: #FFFFFF;
$text-primary: #1B1C1C;
$text-secondary: #6B6B6B;
$text-hint: #9E9E9E;
$line: #E8E5E3;
$error: #F44336;
$shadow-soft: 0 4rpx 20rpx -6rpx rgba(46, 125, 50, 0.1);

.page {
  min-height: 100vh;
  background: $bg;
}

.page-body {
  height: calc(100vh - 100rpx);
  padding-bottom: 140rpx;
}

.content {
  padding-bottom: 48rpx;
}

// ====== Hero Section ======
.hero {
  position: relative;
  margin: 20rpx 24rpx 0;
  padding: 32rpx;
  border-radius: 24rpx;
  background: linear-gradient(135deg, $primary-dark 0%, $primary 50%, #1A5522 100%);
  overflow: hidden;
  box-shadow: 0 8rpx 32rpx -8rpx rgba(27, 94, 32, 0.4);
}

.hero-motif {
  position: absolute;
  right: -20rpx;
  bottom: -24rpx;
  width: 120rpx;
  height: 120rpx;
  border: 4rpx solid rgba(163, 246, 156, 0.1);
  border-radius: 50%;
  display: flex;
  align-items: center;
  justify-content: center;
}

.hero-motif-text {
  font-size: 36rpx;
  font-weight: 700;
  color: rgba(163, 246, 156, 0.1);
  font-family: 'Noto Serif', serif;
}

.hero-body {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  gap: 20rpx;
}

.hero-badges {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.hero-badge-live {
  display: flex;
  align-items: center;
  gap: 8rpx;
  padding: 6rpx 16rpx;
  background: rgba(255, 152, 0, 0.3);
  border: 1rpx solid rgba(255, 220, 190, 0.2);
  border-radius: 40rpx;
  backdrop-filter: blur(8px);
}

.hero-badge-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 50%;
  background: $accent;
}

.hero-badge-text {
  font-size: 20rpx;
  font-weight: 500;
  color: #FFDCBE;
}

.hero-badge-cert {
  display: flex;
  align-items: center;
  gap: 6rpx;
}

.hero-badge-cert-text {
  font-size: 20rpx;
  font-weight: 500;
  color: #CBFFC2;
}

.hero-title-area {
  display: flex;
  align-items: baseline;
}

.hero-title {
  font-size: 40rpx;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: 2rpx;
  font-family: 'Noto Serif', serif;
}

.hero-desc {
  font-size: 24rpx;
  color: rgba(203, 255, 194, 0.9);
  line-height: 1.6;
}

.hero-countdown-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16rpx 24rpx;
  background: rgba(0, 0, 0, 0.25);
  border: 1rpx solid rgba(255, 255, 255, 0.1);
  border-radius: 20rpx;
  backdrop-filter: blur(8px);
}

.countdown-box {
  display: flex;
  align-items: center;
  gap: 10rpx;
}

.countdown-label {
  font-size: 20rpx;
  font-weight: 500;
  color: #FFB870;
}

.countdown-digits {
  display: flex;
  align-items: center;
  gap: 6rpx;
  margin-left: 8rpx;
}

.cd-num {
  padding: 4rpx 10rpx;
  background: $accent;
  color: #ffffff;
  font-size: 24rpx;
  font-weight: 700;
  border-radius: 8rpx;
  font-family: monospace;
}

.cd-sep {
  color: #ffffff;
  font-size: 24rpx;
  font-weight: 700;
}

.rules-link {
  font-size: 20rpx;
  font-weight: 500;
  color: #CBFFC2;
  text-decoration: underline dotted;
}

// ====== Filter Bar ======
.filter-bar {
  margin: 20rpx 24rpx 0;
  padding: 16rpx 0 12rpx;
  background: rgba(252, 249, 248, 0.95);
  border-bottom: 1rpx solid $line;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.03);
}

.category-scroll {
  white-space: nowrap;
  margin-bottom: 12rpx;
}

.category-pills {
  display: flex;
  gap: 12rpx;
  padding: 0 8rpx;
}

.category-pill {
  flex-shrink: 0;
  padding: 10rpx 28rpx;
  border-radius: 40rpx;
  background: #F0EDED;
}

.category-pill.active {
  background: $primary;
  box-shadow: 0 2rpx 8rpx rgba(46, 125, 50, 0.2);
}

.category-pill-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #40493D;
}

.category-pill.active .category-pill-text {
  color: #ffffff;
}

.sort-row {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 16rpx;
}

.sort-options {
  display: flex;
  gap: 32rpx;
}

.sort-item {
  display: flex;
  align-items: center;
}

.sort-text {
  font-size: 22rpx;
  font-weight: 500;
  color: $text-hint;
}

.sort-item.active .sort-text {
  color: $primary;
  font-weight: 700;
}

// ====== Product Grid ======
.product-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 16rpx;
  padding: 20rpx 24rpx 0;
}

.product-card {
  position: relative;
  background: $card;
  border-radius: 24rpx;
  padding: 20rpx;
  border: 1rpx solid #F0EDED;
  box-shadow: $shadow-soft;
  overflow: hidden;
}

.promo-corner {
  position: absolute;
  top: 16rpx;
  left: 16rpx;
  z-index: 2;
  padding: 4rpx 12rpx;
  border-radius: 10rpx;
}

.promo-corner-text {
  color: #ffffff;
  font-size: 20rpx;
  font-weight: 600;
}

.product-image {
  width: 100%;
  aspect-ratio: 1;
  border-radius: 16rpx;
  display: block;
}

.sales-overlay {
  position: absolute;
  bottom: calc(100% - 300rpx);
  right: 28rpx;
  padding: 2rpx 8rpx;
  background: rgba(0, 0, 0, 0.6);
  border-radius: 6rpx;
  backdrop-filter: blur(4px);
}

.sales-overlay-text {
  font-size: 18rpx;
  color: #ffffff;
  font-family: monospace;
}

.product-info {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
  margin-top: 12rpx;
}

.product-tags {
  display: flex;
  align-items: center;
  gap: 8rpx;
}

.mini-tag {
  padding: 2rpx 10rpx;
  border-radius: 6rpx;
  font-size: 18rpx;
  font-weight: 500;
}

.tag-primary {
  background: rgba(13, 99, 27, 0.1);
  color: $primary;
}

.tag-accent {
  background: rgba(255, 220, 190, 0.5);
  color: #2C1600;
}

.product-name {
  font-size: 26rpx;
  font-weight: 600;
  color: $text-primary;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.progress-area {
  display: flex;
  flex-direction: column;
  gap: 4rpx;
  padding-top: 4rpx;
}

.progress-track {
  width: 100%;
  height: 10rpx;
  background: #EAE7E7;
  border-radius: 10rpx;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: $accent;
  border-radius: 10rpx;
}

.progress-labels {
  display: flex;
  justify-content: space-between;
}

.progress-sold {
  font-size: 18rpx;
  color: $text-hint;
}

.progress-remain {
  font-size: 18rpx;
  color: $error;
  font-weight: 500;
}

.product-bottom {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  padding-top: 8rpx;
}

.price-col {
  display: flex;
  flex-direction: column;
}

.price-row {
  display: flex;
  align-items: baseline;
}

.price-symbol {
  font-size: 22rpx;
  font-weight: 700;
  color: $error;
}

.price-value {
  font-size: 34rpx;
  font-weight: 700;
  color: $error;
  line-height: 1;
  margin-left: 4rpx;
}

.price-original {
  font-size: 20rpx;
  color: #707A6C;
  text-decoration: line-through;
}

.add-cart-btn {
  width: 56rpx;
  height: 56rpx;
  border-radius: 50%;
  background: $primary;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4rpx 12rpx rgba(46, 125, 50, 0.2);
}

.empty-products {
  padding: 80rpx 24rpx;
  text-align: center;
}

.empty-text {
  font-size: 28rpx;
  color: $text-hint;
}

// ====== Assurance ======
.assurance {
  padding: 24rpx 0;
  text-align: center;
}

.assurance-items {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 20rpx;
  margin-bottom: 8rpx;
}

.assurance-item {
  font-size: 22rpx;
  color: #707A6C;
}

.assurance-note {
  font-size: 22rpx;
  color: rgba(107, 107, 107, 0.7);
}

// ====== Cart Bar ======
.cart-bar {
  position: fixed;
  bottom: 0;
  left: 0;
  right: 0;
  z-index: 40;
  padding: 16rpx 24rpx;
  padding-bottom: calc(16rpx + env(safe-area-inset-bottom));
  background: rgba(255, 255, 255, 0.95);
  backdrop-filter: blur(16px);
  border-top: 1rpx solid rgba(229, 226, 225, 0.8);
  box-shadow: 0 -4rpx 40rpx rgba(0, 0, 0, 0.06);
}

.cart-bar-inner {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 24rpx;
}

.cart-left {
  display: flex;
  align-items: center;
  gap: 20rpx;
  flex: 1;
  min-width: 0;
}

.cart-icon-wrap {
  position: relative;
  width: 72rpx;
  height: 72rpx;
  border-radius: 50%;
  background: #F0EDED;
  display: flex;
  align-items: center;
  justify-content: center;
  border: 1rpx solid #EAE7E7;
  flex-shrink: 0;
}

.cart-badge {
  position: absolute;
  top: -8rpx;
  right: -8rpx;
  width: 32rpx;
  height: 32rpx;
  border-radius: 50%;
  background: $error;
  display: flex;
  align-items: center;
  justify-content: center;
}

.cart-badge-text {
  font-size: 18rpx;
  font-weight: 700;
  color: #ffffff;
}

.cart-amount-area {
  display: flex;
  flex-direction: column;
  min-width: 0;
}

.cart-amount-row {
  display: flex;
  align-items: baseline;
  gap: 6rpx;
}

.cart-amount-label {
  font-size: 22rpx;
  color: $text-hint;
}

.cart-amount-value {
  font-size: 30rpx;
  font-weight: 700;
  color: $error;
}

.checkout-btn {
  display: flex;
  align-items: center;
  gap: 8rpx;
  padding: 18rpx 32rpx;
  background: linear-gradient(135deg, $primary, $primary-dark);
  border-radius: 20rpx;
  box-shadow: 0 4rpx 16rpx rgba(46, 125, 50, 0.3);
  flex-shrink: 0;
}

.checkout-text {
  font-size: 26rpx;
  font-weight: 600;
  color: #ffffff;
}

// ====== Empty ======
.empty {
  padding: 160rpx 24rpx;
  text-align: center;
}

// ====== Rules Popup ======
.popup-mask {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  bottom: 0;
  z-index: 100;
  background: rgba(0, 0, 0, 0.5);
  display: flex;
  align-items: center;
  justify-content: center;
}

.popup-content {
  width: 80%;
  max-height: 70vh;
  background: $card;
  border-radius: 24rpx;
  overflow: hidden;
  display: flex;
  flex-direction: column;
}

.popup-title {
  padding: 32rpx;
  font-size: 32rpx;
  font-weight: 700;
  color: $text-primary;
  text-align: center;
  border-bottom: 1rpx solid $line;
}

.popup-body {
  flex: 1;
  padding: 32rpx;
  max-height: 50vh;
}

.popup-text {
  font-size: 26rpx;
  color: $text-secondary;
  line-height: 1.8;
}

.popup-close {
  padding: 24rpx 32rpx;
  border-top: 1rpx solid $line;
  text-align: center;
}

.popup-close-text {
  font-size: 28rpx;
  font-weight: 600;
  color: $primary;
}
</style>