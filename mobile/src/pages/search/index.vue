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
                <q-cached-image :src="product.images[0]" mode="aspectFill" class="thumb-img" />
                <view v-if="promoTagOf(product)" class="thumb-badge" :style="promoBadgeStyle(product)">
                  <text class="thumb-badge-text">{{ promoTagOf(product)?.name }}</text>
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
                    <text v-if="marketPriceOf(product) > minPriceOf(product)" class="price-origin">¥{{ formatPrice(marketPriceOf(product)) }}</text>
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
                    <q-cached-image :src="item.images[0]" mode="aspectFill" class="related-img" />
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
import { tagChipStyle, resolveTagColor } from '@/utils/tagColor'

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

function promoTagOf(product: Product) {
  return product.tags.find(tag => tag.type === 'Promotion')
}

function promoBadgeStyle(product: Product) {
  const tag = promoTagOf(product)
  return { backgroundColor: resolveTagColor(tag ? tag.color : undefined) }
}

function displayTags(product: Product) {
  return product.tags.filter(tag => tag.type !== 'Promotion')
}

function minPriceOf(product: Product): number {
  const prices = product.skus.map(sku => sku.price)
  return prices.length ? Math.min(...prices) : 0
}

function marketPriceOf(product: Product): number {
  const sku = product.skus.find(item => item.marketPrice > 0)
  return sku ? sku.marketPrice : 0
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
$on-surface: #1B1C1C;
$on-surface-variant: #6B6B6B;
$outline: #8A8578;
$price: #8B5000;

.page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  background: var(--page-bg);
}

/* ============ 顶栏搜索框 ============ */
.search-box {
  flex: 1;
  min-width: 0;
  display: flex;
  align-items: center;
  gap: 10rpx;
  height: 62rpx;
  padding: 0 18rpx;
  background: #F5F3ED;
  border-radius: 20rpx;
}

.search-input {
  flex: 1;
  min-width: 0;
  font-size: 24rpx;
  color: $on-surface;
}

.search-placeholder {
  color: $outline;
  font-size: 24rpx;
}

.clear-btn {
  width: 30rpx;
  height: 30rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(112, 122, 108, 0.35);
  border-radius: 50%;
  flex-shrink: 0;
}

.search-btn {
  height: 62rpx;
  padding: 0 24rpx;
  margin-left: 12rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  background: $theme-primary;
  border-radius: 20rpx;
  box-shadow: 0 4rpx 12rpx rgba(13, 99, 27, 0.15);
  flex-shrink: 0;

  &:active {
    opacity: 0.9;
  }
}

.search-btn-text {
  font-size: 24rpx;
  font-weight: 600;
  color: #ffffff;
}

.page-body {
  flex: 1;
  min-height: 0;
  overflow-y: auto;
}

/* ============ 初始态 ============ */
.search-init {
  padding: 30rpx 24rpx 60rpx;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24rpx;
}

.section-head-left {
  display: flex;
  align-items: center;
  gap: 10rpx;
}

.section-dot {
  width: 8rpx;
  height: 8rpx;
  border-radius: 50%;
  background: $theme-primary;
}

.section-title {
  font-size: 28rpx;
  font-weight: 700;
  color: $theme-primary-dark;
  letter-spacing: 1rpx;
}

.section-hint {
  font-size: 20rpx;
  color: $outline;
}

.hot-searches {
  margin-bottom: 50rpx;
}

.hot-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 18rpx;
}

.hot-tag {
  display: flex;
  align-items: center;
  gap: 10rpx;
  padding: 14rpx 26rpx;
  background: #ffffff;
  border-radius: 100rpx;
  box-shadow: 0 4rpx 12rpx rgba(46, 125, 50, 0.06);

  &:active {
    transform: scale(0.97);
  }

  &.hot-tag-top {
    background: #FFF6EC;
    border: 1rpx solid rgba(255, 152, 0, 0.25);
  }
}

.hot-badge {
  width: 30rpx;
  height: 30rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  background: $theme-accent;
  color: #ffffff;
  font-size: 19rpx;
  font-weight: 700;
  border-radius: 50%;
}

.hot-tag-text {
  font-size: 24rpx;
  color: $on-surface;
  font-weight: 500;
}

