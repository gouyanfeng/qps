<template>
  <view class="page">
    <q-page-header-back>
      <template #title>
        <view class="search-box">
          <q-icon name="search" size="22" color="#707A6C" />
          <input
            v-model="keyword"
            class="search-input"
            type="text"
            placeholder="道地人参、黄芪、陈皮..."
            placeholder-class="search-placeholder"
            confirm-type="search"
            :focus="true"
            @confirm="handleSearch"
          />
          <view v-if="keyword" class="clear-btn" @click="handleClear">
            <q-icon name="close" size="16" color="#ffffff" />
          </view>
        </view>
      </template>
      <template #right>
        <view class="search-btn" @click="handleSearch">
          <text class="search-btn-text">搜索</text>
        </view>
      </template>
    </q-page-header-back>

    <scroll-view scroll-y class="page-body" enhanced>
      <!-- ============ 初始态：热门搜索 + 搜索历史 ============ -->
      <view v-if="!hasSearched" class="search-init">
        <view class="hot-searches">
          <view class="section-header">
            <view class="section-head-left">
              <view class="section-dot"></view>
              <text class="section-title">热门搜索</text>
            </view>
            <text class="section-hint">道地药材 · 滋补佳品</text>
          </view>
          <view class="hot-tags">
            <view
              v-for="(tag, index) in hotSearchTags"
              :key="tag"
              :class="['hot-tag', { 'hot-tag-top': index < 3 }]"
              @click="searchByTag(tag)"
            >
              <text v-if="index < 3" class="hot-badge">{{ index + 1 }}</text>
              <text class="hot-tag-text">{{ tag }}</text>
            </view>
          </view>
        </view>

        <view v-if="searchHistory.length > 0" class="search-history">
          <view class="section-header">
            <view class="section-head-left">
              <q-icon name="time" size="18" color="#8A8578" />
              <text class="section-title">搜索历史</text>
            </view>
            <view class="clear-history" @click="clearHistory">
              <q-icon name="delete" size="16" color="#9E9E9E" />
              <text class="clear-text">清空</text>
            </view>
          </view>
          <view class="history-tags">
            <view
              v-for="history in searchHistory"
              :key="history"
              class="history-tag"
              @click="searchByTag(history)"
            >
              <text>{{ history }}</text>
            </view>
          </view>
        </view>

        <view class="init-footer">
          <view class="footer-line"></view>
          <text class="footer-text">核心产区直采 · 中检院全项质检 · 顺丰冷链直发</text>
          <view class="footer-line"></view>
        </view>
      </view>

      <!-- ============ 结果态 ============ -->
      <view v-else class="search-results">
        <view v-if="loading" class="loading-state">
          <q-icon name="loading" size="40" color="#BDBDBD" />
          <text class="loading-text">正在为你甄选道地药材…</text>
        </view>

        <view v-else-if="results.length === 0" class="empty-state">
          <q-icon name="search" size="80" color="#DDDDDD" />
          <text class="empty-text">未找到相关商品</text>
          <text class="empty-hint">换个关键词试试，例如「人参」「黄芪」</text>
        </view>

        <template v-else>
          <!-- 结果数量与溯源存证提示条 -->
          <view class="result-meta">
            <view class="meta-left">
              <view class="meta-dot"></view>
              <text class="meta-text">找到 <text class="meta-count">{{ filteredResults.length }}</text> 件道地药材与滋补品</text>
            </view>
            <view class="meta-right">
              <q-icon name="institution-checked" size="14" color="#2E7D32" />
              <text class="meta-right-text">正品溯源保障</text>
            </view>
          </view>

          <!-- 中药材多维快速筛选胶囊 -->
          <scroll-view scroll-x class="quick-scroll" enhanced show-scrollbar="false">
            <view class="quick-row">
              <view
                v-for="pill in quickFilters"
                :key="pill.key"
                :class="['quick-pill', { active: activeQuick === pill.key }]"
                @click="selectQuick(pill.key)"
              >
                <text>{{ pill.name }}</text>
              </view>
            </view>
          </scroll-view>

          <!-- 综合排序工具栏 -->
          <view class="sort-bar">
            <view class="sort-left">
              <view :class="['sort-item', { active: sortBy === 'default' }]" @click="setSort('default')">
                <text>综合排序</text>
                <view v-if="sortBy === 'default'" class="sort-dot"></view>
              </view>
              <view :class="['sort-item', { active: sortBy === 'sales' }]" @click="setSort('sales')">
                <text>销量</text>
                <view v-if="sortBy === 'sales'" class="sort-dot"></view>
              </view>
              <view :class="['sort-item', { active: sortBy === 'price' }]" @click="togglePriceSort">
                <text>价格</text>
                <text class="sort-arrow">{{ priceAsc ? '↑' : '↓' }}</text>
              </view>
            </view>
            <view :class="['filter-btn', { active: filterOpen }]" @click="filterOpen = !filterOpen">
              <text>筛选</text>
              <q-icon name="setting" size="15" :color="filterOpen ? '#2E7D32' : '#6B6B6B'" />
            </view>
          </view>

          <!-- 筛选面板：全部标签 -->
          <view v-if="filterOpen" class="filter-panel">
            <view class="filter-panel-head">
              <text class="filter-panel-title">按标签筛选</text>
              <text class="filter-reset" @click="selectQuick('all')">重置</text>
            </view>
            <view class="filter-tags">
              <view
                v-for="pill in quickFilters"
                :key="`panel-${pill.key}`"
                :class="['filter-tag', { active: activeQuick === pill.key }]"
                @click="selectQuick(pill.key)"
              >
                <text>{{ pill.name }}</text>
              </view>
            </view>
          </view>

          <!-- 搜索结果商品双列卡片 -->
          <view class="result-grid">
            <view
              v-for="product in filteredResults"
              :key="product.id"
              class="result-card qps-tap"
              @click="goToProductDetail(product.id)"
            >
              <view class="card-thumb">
                <q-image :src="product.images[0]" thumb :width="375" mode="aspectFill" class="thumb-img" />
                <view v-if="product.promotionBadgeText" class="thumb-badge" :style="{ backgroundColor: product.promotionBadgeColor || '#C2410C' }">
                  <text class="thumb-badge-text">{{ product.promotionBadgeText }}</text>
                </view>
                <view v-else-if="product.categoryName" class="thumb-badge thumb-badge-plain">
                  <text class="thumb-badge-text">{{ product.categoryName }}</text>
                </view>
              </view>

              <view class="card-body">
                <view v-if="displayTags(product).length" class="card-tags">
                  <text
                    v-for="tag in displayTags(product).slice(0, 2)"
                    :key="tag.id"
                    class="card-tag"
                    :style="tagChipStyle(tag.color)"
                  >{{ tag.name }}</text>
                </view>
                <text class="card-name">{{ product.name }}</text>
                <text class="card-desc">{{ product.description || product.categoryName }}</text>

                <view class="card-bottom">
                  <view class="price-col">
                    <text v-if="originalPriceOf(product) > 0" class="price-origin">¥{{ formatPrice(originalPriceOf(product)) }}</text>
                    <view class="price-row">
                      <text class="price-symbol">¥</text>
                      <text class="price-value">{{ formatPrice(minPriceOf(product)) }}</text>
                    </view>
                  </view>
                  <view class="add-btn" @click.stop="quickAdd(product)">
                    <q-icon name="add" size="18" color="#ffffff" />
                  </view>
                </view>
              </view>
            </view>
          </view>

          <!-- 关联推荐横滑区 -->
          <view v-if="relatedProducts.length" class="related">
            <view class="related-head">
              <view class="related-head-left">
                <q-icon name="tea" size="20" color="#2E7D32" />
                <text class="related-title">常看关联滋补佳品</text>
              </view>
              <text class="related-hint">馆内医师审方推荐</text>
            </view>
            <scroll-view scroll-x enhanced show-scrollbar="false">
              <view class="related-row">
                <view
                  v-for="item in relatedProducts"
                  :key="item.id"
                  class="related-card"
                  @click="goToProductDetail(item.id)"
                >
                  <view class="related-card-top">
                    <q-image :src="item.images[0]" thumb :width="375" mode="aspectFill" class="related-img" />
                    <view class="related-info">
                      <text class="related-name">{{ item.name }}</text>
                      <text class="related-desc">{{ item.description || item.categoryName }}</text>
                    </view>
                  </view>
                  <view class="related-bottom">
                    <text class="related-price">¥{{ formatPrice(minPriceOf(item)) }}</text>
                    <text class="related-more">搭配加购 ›</text>
                  </view>
                </view>
              </view>
            </scroll-view>
          </view>

          <!-- 底部品质背书 -->
          <view class="result-footer">
            <view class="footer-line"></view>
            <text class="footer-text">核心产区直采 · 中检院全项质检 · 顺丰冷链直发</text>
            <view class="footer-line"></view>
          </view>
        </template>
      </view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue'
