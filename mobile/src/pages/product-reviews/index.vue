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
          <q-image
            v-for="(img, imgIdx) in review.images"
            :key="imgIdx"
            class="review-image"
            :src="img"
            thumb
            :width="120"
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
@import "./index.scss";
</style>
