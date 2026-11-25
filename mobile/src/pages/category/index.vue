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
              <q-image :src="p.images[0]" thumb :width="375" mode="aspectFill" class="thumb-img" />
              <view v-if="p.promotionBadgeText" class="promo-corner" :style="{ backgroundColor: p.promotionBadgeColor || '#C2410C' }">
                <text class="promo-corner-text">{{ p.promotionBadgeText }}</text>
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
                  <text v-if="originalPriceOf(p) > 0" class="p-origin">¥{{ formatPrice(originalPriceOf(p)) }}</text>
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
import { tagChipStyle } from '@/utils/tagColor'

interface SideNavItem {
  key: string
  label: string
  categoryIds: string[] | null
  /** 二级类目完整列表，来自分类树，不随商品筛选变化 */
  tabs: FilterTab[]
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
      { key: 'all', label: '全部商品', categoryIds: null, tabs: [] },
      ...categories.map((category: Category) => ({
        key: category.id,
        label: category.name,
        categoryIds: category.children.map(child => child.id),
        tabs: category.children.map(child => ({ id: child.id, name: child.name }))
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
  // 二级列表始终取分类树，不依赖当前已加载商品，避免点击后其他二级消失
  return [{ id: 'all', name: '全部' }, ...(nav?.tabs ?? [])]
})

const categoryFilteredProducts = computed(() => baseProducts.value.filter(product =>
  activeFilter.value === 'all' || product.categoryId === activeFilter.value
))

const visibleProducts = computed(() => {
  return categoryFilteredProducts.value.filter(product =>
    !activeTagCode.value || product.tags.some(tag => tag.code === activeTagCode.value)
  )
})

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

// 列表价规格：按折后价最低的一条；折后价与折扣标签均由后端计算
function lowestSkuOf(product: Product) {
  if (!product.skus.length) return null
  return product.skus.reduce((min, item) =>
    (item.discountedPrice ?? item.price) < (min.discountedPrice ?? min.price) ? item : min)
}

function minPriceOf(product: Product): number {
  const sku = lowestSkuOf(product)
  return sku ? sku.discountedPrice ?? sku.price : 0
}

// 划线价：有折扣时显示该规格原价，无折扣不展示
function originalPriceOf(product: Product): number {
  const sku = lowestSkuOf(product)
  return sku && sku.discountedPrice !== null && sku.discountedPrice < sku.price ? sku.price : 0
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
@import "./index.scss";
</style>