import { searchProducts, getHotProducts, addToCart } from '@/api'
import type { Product } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'
import { tagChipStyle } from '@/utils/tagColor'

const keyword = ref('')
const hasSearched = ref(false)
const loading = ref(false)
const results = ref<Product[]>([])
const relatedProducts = ref<Product[]>([])
const searchHistory = ref<string[]>([])

const sortBy = ref<'default' | 'sales' | 'price'>('default')
const priceAsc = ref(true)
const activeQuick = ref('all')
const filterOpen = ref(false)

const hotSearchTags = ['人参', '黄芪', '枸杞', '当归', '陈皮', '鹿茸', '金银花']

const HISTORY_STORAGE_KEY = 'qps_search_history'

onMounted(() => {
  loadSearchHistory()
})

const quickFilters = computed(() => {
  const map = new Map<string, string>()
  results.value.forEach(product => {
    displayTags(product).forEach(tag => {
      if (tag.code && !map.has(tag.code)) map.set(tag.code, tag.name)
    })
  })
  return [
    { key: 'all', name: '全部' },
    ...Array.from(map, ([key, name]) => ({ key, name }))
  ]
})

const filteredResults = computed(() => {
  const list = activeQuick.value === 'all'
    ? results.value
    : results.value.filter(product =>
        displayTags(product).some(tag => tag.code === activeQuick.value)
      )
  const copy = [...list]
  if (sortBy.value === 'sales') {
    copy.sort((a, b) => b.sales - a.sales)
  } else if (sortBy.value === 'price') {
    copy.sort((a, b) => priceAsc.value
      ? minPriceOf(a) - minPriceOf(b)
      : minPriceOf(b) - minPriceOf(a)
    )
  }
  return copy
})

