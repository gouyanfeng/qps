<template>
  <view class="page">
    <q-page-header-back :title="activity?.name || '活动详情'" />
    <scroll-view scroll-y class="page-body" enhanced>
      <view v-if="activity" class="content">
        <image v-if="activity.coverImage" :src="resolveFileUrl(activity.coverImage)" mode="widthFix" class="cover" />
        <view class="intro">
          <text class="title">{{ activity.name }}</text>
          <text v-if="activity.subtitle" class="subtitle">{{ activity.subtitle }}</text>
          <text v-if="activity.description" class="description">{{ activity.description }}</text>
        </view>
        <view v-if="activity.promotions.length" class="promotion-section">
          <text class="section-title">活动优惠</text>
          <view v-for="promotion in activity.promotions" :key="promotion.id" class="promotion">
            <text class="promotion-name">{{ promotion.name }}</text>
            <text v-if="promotion.description" class="promotion-description">{{ promotion.description }}</text>
          </view>
        </view>
        <view v-if="activity.detailHtml" class="detail">
          <rich-text :nodes="activity.detailHtml" />
        </view>
      </view>
      <view v-else-if="!loading" class="empty">活动暂不可用</view>
    </scroll-view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onLoad } from '@dcloudio/uni-app'
import { getMallActivity } from '@/api'
import type { MallActivity } from '@/api/types'
import { resolveFileUrl, UnauthorizedError } from '@/utils/http'

const activity = ref<MallActivity | null>(null)
const loading = ref(true)

onLoad(async (options) => {
  if (!options?.id) {
    loading.value = false
    uni.showToast({ title: '活动链接无效', icon: 'none' })
    return
  }

  try {
    activity.value = await getMallActivity(options.id)
  } catch (error) {
    console.error('加载活动详情失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '活动暂不可用', icon: 'none' })
  } finally {
    loading.value = false
  }
})
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background: var(--page-bg);
}

.page-body {
  height: calc(100vh - 100rpx);
}

.content {
  padding-bottom: 48rpx;
}

.cover {
  display: block;
  width: 100%;
}

.intro,
.promotion-section,
.detail {
  margin: 24rpx;
  padding: 28rpx;
  border-radius: 20rpx;
  background: #fff;
}

.intro {
  display: flex;
  flex-direction: column;
  gap: 16rpx;
}

.title {
  color: #1b1c1c;
  font-size: 40rpx;
  font-weight: 700;
}

.subtitle {
  color: #2e7d32;
  font-size: 28rpx;
}

.description,
.promotion-description {
  color: #60665f;
  font-size: 28rpx;
  line-height: 1.7;
}

.section-title {
  display: block;
  margin-bottom: 20rpx;
  color: #1b1c1c;
  font-size: 32rpx;
  font-weight: 600;
}

.promotion {
  display: flex;
  flex-direction: column;
  gap: 8rpx;
  padding: 20rpx 0;
  border-top: 1rpx solid #eef0ed;
}

.promotion-name {
  color: #8b5000;
  font-size: 30rpx;
  font-weight: 600;
}

.empty {
  padding: 160rpx 24rpx;
  color: #777;
  text-align: center;
}
</style>
