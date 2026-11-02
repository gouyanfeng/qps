<template>
  <view class="q-page-header" :style="{ background }">
    <view v-if="showBack" class="header-side">
      <view class="header-back" @tap="goBack">
        <q-icon name="arrow-left" size="30" :color="backColor" />
      </view>
    </view>
    <view v-else class="header-side"></view>

    <view class="header-title">
      <slot name="title">
        <view class="header-title-main" :style="{ color: titleColor }">{{ title }}</view>
        <view v-if="subtitle" class="header-title-sub">{{ subtitle }}</view>
      </slot>
    </view>

    <view class="header-side header-right">
      <slot name="right" />
    </view>
  </view>
</template>

<script setup lang="ts">
const props = withDefaults(defineProps<{
  title?: string
  subtitle?: string
  fallback?: string
  backColor?: string
  titleColor?: string
  background?: string
  showBack?: boolean
}>(), {
  title: '',
  subtitle: '',
  fallback: '/pages/index/index',
  backColor: '#1B5E20',
  titleColor: '#1B5E20',
  background: 'var(--page-bg)',
  showBack: true
})

function goBack() {
  uni.navigateBack({
    fail: () => {
      uni.switchTab({ url: props.fallback })
    }
  })
}
</script>

<style scoped>
.q-page-header {
  box-sizing: border-box;
  display: flex;
  align-items: center;
  flex-shrink: 0;
  min-height: var(--page-top-offset);
  padding: var(--safe-top) var(--topbar-padding-x) var(--topbar-padding-bottom);
  position: relative;
  z-index: 40;
}

.q-page-header::after {
  content: '';
  position: absolute;
  left: 0;
  right: 0;
  bottom: 0;
  height: 1rpx;
  background: rgba(27, 28, 28, 0.08);
}

.header-side {
  width: 64rpx;
  flex-shrink: 0;
  display: flex;
  align-items: center;
}

.header-right {
  justify-content: flex-end;
  width: auto;
  min-width: 64rpx;
}

.header-back {
  width: 64rpx;
  height: 64rpx;
  display: flex;
  align-items: center;
  justify-content: center;
}

.header-title {
  flex: 1;
  min-width: 0;
  text-align: center;
}

.header-title-main {
  font-size: 34rpx;
  font-weight: 700;
  letter-spacing: 2rpx;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.header-title-sub {
  margin-top: 4rpx;
  font-size: 18rpx;
  font-weight: 600;
  letter-spacing: 4rpx;
  color: #8A8578;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}
</style>
