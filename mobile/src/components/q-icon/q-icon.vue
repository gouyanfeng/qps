<template>
  <text class="q-icon q-icon-font" :style="iconStyle">{{ glyph }}</text>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { iconChars } from '@/icons/iconfont'

const props = withDefaults(defineProps<{ name: string; size?: string | number; color?: string }>(), {
  size: 24,
  color: 'currentColor'
})

// size 兼容 px / rpx / 纯数字（按 px 处理）
const fontSize = computed(() => {
  const s = String(props.size)
  return s.endsWith('px') || s.endsWith('rpx') ? s : `${s}px`
})

const glyph = computed(() => iconChars[props.name] || '')

const iconStyle = computed(() => ({
  fontSize: fontSize.value,
  color: props.color
}))
</script>

<style scoped>
.q-icon {
  display: inline-block;
  vertical-align: middle;
  flex-shrink: 0;
}
</style>