.search-history {
  margin-top: 10rpx;
}

.clear-history {
  display: flex;
  align-items: center;
  gap: 6rpx;
}

.clear-text {
  font-size: 22rpx;
  color: #9E9E9E;
}

.history-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 16rpx;
}

.history-tag {
  padding: 12rpx 24rpx;
  background: #F5F3ED;
  border-radius: 100rpx;
  font-size: 22rpx;
  color: $on-surface-variant;

  &:active {
    transform: scale(0.97);
  }
}

/* ============ 结果态 ============ */
.search-results {
  padding-bottom: 40rpx;
}

.loading-state,
.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 200rpx 0;
  gap: 16rpx;
}

.loading-text,
.empty-hint {
  font-size: 24rpx;
  color: #BDBDBD;
}

.empty-text {
  font-size: 30rpx;
  font-weight: 700;
  color: $on-surface;
}

/* 结果数量与溯源提示条 */
.result-meta {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 22rpx 24rpx 10rpx;
}

.meta-left {
  display: flex;
  align-items: center;
  gap: 10rpx;
}

.meta-dot {
  width: 10rpx;
  height: 10rpx;
  border-radius: 50%;
  background: $theme-primary;
}

.meta-text {
  font-size: 21rpx;
  color: $on-surface-variant;
}

.meta-count {
  font-size: 22rpx;
  font-weight: 700;
  color: $theme-primary;
}

.meta-right {
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 6rpx 14rpx;
  border-radius: 100rpx;
  background: #F1F5EF;
}

.meta-right-text {
  font-size: 19rpx;
  color: $theme-primary;
}

/* 快速筛选胶囊 */
.quick-scroll {
  width: 100%;
  white-space: nowrap;
}

.quick-row {
  display: flex;
  align-items: center;
  gap: 14rpx;
  padding: 10rpx 24rpx 14rpx;
}

.quick-pill {
  flex-shrink: 0;
  height: 54rpx;
  padding: 0 22rpx;
  display: flex;
  align-items: center;
  border-radius: 100rpx;
  background: #F1EFEA;
  font-size: 21rpx;
  color: $on-surface-variant;

  &.active {
    background: $theme-primary;
    color: #ffffff;
    font-weight: 600;
  }
}

/* 排序工具栏 */
.sort-bar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 18rpx 24rpx;
  background: rgba(252, 249, 248, 0.92);
}

.sort-left {
  display: flex;
  align-items: center;
  gap: 34rpx;
}

.sort-item {
  display: flex;
  align-items: center;
  gap: 6rpx;
  font-size: 24rpx;
  color: $on-surface-variant;

  &.active {
    color: $theme-primary;
    font-weight: 700;
  }
}

.sort-arrow {
  font-size: 20rpx;
  color: #9E9E9E;
}

.sort-dot {
  width: 6rpx;
  height: 6rpx;
  border-radius: 50%;
  background: $theme-primary;
}

.filter-btn {
  display: flex;
  align-items: center;
  gap: 6rpx;
  padding: 8rpx 18rpx;
  border-radius: 12rpx;
  background: #F5F3ED;
  font-size: 20rpx;
  color: $on-surface-variant;

  &.active {
    background: #EDF3EB;
    color: $theme-primary;
  }
}

/* 筛选面板 */
.filter-panel {
  margin: 0 24rpx 8rpx;
  padding: 20rpx 22rpx 24rpx;
  border-radius: 16rpx;
  background: #F7F5F0;
}

.filter-panel-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 16rpx;
}

.filter-panel-title {
  font-size: 21rpx;
  font-weight: 600;
  color: $on-surface;
}

.filter-reset {
  font-size: 21rpx;
  color: $theme-primary;
}

.filter-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 14rpx;
}

.filter-tag {
  padding: 10rpx 22rpx;
  border-radius: 100rpx;
  background: #ffffff;
  font-size: 21rpx;
  color: $on-surface-variant;

  &.active {
    background: $theme-primary;
    color: #ffffff;
    font-weight: 600;
  }
}

/* 商品双列卡片 */
.result-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 20rpx;
  padding: 8rpx 24rpx 24rpx;
}