async function handleSearch() {
  const kw = keyword.value.trim()
  if (!kw) {
    uni.showToast({ title: '请输入搜索关键词', icon: 'none' })
    return
  }
  if (loading.value) return

  loading.value = true
  hasSearched.value = true
  try {
    results.value = await searchProducts(kw)
    activeQuick.value = 'all'
    filterOpen.value = false
    saveToHistory(kw)
    loadRelated()
  } catch (error) {
    console.error('搜索商品失败', error)
    results.value = []
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '搜索失败，请稍后重试', icon: 'none' })
  } finally {
    loading.value = false
  }
}

async function loadRelated() {
  if (relatedProducts.value.length) return
  try {
    const hot = await getHotProducts()
    const searchedIds = new Set(results.value.map(product => product.id))
    relatedProducts.value = hot.filter(product => !searchedIds.has(product.id)).slice(0, 5)
  } catch (error) {
    console.error('加载关联推荐失败', error)
  }
}

function handleClear() {
  keyword.value = ''
  hasSearched.value = false
  results.value = []
  activeQuick.value = 'all'
  filterOpen.value = false
}

function searchByTag(tag: string) {
  keyword.value = tag
  handleSearch()
}

function setSort(sort: 'default' | 'sales') {
  if (sortBy.value === sort) return
  sortBy.value = sort
}

function togglePriceSort() {
  if (sortBy.value === 'price') {
    priceAsc.value = !priceAsc.value
    return
  }
  sortBy.value = 'price'
  priceAsc.value = true
}

function selectQuick(key: string) {
  activeQuick.value = key
}

function saveToHistory(value: string) {
  const history = searchHistory.value.filter(item => item !== value)
  history.unshift(value)
  searchHistory.value = history.slice(0, 10)
  uni.setStorageSync(HISTORY_STORAGE_KEY, JSON.stringify(searchHistory.value))
}

function loadSearchHistory() {
  try {
    const history = uni.getStorageSync(HISTORY_STORAGE_KEY)
    searchHistory.value = history ? JSON.parse(history) : []
  } catch {
    searchHistory.value = []
  }
}

function clearHistory() {
  uni.showModal({
    title: '提示',
    content: '确定要清空搜索历史吗？',
    success: (res) => {
      if (res.confirm) {
        searchHistory.value = []
        uni.removeStorageSync(HISTORY_STORAGE_KEY)
      }
    }
  })
}

function displayTags(product: Product) {
  return product.tags.filter(tag => tag.type !== 'Promotion')
}

function lowestSkuOf(product: Product) {
  if (!product.skus.length) return null
  return product.skus.reduce((min, item) =>
    (item.discountedPrice ?? item.price) < (min.discountedPrice ?? min.price) ? item : min)
}

function minPriceOf(product: Product): number {
  const sku = lowestSkuOf(product)
  return sku ? sku.discountedPrice ?? sku.price : 0
}

function originalPriceOf(product: Product): number {
  const sku = lowestSkuOf(product)
  return sku && sku.discountedPrice !== null && sku.discountedPrice < sku.price ? sku.price : 0
}

function formatPrice(price: number): string {
  return price % 1 === 0 ? String(price) : price.toFixed(2)
}

async function quickAdd(product: Product) {
  const sku = product.skus[0]
  if (!sku) return
  try {
    await addToCart({
      productId: product.id,
      productName: product.name,
      productImage: product.images[0] || '',
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

function goToProductDetail(productId: string) {
  uni.navigateTo({ url: `/pages/product-detail/index?id=${productId}` })
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>