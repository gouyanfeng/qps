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

<style scoped lang="scss">
@import "./q-page-header-back.scss";
</style>
