<template>
  <view class="page">
    <!-- ============ 顶栏：统一标题 ============ -->
    <q-page-header-back :show-back="false" title="道地选药" />

    <view class="page-body">
      <!-- ============ 左侧分类导航 ============ -->
      <scroll-view scroll-y class="side" enhanced show-scrollbar="false">
        <view class="seal-wrap">
          <view class="brand-seal">
            <text class="seal-char">本</text>
          </view>
        </view>

        <view
          v-for="nav in sideNav"
          :key="nav.key"
          class="side-item"
          @click="selectSide(nav.key)"
        >
          <view :class="['side-card', { active: activeKey === nav.key }]">
            <text class="side-name">{{ nav.label }}</text>
          </view>
        </view>
      </scroll-view>

      <!-- ============ 右侧内容 ============ -->
      <q-scroll-list class="main" :loading="loading" :has-more="hasMore" more-text="上拉加载更多" @loadmore="loadMore">
        <!-- 筛选胶囊 -->
        <scroll-view scroll-x enhanced show-scrollbar="false" class="filter-scroll">
          <view class="filter-row">
            <view
              v-for="tab in filterTabs"
              :key="tab.id"
              :class="['filter-pill', { active: activeFilter === tab.id }]"
              @click="selectFilter(tab.id)"
            >
              <text>{{ tab.name }}</text>
            </view>
          </view>
        </scroll-view>

        <!-- 道地直采 Banner -->
        <view class="banner">
          <view class="banner-text">
            <view class="banner-chip">
              <q-icon name="check-circle-filled" size="16" color="#2E7D32" />
              <text class="banner-chip-text">核心产区 · 典范保真</text>
            </view>
            <text class="banner-title">道地直采 · 一物一码</text>
            <text class="banner-sub">严苛甄选 GAP 规范化种植基地</text>
          </view>
          <view class="banner-icon">
            <q-icon name="tree-round-dot" size="44" color="#2E7D32" />
          </view>
        </view>

        <!-- 商品网格 -->
        <view class="product-grid">
          <view
            v-for="p in visibleProducts"
            :key="p.id"
            class="product-card"
            @click="goToProductDetail(p.id)"
          >
            <view class="thumb">
              <q-cached-image :src="p.images[0]" mode="aspectFill" class="thumb-img" />
              <view v-if="promoTagOf(p)" class="promo-corner" :style="{ backgroundColor: resolveTagColor(promoTagOf(p)!.color) || '#C2410C' }">
                <text class="promo-corner-text">{{ promoTagOf(p)!.name }}</text>
              </view>
            </view>
            <view class="p-info">
              <text class="p-name">{{ p.name }}</text>
              <view v-if="displayTags(p).length" class="product-tags">
                <text
                  v-for="tag in displayTags(p)"
                  :key="tag.id"
                  class="product-tag"
                  :style="tagChipStyle(tag.color)"
                >{{ tag.name }}</text>
              </view>
              <text class="p-desc">{{ p.description }}</text>
              <view class="p-divider"></view>
              <view class="p-bottom">
                <view class="price-row">
                  <text class="p-symbol">¥</text>
                  <text class="p-price">{{ formatPrice(minPriceOf(p)) }}</text>
                  <text v-if="crossedPriceOf(p) > 0" class="p-origin">¥{{ formatPrice(crossedPriceOf(p)) }}</text>
                </view>
                <view class="add-btn" @click.stop="quickAdd(p)">
                  <q-icon name="add" size="20" color="#ffffff" />
                </view>
              </view>
            </view>
          </view>
        </view>

        <!-- 空态 -->
        <view v-if="visibleProducts.length === 0 && !loading" class="empty">
          <q-icon name="cart" size="56" color="#C9C4BA" />
          <text class="empty-text">该分类好物正在上新，敬请期待</text>
        </view>

        <!-- 加载状态由 q-scroll-list 统一展示 -->

        <view class="main-pad"></view>
      </q-scroll-list>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getCategories, getProducts, addToCart } from '@/api'
import type { Category, Product } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'
import { tagChipStyle, resolveTagColor } from '@/utils/tagColor'

interface SideNavItem {
  key: string
  label: string
  categoryIds: string[] | null
}

interface FilterTab {
  id: string
  name: string
}

const sideNav = ref<SideNavItem[]>([])
const products = ref<Product[]>([])
const activeKey = ref('all')
const activeFilter = ref('all')
const activeTagCode = ref('')

const page = ref(1)
const pageSize = 20
const hasMore = ref(true)
const loading = ref(false)

