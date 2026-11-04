<template>
  <view class="page">
    <q-page-header-back title="商品评价" />

    <view class="summary-card">
      <view class="summary-item">
        <text class="summary-value rate">{{ positiveRateText }}</text>
        <text class="summary-label">好评率</text>
      </view>
      <view class="summary-divider"></view>
      <view class="summary-item">
        <text class="summary-value">{{ totalCount }}</text>
        <text class="summary-label">条评价</text>
      </view>
    </view>

    <scroll-view scroll-x class="tags-scroll" :show-scrollbar="false">
      <view class="tags">
        <view class="tag-pill" :class="{ active: !activeTag }" @click="switchTag('')">
          <text>全部 ({{ totalCount }})</text>
        </view>
        <view
          v-for="stat in tagStats"
          :key="stat.tag"
          class="tag-pill"
          :class="{ active: activeTag === stat.tag }"
          @click="switchTag(stat.tag)"
        >
          <text>{{ stat.tag }} ({{ stat.count }})</text>
        </view>
      </view>
    </scroll-view>

    <view v-if="reviews.length" class="review-list">
      <view v-for="review in reviews" :key="review.id" class="review-item">
        <view class="review-item-head">
          <view class="review-user">
            <view class="review-avatar">
              <text>{{ avatarChar(review) }}</text>
            </view>
            <view class="review-user-meta">
              <view class="review-user-name-row">
                <text class="review-nickname">{{ review.memberNickname }}</text>
                <text v-if="review.memberLevelName" class="review-level">{{ review.memberLevelName }}</text>
              </view>
            </view>
          </view>
          <view class="review-stars">
            <q-icon
              v-for="n in 5"
              :key="n"
              :name="n <= review.rating ? 'star-filled' : 'star'"
              size="24"
              :color="n <= review.rating ? '#FF9800' : '#D8D3CB'"
            />
          </view>
        </view>
        <text class="review-content">{{ review.content }}</text>
        <view v-if="review.images.length" class="review-images">
          <q-cached-image
            v-for="(img, imgIdx) in review.images"
            :key="imgIdx"
            class="review-image"
            :src="img"
            mode="aspectFill"
            @click="previewReviewImages(review.images, imgIdx)"
          />
        </view>
        <view v-if="review.replyContent" class="review-reply">
          <text class="review-reply-label">商家回复</text>
          <text class="review-reply-text">{{ review.replyContent }}</text>
        </view>
        <view class="review-foot">
          <text class="review-spec">规格：{{ review.specText }}</text>
          <text class="review-date">{{ formatReviewDate(review.reviewTime) }}</text>
        </view>
      </view>
    </view>

    <view v-if="reviews.length" class="list-status">
      <text>{{ loading ? '加载中…' : (hasMore ? '' : '没有更多了') }}</text>
    </view>

    <view v-if="loaded && !loading && !reviews.length" class="empty-state">
      <q-icon name="chat" size="48" color="#BDBDBD" />
      <text class="empty-text">暂无评价</text>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed } from 'vue'
import { onLoad, onReachBottom } from '@dcloudio/uni-app'
import { getProductReviews } from '@/api'
import type { ProductReview, ReviewTagStat } from '@/api/types'
import { resolveFileUrl } from '@/utils/http'

const PAGE_SIZE = 10

const productId = ref('')
const activeTag = ref('')
const reviews = ref<ProductReview[]>([])
const tagStats = ref<ReviewTagStat[]>([])
const totalCount = ref(0)
const positiveRate = ref(0)
const page = ref(1)
const hasMore = ref(false)
const loading = ref(false)
const loaded = ref(false)

const positiveRateText = computed(() => {
  const rate = Number(positiveRate.value) || 0
  return `${rate % 1 === 0 ? rate : rate.toFixed(1)}%`
})

onLoad((options) => {
  productId.value = (options?.productId as string) || ''
  activeTag.value = (options?.tag as string) || ''
  if (!productId.value) {
    uni.showToast({ title: '缺少商品信息', icon: 'none' })
    return
  }
  loadReviews(true)
})

onReachBottom(() => {
  if (!hasMore.value || loading.value) return
  page.value++
  loadReviews()
})

let requestSeq = 0

async function loadReviews(reset: boolean = false) {
  if (!productId.value) return
  // 上拉加载时避免重复请求；切换标签（reset）允许打断当前请求
  if (!reset && loading.value) return
  const seq = ++requestSeq
  if (reset) {
    page.value = 1
    reviews.value = []
    hasMore.value = false
    loaded.value = false
  }
  loading.value = true
  try {
    const result = await getProductReviews(
      productId.value,
      page.value,
      PAGE_SIZE,
      activeTag.value || undefined
    )
    if (seq !== requestSeq) return
    reviews.value = reset ? result.items : [...reviews.value, ...result.items]
    totalCount.value = result.totalCount
    positiveRate.value = result.positiveRate
    tagStats.value = result.tagStats
    hasMore.value = reviews.value.length < result.totalCount
    loaded.value = true
  } catch (error) {
    if (seq !== requestSeq) return
    console.error('加载商品评价失败', error)
    uni.showToast({ title: '评价加载失败', icon: 'none' })
  } finally {
    if (seq === requestSeq) loading.value = false
  }
}

function switchTag(tag: string) {
  if (activeTag.value === tag) return
  activeTag.value = tag
  loadReviews(true)
}

