<template>
  <scroll-view
    scroll-y
    class="q-scroll-list"
    :enhanced="enhanced"
    :show-scrollbar="showScrollbar"
    :lower-threshold="threshold"
    @scrolltolower="onScrollToLower"
  >
    <slot />
    <view v-if="loading || !hasMore || moreText" class="q-scroll-list-status">
      <text class="q-scroll-list-status-text">{{ loading ? loadingText : (!hasMore ? finishedText : moreText) }}</text>
    </view>
  </scroll-view>
</template>

<script setup lang="ts">
const props = withDefaults(defineProps<{
  loading?: boolean
  hasMore?: boolean
  threshold?: number
  loadingText?: string
  finishedText?: string
  moreText?: string
  enhanced?: boolean
  showScrollbar?: boolean
}>(), {
  loading: false,
  hasMore: true,
  threshold: 100,
  loadingText: '加载中…',
  finishedText: '—— 没有更多了 ——',
  moreText: '',
  enhanced: true,
  showScrollbar: true
})

const emit = defineEmits<{ loadmore: [] }>()

function onScrollToLower() {
  if (props.loading || !props.hasMore) return
  emit('loadmore')
}
</script>

<style scoped>
.q-scroll-list {
  flex: 1;
  min-height: 0;
}

.q-scroll-list-status {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 28rpx 0 40rpx;
}

.q-scroll-list-status-text {
  font-size: 24rpx;
  color: #8A8578;
  letter-spacing: 2rpx;
}
</style>