.result-card {
  background: #ffffff;
  border-radius: 20rpx;
  overflow: hidden;
  box-shadow: 0 6rpx 20rpx -6rpx rgba(46, 125, 50, 0.12);
}

.card-thumb {
  position: relative;
  margin: 14rpx 14rpx 0;
  border-radius: 14rpx;
  overflow: hidden;
  background: #F5F3ED;
}

.thumb-img {
  width: 100%;
  height: 300rpx;
  display: block;
}

.thumb-badge {
  position: absolute;
  top: 0;
  left: 0;
  z-index: 2;
  padding: 4rpx 14rpx;
  border-radius: 14rpx 0 14rpx 0;
  background: $theme-primary;
}

.thumb-badge-plain {
  background: rgba(46, 125, 50, 0.85);
}

.thumb-badge-text {
  font-size: 19rpx;
  color: #ffffff;
}

.card-body {
  padding: 12rpx 16rpx 16rpx;
  display: flex;
  flex-direction: column;
  gap: 6rpx;
}

.card-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8rpx;
}

.card-tag {
  padding: 4rpx 12rpx;
  border: 1rpx solid #d5e3d1;
  border-radius: 100rpx;
  font-size: 18rpx;
}

.card-name {
  font-size: 24rpx;
  font-weight: 700;
  line-height: 1.35;
  color: $on-surface;
  overflow: hidden;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  height: 66rpx;
}

.card-desc {
  font-size: 19rpx;
  color: $outline;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.card-bottom {
  display: flex;
  align-items: flex-end;
  justify-content: space-between;
  margin-top: 6rpx;
}

.price-col {
  display: flex;
  flex-direction: column;
}

.price-origin {
  font-size: 18rpx;
  color: #B0ACA2;
  text-decoration: line-through;
}

.price-row {
  display: flex;
  align-items: baseline;
}

.price-symbol {
  font-size: 20rpx;
  font-weight: 700;
  color: $price;
  margin-right: 2rpx;
}

.price-value {
  font-size: 32rpx;
  font-weight: 700;
  color: $price;
  letter-spacing: -1rpx;
}

.add-btn {
  width: 52rpx;
  height: 52rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 50%;
  background: $theme-primary;
  box-shadow: 0 4rpx 12rpx rgba(46, 125, 50, 0.25);

  &:active {
    transform: scale(0.92);
  }
}

/* 关联推荐 */
.related {
  margin: 8rpx 24rpx 30rpx;
  padding: 20rpx 22rpx;
  border-radius: 18rpx;
  background: #F7F5F0;
}

.related-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  margin-bottom: 18rpx;
}

.related-head-left {
  display: flex;
  align-items: center;
  gap: 8rpx;
}

.related-title {
  font-size: 24rpx;
  font-weight: 700;
  color: $on-surface;
}

.related-hint {
  font-size: 19rpx;
  color: $outline;
}

.related-row {
  display: flex;
  gap: 16rpx;
  padding-bottom: 4rpx;
}

.related-card {
  flex-shrink: 0;
  width: 300rpx;
  padding: 16rpx;
  border-radius: 14rpx;
  background: #ffffff;
  box-shadow: 0 4rpx 12rpx rgba(46, 125, 50, 0.05);
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.related-card-top {
  display: flex;
  align-items: center;
  gap: 12rpx;
  margin-bottom: 14rpx;
}

.related-img {
  width: 72rpx;
  height: 72rpx;
  border-radius: 12rpx;
  flex-shrink: 0;
  background: #F5F3ED;
}

.related-info {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
  gap: 4rpx;
}

.related-name {
  font-size: 22rpx;
  font-weight: 600;
  color: $on-surface;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.related-desc {
  font-size: 18rpx;
  color: $outline;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.related-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.related-price {
  font-size: 21rpx;
  font-weight: 600;
  color: $theme-primary;
}

.related-more {
  font-size: 19rpx;
  color: $on-surface-variant;
}

/* 底部背书 */
.result-footer,
.init-footer {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 14rpx;
  padding: 24rpx 24rpx 40rpx;
}

.init-footer {
  margin-top: 60rpx;
}

.footer-line {
  width: 40rpx;
  height: 1rpx;
  background: #DCD8CE;
}

.footer-text {
  font-size: 19rpx;
  color: $outline;
}
</style>