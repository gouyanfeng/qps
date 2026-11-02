<template>
  <view class="q-page-header" :class="{ 'has-border': border }" :style="{ background, gap }">
    <slot />
  </view>
</template>

<script setup lang="ts">
withDefaults(defineProps<{
  background?: string
  gap?: string
  border?: boolean
}>(), {
  background: 'var(--page-bg)',
  gap: '0',
  border: false
})
</script>

<style scoped>
.q-page-header {
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: space-between;
  flex-shrink: 0;
  min-height: var(--page-top-offset);
  padding: var(--safe-top) var(--topbar-padding-x) var(--topbar-padding-bottom);
  position: relative;
  z-index: 40;
}

.q-page-header.has-border::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 1rpx;
  background: rgba(27, 28, 28, 0.08);
}
</style>
