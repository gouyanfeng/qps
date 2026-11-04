<template>
  <view class="page">
    <q-page-header-back title="搜索" />

    <view class="search-bar-wrap">
      <view class="search-input-wrapper qps-search-bar">
        <q-icon name="search" size="20px" color="#999" />
        <input
          v-model="keyword"
          class="search-input"
          placeholder="搜索中药材名称或功效"
          :focus="true"
          @confirm="handleSearch"
          @clear="handleClear"
        />
        <view v-if="keyword" class="clear-btn" @click="handleClear">
          <q-icon name="close" size="16px" color="#999" />
        </view>
      </view>
    </view>

    <scroll-view scroll-y class="page-body" enhanced>
      <view v-if="!hasSearched" class="search-init">
        <view class="hot-searches">
          <view class="section-header">
              <q-icon name="star-filled" size="18px" color="#FF9800" />
              <text class="section-title qps-section-title">热门搜索</text>
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
            <q-icon name="time" size="18px" color="#757575" />
            <text class="section-title qps-section-title">搜索历史</text>
            <view class="clear-history" @click="clearHistory">
              <q-icon name="delete" size="16px" color="#999" />
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
      </view>

      <view v-else class="search-results">
        <view v-if="searchResults.length === 0" class="empty-results qps-empty-state">
          <q-icon name="search" size="80px" color="#ddd" />
          <text class="empty-text">未找到相关商品</text>
          <text class="empty-hint">换个关键词试试吧</text>
        </view>
        <view v-else class="results-list">
          <view class="results-header">
            <text class="results-count qps-section-more">找到 {{ searchResults.length }} 件相关商品</text>
          </view>
          <view
            v-for="product in searchResults"
            :key="product.id"
            class="result-item qps-card qps-shadow-sm qps-tap"
            @click="goToProductDetail(product.id)"
          >
            <q-cached-image :src="product.images[0]" mode="aspectFill" class="result-image" />
            <view class="result-info">
              <text class="result-name">{{ product.name }}</text>
              <text class="result-category">{{ product.categoryName }}</text>
              <view v-if="product.tags.length" class="result-tags">
                <text
                  v-for="tag in product.tags"
                  :key="tag.id"
                  class="result-tag"
                  :style="tagChipStyle(tag.color)"
                >{{ tag.name }}</text>
              </view>
              <text class="result-desc">{{ product.description }}</text>
              <view class="result-bottom">
                <view class="price-row">
                  <text class="price-symbol qps-price-accent qps-price-symbol">¥</text>
                  <text class="price-value qps-price-accent qps-price-value">{{ product.skus[0].price }}</text>
                </view>
                <text class="result-sales">已售 {{ product.sales }}</text>
              </view>
            </view>
          </view>
        </view>
      </view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { searchProducts } from '@/api'
import type { Product } from '@/api/types'
import { UnauthorizedError } from '@/utils/http'
import { tagChipStyle } from '@/utils/tagColor'

const keyword = ref('')
const hasSearched = ref(false)
const searchResults = ref<Product[]>([])
const searchHistory = ref<string[]>([])

const hotSearchTags = ['人参', '黄芪', '枸杞', '当归', '菊花', '鹿茸', '金银花']

const HISTORY_STORAGE_KEY = 'qps_search_history'

onMounted(() => {
  loadSearchHistory()
})

