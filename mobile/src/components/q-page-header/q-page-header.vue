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

<style scoped lang="scss">
@import "./q-page-header.scss";
</style>