function currentCategoryId(): string | undefined {
  if (activeFilter.value !== 'all') return activeFilter.value
  if (activeKey.value !== 'all') return activeKey.value
  return undefined
}

async function loadProducts(reset = false) {
  if (loading.value) return
  if (reset) {
    page.value = 1
    hasMore.value = true
    products.value = []
  }
  if (!hasMore.value) return
  loading.value = true
  try {
    const catId = currentCategoryId()
    const res = await getProducts(catId, undefined, page.value, pageSize)
    products.value = reset ? res.items : [...products.value, ...res.items]
    hasMore.value = res.hasMore
    page.value++
  } catch (error) {
    console.error('加载商品失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

async function loadMore() {
  if (!hasMore.value || loading.value) return
  await loadProducts(false)
}

onMounted(async () => {
  try {
    const categories = await getCategories()
    sideNav.value = [
      { key: 'all', label: '全部商品', categoryIds: null },
      ...categories.map((category: Category) => ({
        key: category.id,
        label: category.name,
        categoryIds: category.children.map(child => child.id)
      }))
    ]
    applyPendingCategory()
    await loadProducts(true)
  } catch (error) {
    console.error('加载商城分类数据失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '分类数据加载失败', icon: 'none' })
  }
})

function applyPendingCategory() {
  const catId = uni.getStorageSync('pendingCategoryId') as string | undefined
  if (!catId) return
  uni.removeStorageSync('pendingCategoryId')
  const parent = sideNav.value.find(nav => nav.categoryIds?.includes(catId))
  if (parent) {
    activeKey.value = parent.key
    activeFilter.value = catId
    activeTagCode.value = ''
  }
}

onShow(() => {
  applyPendingCategory()
})

// 后端已按 parentId 展开到所有子分类，前端不再二次过滤
const baseProducts = computed(() => products.value)

const filterTabs = computed<FilterTab[]>(() => {
  const nav = sideNav.value.find(item => item.key === activeKey.value)
  const children = nav?.categoryIds || []
  const names = new Map(products.value.map(product => [product.categoryId || '', product.categoryName]))
  return [{ id: 'all', name: '全部' }, ...children.map(id => ({ id, name: names.get(id) || '' })).filter(item => item.name)]
})

const categoryFilteredProducts = computed(() => baseProducts.value.filter(product =>
  activeFilter.value === 'all' || product.categoryId === activeFilter.value
))

const visibleProducts = computed(() => {
  return categoryFilteredProducts.value.filter(product =>
    !activeTagCode.value || product.tags.some(tag => tag.code === activeTagCode.value)
  )
})

function promoTagOf(product: Product) {
  return product.tags.find(tag => tag.type === 'Promotion')
}

function displayTags(product: Product) {
  return product.tags.filter(tag => tag.type !== 'Promotion')
}

function selectSide(key: string) {
  if (activeKey.value === key) return
  activeKey.value = key
  activeFilter.value = 'all'
  activeTagCode.value = ''
  loadProducts(true)
}

function selectFilter(id: string) {
  if (activeFilter.value === id) return
  activeFilter.value = id
  activeTagCode.value = ''
  loadProducts(true)
}

function formatPrice(price: number): string {
  return price % 1 === 0 ? String(price) : price.toFixed(1)
}

// 列表价：所有规格中的最低促销价
function minPriceOf(product: Product): number {
  return product.skus.length ? Math.min(...product.skus.map(sku => sku.price)) : 0
}

// 划线价：最低价规格的市场价；不高于现价时不展示
function crossedPriceOf(product: Product): number {
  if (!product.skus.length) return 0
  const sku = product.skus.reduce((min, item) => (item.price < min.price ? item : min))
  return sku.marketPrice > sku.price ? sku.marketPrice : 0
}

async function quickAdd(p: Product) {
  const sku = p.skus[0]
  if (!sku) return
  try {
    await addToCart({
      productId: p.id,
      productName: p.name,
      productImage: p.images[0] || '',
      skuId: sku.id,
      specName: sku.specName,
      price: sku.price,
      quantity: 1,
      weightGram: sku.weightGram,
      selected: true
    })
    uni.showToast({ title: '已加入购物车', icon: 'none' })
  } catch (error) {
    console.error('加入购物车失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '加入购物车失败', icon: 'none' })
  }
}

function goToProductDetail(id: string) {
  uni.navigateTo({ url: `/pages/product-detail/index?id=${id}` })
}

</script>

<style lang="scss" scoped>
$side-bg-top: #FBF8F0;
$side-bg-bottom: #F3EDDE;
$side-text: #6B5145;
$side-green: #2C5F24;
$side-red: #C62828;

$theme-primary: #2E7D32;
$main-bg: #FCFAF6;
$on-surface: #1B1C1C;
$on-surface-variant: #6B6B6B;
$line: #E8E3D6;
$price: #8B5000;

.page {
  display: flex;
  flex-direction: column;
  // tabBar 页：100vh 会超出 uni-page-wrapper（H5 已扣掉底部 50px），用 --window-bottom 扣除
  height: calc(100vh - var(--window-bottom, 0px));
  background: var(--page-bg);
  overflow: hidden;
}

// ============ 顶栏内容 ============
.brand {
  display: flex;
  align-items: center;
  gap: 4rpx;
  flex-shrink: 0;
}
.brand-text {
  font-size: 34rpx;
  font-weight: 800;
  color: $theme-primary;
  letter-spacing: 1rpx;
}
.search-pill {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 8rpx;
  height: 62rpx;
  padding: 0 20rpx;
  background: #F5F3ED;
  border-radius: 100rpx;
}
.search-text {
  font-size: 21rpx;
  color: #8A8578;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.notice {
  position: relative;
  flex-shrink: 0;
  width: 48rpx;
  height: 48rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}
.notice-dot {
  position: absolute;
  top: 4rpx;
  right: 2rpx;
  width: 12rpx;
  height: 12rpx;
  border-radius: 50%;
  background: #E53935;
}

.page-body {
  flex: 1;
  display: flex;
  min-height: 0;
}

// ============ 左侧导航 ============
.side {
  width: 178rpx;
  flex-shrink: 0;
  height: 100%;
  box-sizing: border-box;
  padding: 24rpx 12rpx 40rpx;
  background: linear-gradient(180deg, $side-bg-top 0%, $side-bg-bottom 100%);
  border-right: 1rpx solid #E7E0CF;
  position: relative;
}
.side::before {
  content: '';
  position: absolute;
  inset: 0;
  background-image: radial-gradient(circle at 24rpx 24rpx, rgba(196, 182, 150, 0.14) 2rpx, transparent 3rpx);
  background-size: 40rpx 40rpx;
  pointer-events: none;
}
.seal-wrap {
  display: flex;
  justify-content: center;
  padding: 6rpx 0 26rpx;
  position: relative;
  z-index: 1;
}
.brand-seal {
  width: 58rpx;
  height: 58rpx;
  border-radius: 50%;
  background: $side-red;
  border: 2rpx solid rgba(255, 255, 255, 0.6);
  box-shadow: 0 4rpx 12rpx rgba(198, 40, 40, 0.3);
  display: flex;
  align-items: center;
  justify-content: center;
}
.seal-char {
  font-size: 26rpx;
  font-weight: 700;
  color: #ffffff;
  line-height: 1;
}
.side-item {
  position: relative;
  z-index: 1;
  margin-bottom: 12rpx;
}
.side-card {
  position: relative;
  min-height: 78rpx;
  padding: 12rpx 6rpx;
  border-radius: 14rpx;
  background: #FFFDF8;
  border: 1rpx solid #EDE7D8;
  display: flex;
  align-items: center;
  justify-content: center;
  box-sizing: border-box;
}
.side-card.active {
  min-height: 116rpx;
  padding: 12rpx 8rpx;
  background: linear-gradient(160deg, #2E6827 0%, #2C5F24 60%, #275722 100%);
  border-color: transparent;
  border-radius: 16rpx;
  box-shadow: 0 8rpx 20rpx -6rpx rgba(44, 95, 36, 0.45);
}
.side-name {
  font-size: 23rpx;
  font-weight: 600;
  color: $side-text;
  line-height: 1.3;
  text-align: center;
  white-space: nowrap;
}
.side-card.active .side-name {
  width: 92rpx;
  font-size: 28rpx;
  font-weight: 700;
  color: #ffffff;
  white-space: normal;
  letter-spacing: 2rpx;
}
.side::after {
  content: '本草';
  position: absolute;
  top: 92rpx;
  left: 0;
  right: 0;
  text-align: center;
  font-size: 46rpx;
  font-weight: 700;
  color: rgba(198, 40, 40, 0.05);
  letter-spacing: -2rpx;
  pointer-events: none;
}

// ============ 右侧内容 ============
.main {
  flex: 1;
  min-width: 0;
  height: 100%;
  padding: 20rpx 0 0;
  box-sizing: border-box;
}

.filter-scroll {
  width: 100%;
  white-space: nowrap;
}
.filter-row {
  display: inline-flex;
  align-items: center;
  gap: 14rpx;
  padding: 0 20rpx;
}
.filter-pill {
  flex-shrink: 0;
  height: 54rpx;
  padding: 0 26rpx;
  display: flex;
  align-items: center;
  border-radius: 100rpx;
  background: #ffffff;
  border: 1rpx solid #E5DFD1;
  font-size: 22rpx;
  color: #3A3A38;
  line-height: 1;
}
.filter-pill.active {
  background: #2E6B2A;
  border-color: #2E6B2A;
  color: #ffffff;
  font-weight: 700;
}

.banner {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin: 18rpx 20rpx 0;
  padding: 22rpx 20rpx;
  background: #FBF6E8;
  border: 1rpx solid #EEE4CE;
  border-radius: 18rpx;
}
.banner-text {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 6rpx;
}
.banner-chip {
  display: inline-flex;
  align-items: center;
  gap: 4rpx;
  align-self: flex-start;
}
.banner-chip-text {
  font-size: 20rpx;
  font-weight: 700;
  color: $theme-primary;
}
.banner-title {
  font-size: 33rpx;
  font-weight: 800;
  color: #1F3A1C;
  letter-spacing: 1rpx;
}
.banner-sub {
  font-size: 20rpx;
  color: #7A7568;
}
.banner-icon {
  width: 108rpx;
  height: 108rpx;
  flex-shrink: 0;
  border-radius: 50%;
  background: #E7F1DF;
  display: flex;
  align-items: center;
  justify-content: center;
}

// ============ 商品网格 ============
.product-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20rpx;
  padding: 18rpx 20rpx 0;
}
.product-card {
  background: #ffffff;
  border-radius: 16rpx;
  overflow: hidden;
  box-shadow: 0 4rpx 16rpx -8rpx rgba(46, 125, 50, 0.12);
}
.thumb {
  position: relative;
  margin: 14rpx 14rpx 0;
  border-radius: 12rpx;
  overflow: hidden;
}
.promo-corner {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 2;
  padding: 4rpx 14rpx;
  border-radius: 12rpx 0 12rpx 12rpx;
}
.promo-corner-text {
  color: #ffffff;
  font-size: 20rpx;
  line-height: 30rpx;
}
.thumb-img {
  width: 100%;
  height: 196rpx;
  display: block;
}
.p-info {
  padding: 14rpx 18rpx 16rpx;
  display: flex;
  flex-direction: column;
  gap: 6rpx;
}
.p-name {
  font-size: 24rpx;
  font-weight: 700;
  color: $on-surface;
  line-height: 1.35;
  height: 66rpx;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}
.p-desc {
  font-size: 19rpx;
  color: #8A8578;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
.product-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8rpx;
  margin-top: 8rpx;
}
.product-tag, .tag-filter {
  padding: 4rpx 12rpx;
  border: 1rpx solid #d5e3d1;
  border-radius: 20rpx;
  color: #52714f;
  font-size: 19rpx;
}
.tag-filter-scroll { padding: 0 24rpx 12rpx; }
.tag-filter.active { color: #fff; background: #2e7d32; border-color: #2e7d32; }
.p-divider {
  height: 1rpx;
  background: #EFEAE0;
  margin: 6rpx 0 4rpx;
}
.p-bottom {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
}
.price-row {
  display: flex;
  align-items: baseline;
  gap: 2rpx;
  min-width: 0;
}
.p-symbol {
  font-size: 20rpx;
  font-weight: 700;
  color: $price;
}
.p-price {
  font-size: 34rpx;
  font-weight: 800;
  color: $price;
  line-height: 1;
}
.p-origin {
  margin-left: 6rpx;
  font-size: 17rpx;
  color: #B3AEA2;
  text-decoration: line-through;
}
.add-btn {
  width: 46rpx;
  height: 46rpx;
  flex-shrink: 0;
  border-radius: 50%;
  background: $theme-primary;
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4rpx 10rpx rgba(46, 125, 50, 0.3);
}

.empty {
  padding: 100rpx 40rpx;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20rpx;
}
.empty-text {
  font-size: 22rpx;
  color: #A9A498;
}
.main-pad {
  height: 40rpx;
}
.load-footer {
  display: flex;
  justify-content: center;
  padding: 24rpx 0 8rpx;
}
.load-text {
  font-size: 22rpx;
  color: #A9A498;
}
</style>