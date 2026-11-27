<template>
  <view
    class="q-field"
    :class="{
      'q-field--focused': focused,
      'q-field--with-label': !!label,
      'q-field--shadow': shadow,
      'q-field--disabled': disabled
    }"
    :style="fieldStyle"
  >
    <text v-if="label" class="q-field-label" :style="{ width: labelWidth }">{{ label }}</text>

    <slot name="prefix" />

    <input
      class="q-field-input"
      :value="modelValue"
      :type="inputType"
      :password="masked"
      :placeholder="placeholder"
      placeholder-class="q-field-placeholder"
      :placeholder-style="`font-size:${fontSize};color:#9E9E9E`"
      :confirm-type="confirmType"
      :maxlength="maxlength"
      :disabled="disabled"
      :style="{ textAlign: align }"
      @input="onInput"
      @confirm="$emit('confirm', $event)"
      @focus="onFocus"
      @blur="onBlur"
    />

    <view v-if="clearable && modelValue" class="q-field-clear" @tap.stop="onClear">
      <q-icon name="close" size="12" color="#FFFFFF" />
    </view>

    <text
      v-if="showPasswordToggle"
      class="q-field-eye"
      @tap.stop="revealed = !revealed"
    >{{ revealed ? '隐藏' : '显示' }}</text>

    <slot name="suffix" />
  </view>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'

const props = withDefaults(defineProps<{
  modelValue?: string
  placeholder?: string
  type?: string
  label?: string
  labelWidth?: string
  maxlength?: number
  disabled?: boolean
  clearable?: boolean
  /** 密码类型，自动遮蔽；配合 showPasswordToggle 显示切换按钮 */
  password?: boolean
  showPasswordToggle?: boolean
  confirmType?: string
  align?: 'left' | 'right'
  /** 白底卡片 + 阴影，用于登录/注册页的大输入框 */
  shadow?: boolean
  bg?: string
  radius?: string
  height?: string
  padding?: string
  fontSize?: string
  /** v-model.trim 修饰符，由 Vue 自动注入 */
  modelModifiers?: { trim?: boolean }
}>(), {
  modelValue: '',
  placeholder: '',
  type: 'text',
  label: '',
  labelWidth: '140rpx',
  maxlength: 140,
  disabled: false,
  clearable: false,
  password: false,
  showPasswordToggle: false,
  confirmType: 'done',
  align: 'left',
  shadow: false,
  bg: '#F7F6F2',
  radius: '10rpx',
  height: '76rpx',
  padding: '0 20rpx',
  fontSize: '24rpx',
  modelModifiers: () => ({})
})

const emit = defineEmits<{
  (e: 'update:modelValue', value: string): void
  (e: 'confirm', event: unknown): void
  (e: 'focus', event: unknown): void
  (e: 'blur', event: unknown): void
  (e: 'clear'): void
}>()

const focused = ref(false)
const revealed = ref(false)

// 外部把 type 切回非密码时，重置遮蔽状态
watch(() => props.password, (value) => {
  if (!value) revealed.value = false
})

const masked = computed(() => props.password && !revealed.value)
const inputType = computed(() => (props.password ? 'text' : props.type))

const fieldStyle = computed(() => ({
  background: props.bg,
  borderRadius: props.radius,
  height: props.height,
  padding: props.padding
}))

function onInput(event: any) {
  const raw = event?.detail?.value ?? ''
  emit('update:modelValue', props.modelModifiers?.trim ? raw.trim() : raw)
}

function onFocus(event: unknown) {
  focused.value = true
  emit('focus', event)
}

function onBlur(event: unknown) {
  focused.value = false
  emit('blur', event)
}

function onClear() {
  emit('update:modelValue', '')
  emit('clear')
}
</script>

<style scoped lang="scss">
@import "./q-field.scss";
</style>