function avatarChar(review: ProductReview) {
  const nickname = (review.memberNickname || '').trim()
  return nickname ? nickname.charAt(0) : '匿'
}

function formatReviewDate(value: string) {
  if (!value) return ''
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return value.slice(0, 10).replace(/-/g, '.')
  const pad = (num: number) => String(num).padStart(2, '0')
  return `${date.getFullYear()}.${pad(date.getMonth() + 1)}.${pad(date.getDate())}`
}

function previewReviewImages(images: string[], index: number) {
  if (!images.length) return
  uni.previewImage({
    urls: images.map(resolveFileUrl),
    current: resolveFileUrl(images[index])
  })
}
</script>

<style lang="scss" scoped>
$primary: #2E7D32;
$price: #8B5000;
$on-surface: #1B1C1C;
$muted: #8A8578;
$line: #EFEEEB;
$surface: #ffffff;
$shadow-card: 0 4rpx 20rpx -4rpx rgba(46, 125, 50, 0.08);

.page {
  min-height: 100vh;
  background: var(--page-bg);
  padding-bottom: calc(40rpx + env(safe-area-inset-bottom, 0));
}

/* 顶部汇总 */
.summary-card {
  display: flex;
  align-items: center;
  margin: 20rpx 24rpx 0;
  padding: 32rpx 0;
  background: $surface;
  border-radius: 24rpx;
  box-shadow: $shadow-card;
}
.summary-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 6rpx;
}
.summary-value {
  font-size: 52rpx;
  font-weight: 800;
  color: $on-surface;
  line-height: 1.1;
  letter-spacing: -1rpx;

  &.rate { color: $price; }
}
.summary-label {
  font-size: 22rpx;
  color: $muted;
}
.summary-divider {
  width: 1rpx;
  height: 72rpx;
  background: $line;
}

/* 标签胶囊 */
.tags-scroll {
  white-space: nowrap;
  margin-top: 20rpx;
}
.tags {
  display: inline-flex;
  align-items: center;
  gap: 12rpx;
  padding: 0 24rpx;
}
.tag-pill {
  flex-shrink: 0;
  padding: 10rpx 22rpx;
  background: #F3F0ED;
  border-radius: 999rpx;

  text {
    font-size: 22rpx;
    color: #6F6B62;
    line-height: 1.4;
  }

  &.active {
    background: $primary;

    text { color: #ffffff; font-weight: 600; }
  }
}

/* 评价列表 */
.review-list {
  margin: 20rpx 24rpx 0;
  padding: 0 28rpx;
  background: $surface;
  border-radius: 24rpx;
  box-shadow: $shadow-card;
}
.review-item {
  padding: 28rpx 0;
  border-bottom: 1rpx solid $line;

  &:last-child { border-bottom: 0; }
}
.review-item-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 16rpx;
}
.review-user { display: flex; align-items: center; gap: 14rpx; flex: 1; min-width: 0; }
.review-avatar {
  width: 52rpx; height: 52rpx; flex-shrink: 0;
  display: flex; align-items: center; justify-content: center;
  border-radius: 50%;
  background: rgba(46, 125, 50, 0.1);
}
.review-avatar text { font-size: 22rpx; font-weight: 700; color: $primary; }
.review-user-meta { flex: 1; min-width: 0; }
.review-user-name-row { display: flex; align-items: center; gap: 10rpx; min-width: 0; }
.review-nickname {
  min-width: 0;
  font-size: 24rpx; font-weight: 700; color: $on-surface;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.review-level {
  flex-shrink: 0;
  padding: 2rpx 8rpx;
  background: rgba(46, 125, 50, 0.1);
  border-radius: 6rpx;
  font-size: 17rpx; font-weight: 600; color: $primary;
}
.review-stars { display: flex; align-items: center; gap: 2rpx; flex-shrink: 0; }

.review-content {
  display: block;
  margin-top: 12rpx;
  font-size: 24rpx; line-height: 1.65; color: #30342E;
  word-break: break-all;
}
.review-images {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 10rpx;
  margin-top: 14rpx;
}
.review-image {
  width: 100%;
  height: 196rpx;
  border-radius: 10rpx;
  background: #F3F3F0;
}
.review-reply {
  display: flex;
  flex-direction: column;
  gap: 6rpx;
  margin-top: 14rpx;
  padding: 14rpx 18rpx;
  background: #F7F5F1;
  border-radius: 10rpx;
}
.review-reply-label { font-size: 19rpx; font-weight: 700; color: $primary; }
.review-reply-text {
  font-size: 22rpx; line-height: 1.6; color: #5F5B53;
  word-break: break-all;
}
.review-foot {
  display: flex; align-items: center; justify-content: space-between;
  gap: 16rpx;
  margin-top: 12rpx;
}
.review-spec {
  flex: 1; min-width: 0;
  font-size: 19rpx; color: #A09A90;
  overflow: hidden; text-overflow: ellipsis; white-space: nowrap;
}
.review-date { flex-shrink: 0; font-size: 19rpx; color: #A09A90; }

/* 加载状态与空态 */
.list-status {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 28rpx 0 12rpx;
}
.list-status text { font-size: 22rpx; color: $muted; letter-spacing: 2rpx; }

.empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 16rpx;
  padding: 140rpx 0;
}
.empty-text { font-size: 26rpx; color: $muted; }
</style>
