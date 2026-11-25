<template>
  <view class="q-page" :style="{ background }">
    <scroll-view
      scroll-y
      class="q-page-body"
      :enhanced="true"
      :refresher-enabled="refreshEnabled"
      :refresher-triggered="refresherTriggered"
      refresher-default-style="black"
      :refresher-threshold="refresherThreshold"
      :lower-threshold="lowerThreshold"
      @refresherrefresh="onRefresh"
      @scrolltolower="onLoadmore"
    >
      <slot name="header" />
      <slot />
      <view v-if="showLoadmore" class="q-page-status">
        <text class="q-page-status-text">
          {{ loading ? loadingText : (!hasMore ? finishedText : moreText) }}
        </text>
      </view>
    </scroll-view>
    <slot name="footer" />
  </view>
</template>

<script setup lang="ts">
const props = withDefaults(defineProps<{
  background?: string
  refreshEnabled?: boolean
  refresherTriggered?: boolean
  refresherThreshold?: number
  showLoadmore?: boolean
  hasMore?: boolean
  loading?: boolean
  loadingText?: string
  finishedText?: string
  moreText?: string
  lowerThreshold?: number
}>(), {
  background: 'var(--page-bg)',
  refreshEnabled: false,
  refresherTriggered: false,
  refresherThreshold: 60,
  showLoadmore: false,
  hasMore: true,
  loading: false,
  loadingText: '加载中…',
  finishedText: '—— 没有更多了 ——',
  moreText: '',
  lowerThreshold: 100
})

const emit = defineEmits<{
  (e: 'refresh'): void
  (e: 'loadmore'): void
}>()

function onRefresh() {
  emit('refresh')
}

function onLoadmore() {
  if (!props.showLoadmore || props.loading || !props.hasMore) return
  emit('loadmore')
}
</script>

<style scoped>
.q-page {
  display: flex;
  flex-direction: column;
  height: calc(100vh - var(--window-bottom, 0px));
  min-height: 0;
  overflow: hidden;
}

.q-page-body {
  flex: 1;
  min-height: 0;
  background: var(--page-bg);
}

.q-page-status {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 28rpx 0 40rpx;
}

.q-page-status-text {
  font-size: 24rpx;
  color: #8A8578;
  letter-spacing: 2rpx;
}
</style>
