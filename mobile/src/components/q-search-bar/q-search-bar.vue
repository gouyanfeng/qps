<template>
  <view class="q-search-bar" :style="barStyle">
    <q-icon name="search" :size="iconSize" color="#707A6C" class="q-search-bar-icon" />
    <input
      class="q-search-bar-input"
      :value="modelValue"
      type="text"
      :placeholder="placeholder"
      placeholder-class="q-search-bar-placeholder"
      :placeholder-style="`font-size:${fontSize};color:#9E9E9E`"
      :confirm-type="confirmType"
      :maxlength="maxlength"
      :focus="focus"
      :disabled="disabled"
      @input="onInput"
      @confirm="$emit('confirm', $event)"
      @focus="$emit('focus', $event)"
      @blur="$emit('blur', $event)"
    />
    <view v-if="clearable && modelValue" class="q-search-bar-clear" @tap.stop="onClear">
      <q-icon name="close" size="12" color="#FFFFFF" />
    </view>
    <slot name="right" />
  </view>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = withDefaults(defineProps<{
  modelValue?: string
  placeholder?: string
  /** 自动聚焦（商品搜索页进入时拉起键盘） */
  focus?: boolean
  clearable?: boolean
  confirmType?: string
  maxlength?: number
  disabled?: boolean
  /** 搜索框背景色 */
  bg?: string
  /** 圆角，支持 rpx / px */
  radius?: string
  /** 高度，支持 rpx / px */
  height?: string
  /** 输入字号，支持 rpx / px */
  fontSize?: string
  /** 搜索图标大小，数字按 px */
  iconSize?: number | string
}>(), {
  modelValue: '',
  placeholder: '搜索',
  focus: false,
  clearable: true,
  confirmType: 'search',
  maxlength: 50,
  disabled: false,
  bg: '#F6F3F2',
  radius: '36rpx',
  height: '72rpx',
  fontSize: '24rpx',
  iconSize: 16
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'confirm', event: unknown): void
  (e: 'focus', event: unknown): void
  (e: 'blur', event: unknown): void
  (e: 'clear'): void
}>()

const barStyle = computed(() => ({
  background: props.bg,
  borderRadius: props.radius,
  height: props.height
}))

function onInput(event: any) {
  emit('update:modelValue', event?.detail?.value ?? '')
}

function onClear() {
  emit('update:modelValue', '')
  emit('clear')
}
</script>

<style scoped lang="scss">
@import "./q-search-bar.scss";
</style>