async function handleSearch() {
  if (!keyword.value.trim()) return

  try {
    searchResults.value = await searchProducts(keyword.value.trim())
    hasSearched.value = true
    saveToHistory(keyword.value.trim())
  } catch (error) {
    console.error('搜索商品失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '搜索失败，请稍后重试', icon: 'none' })
  }
}

function handleClear() {
  keyword.value = ''
  hasSearched.value = false
  searchResults.value = []
}

function searchByTag(tag: string) {
  keyword.value = tag
  handleSearch()
}

function saveToHistory(keyword: string) {
  const history = searchHistory.value.filter(h => h !== keyword)
  history.unshift(keyword)
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

function goToProductDetail(productId: string) {
  uni.navigateTo({ url: `/pages/product-detail/index?id=${productId}` })
}

function goBack() {
  uni.navigateBack()
}
</script>

<style lang="scss" scoped>
.page {
  display: flex;
  flex-direction: column;
  height: 100vh;
  padding-bottom: 100rpx;
}

.search-bar-wrap {
  padding: 0 24rpx 20rpx;
}

.search-input-wrapper {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 16rpx;
}

.search-input {
  flex: 1;
  font-size: 28rpx;
  color: $theme-text-primary;
}

.clear-btn {
  width: 40rpx;
  height: 40rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #E0E0E0;
  border-radius: 50%;
  font-size: 20rpx;
  color: #fff;
}

.search-cancel {
  color: $theme-text-primary;
  font-size: 28rpx;
  font-weight: 500;
}

.page-body {
  flex: 1;
  overflow-y: auto;
}

.search-init {
  padding: 30rpx;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24rpx;
}

.hot-searches {
  margin-bottom: 50rpx;
}

.hot-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 20rpx;
}

.hot-tag {
  display: flex;
  align-items: center;
  gap: 12rpx;
  padding: 16rpx 28rpx;
  background: #fff;
  border-radius: 40rpx;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.06);

  &.hot-tag-top {
    background: linear-gradient(135deg, lighten($theme-accent, 35%) 0%, lighten($theme-accent, 15%) 100%);
    box-shadow: 0 2rpx 12rpx rgba(255, 152, 0, 0.15);
  }
}

.hot-badge {
  width: 32rpx;
  height: 32rpx;
  display: flex;
  align-items: center;
  justify-content: center;
  background: linear-gradient(135deg, #FF6B6B 0%, #FF4757 100%);
  color: #fff;
  font-size: 20rpx;
  font-weight: bold;
  border-radius: 50%;
}

.hot-tag-text {
  font-size: 26rpx;
  color: $theme-text-primary;
  font-weight: 500;
}

.search-history {
  margin-top: 50rpx;
}

.clear-history {
  display: flex;
  align-items: center;
  gap: 8rpx;
}

.clear-text {
  font-size: 24rpx;
  color: $theme-text-hint;
}

.history-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 16rpx;
}

.history-tag {
  padding: 12rpx 24rpx;
  background: #fff;
  border-radius: 30rpx;
  font-size: 24rpx;
  color: $theme-text-secondary;
  box-shadow: 0 2rpx 8rpx rgba(0, 0, 0, 0.04);
}

.search-results {
  padding: 30rpx;
}

.empty-results {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 200rpx 0;
}

.empty-text {
  font-size: 32rpx;
  color: $theme-text-primary;
  font-weight: bold;
  margin: 16rpx 0;
}

.empty-hint {
  font-size: 26rpx;
  color: $theme-text-hint;
}

.results-list {
  display: flex;
  flex-direction: column;
  gap: 24rpx;
}

.results-header {
  padding: 20rpx 0;
  border-bottom: 2rpx solid lighten($theme-primary-light, 25%);
  margin-bottom: 10rpx;
}

.result-item {
  display: flex;
  gap: 24rpx;
  padding: 24rpx;
  border: 2rpx solid $theme-bg;
  overflow: hidden;
}

.result-image {
  width: 180rpx;
  height: 180rpx;
  border-radius: 16rpx;
  flex-shrink: 0;
}

.result-info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 10rpx;
}

.result-name {
  font-size: 30rpx;
  color: $theme-text-primary;
  font-weight: 600;
}

.result-category {
  font-size: 22rpx;
  color: $theme-primary-light;
  padding: 4rpx 12rpx;
  background: lighten($theme-primary-light, 25%);
  border-radius: 12rpx;
  display: inline-block;
  width: fit-content;
}

.result-tags {
  display: flex;
  flex-wrap: wrap;
  gap: 8rpx;
}

.result-tag {
  padding: 4rpx 12rpx;
  border: 1rpx solid #d5e3d1;
  border-radius: 20rpx;
  color: #52714f;
  font-size: 19rpx;
}

.result-desc {
  font-size: 24rpx;
  color: $theme-text-secondary;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.5;
}

.result-bottom {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  margin-top: 10rpx;
}

.price-row {
  display: flex;
  align-items: baseline;
}

.result-sales {
  font-size: 22rpx;
  color: $theme-text-hint;
}
</style>