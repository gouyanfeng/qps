<template>
  <view class="page">
    <!-- 顶部返回 -->
    <view class="login-nav">
      <view class="nav-back" @click="goBack">
        <q-icon name="arrow-left" size="20" color="#40493D" />
      </view>
    </view>

    <view class="body">
      <!-- 品牌徽记 -->
      <view class="brand">
        <view class="crest">
          <view class="crest-inner">
            <q-icon name="tea" size="28" color="#FFE082" />
            <text class="crest-text">本草</text>
          </view>
        </view>
        <text class="brand-name">本草国医馆</text>
      </view>

      <!-- 登录表单 -->
      <view class="form">
        <view class="tabs">
          <view :class="['tab', { active: mode === 'sms' }]" @click="switchMode('sms')">
            <q-icon name="tea" size="17" :color="mode === 'sms' ? '#2E7D32' : '#8A8578'" />
            <text>手机验证码登录</text>
          </view>
          <view :class="['tab', { active: mode === 'pwd' }]" @click="switchMode('pwd')">
            <q-icon name="lock-on" size="17" :color="mode === 'pwd' ? '#2E7D32' : '#8A8578'" />
            <text>账号密码登录</text>
          </view>
        </view>

        <view class="field">
          <view class="prefix">
            <text class="prefix-text">+86</text>
          </view>
          <input
            v-model="phone"
            class="input"
            type="number"
            maxlength="11"
            placeholder="请输入手机号"
            placeholder-class="input-placeholder"
          />
        </view>

        <view v-if="mode === 'sms'" class="field">
          <q-icon name="lock-on" size="19" color="#2E7D32" />
          <input
            v-model="code"
            class="input code-input"
            type="number"
            maxlength="6"
            placeholder="请输入验证码"
            placeholder-class="input-placeholder"
          />
          <view :class="['code-btn', { disabled: countdown > 0 }]" @click="sendCode">
            <text>{{ codeBtnText }}</text>
          </view>
        </view>

        <view v-else class="field">
          <q-icon name="lock-on" size="19" color="#2E7D32" />
          <input
            v-model="password"
            class="input"
            :password="!showPassword"
            placeholder="请输入账号密码"
            placeholder-class="input-placeholder"
          />
          <text class="eye" @click="showPassword = !showPassword">{{ showPassword ? '隐藏' : '显示' }}</text>
        </view>

        <view :class="['submit', { loading: submitting }]" @click="submit">
          <text>{{ submitting ? '登录中…' : '立即登录' }}</text>
          <q-icon name="chevron-right" size="18" color="#ffffff" />
        </view>
      </view>

      <!-- 其他登录方式 -->
      <view class="alt">
        <view class="divider">
          <view class="divider-line"></view>
          <text class="divider-text">其他登录方式</text>
          <view class="divider-line right"></view>
        </view>
        <view class="alt-row">
          <view class="alt-item" @click="thirdLogin('微信')">
            <view class="alt-circle">
              <q-icon name="chat" size="20" color="#07C160" />
            </view>
            <text class="alt-label">微信</text>
          </view>
          <view class="alt-item" @click="thirdLogin('Apple')">
            <view class="alt-circle">
              <q-icon name="user-vip" size="20" color="#1B1C1C" />
            </view>
            <text class="alt-label">Apple</text>
          </view>
        </view>
      </view>

      <!-- 协议 -->
      <view class="agreement">
        <view class="agree-row" @click="agreed = !agreed">
          <view :class="['checkbox', { checked: agreed }]">
            <q-icon v-if="agreed" name="check" size="10" color="#ffffff" />
          </view>
          <text class="agree-text">
            已阅读并同意<text class="link">《用户服务协议》</text>与<text class="link">《隐私政策》</text>
          </text>
        </view>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref, computed, onUnmounted } from 'vue'
import { login as loginApi } from '@/api'

type LoginMode = 'sms' | 'pwd'

const mode = ref<LoginMode>('sms')
const phone = ref('')
const code = ref('')
const password = ref('')
const showPassword = ref(false)
const agreed = ref(true)
const countdown = ref(0)
const submitting = ref(false)

let timer: ReturnType<typeof setInterval> | null = null

const codeBtnText = computed(() => (countdown.value > 0 ? `${countdown.value}s 后重发` : '获取验证码'))

function toast(title: string) {
  uni.showToast({ title, icon: 'none' })
}

function switchMode(next: LoginMode) {
  mode.value = next
}

function isValidPhone() {
  return /^1[3-9]\d{9}$/.test(phone.value)
}

function sendCode() {
  if (countdown.value > 0) return
  if (!isValidPhone()) return toast('请输入正确的手机号')

  toast('验证码已发送')
  countdown.value = 60
  timer = setInterval(() => {
    if (countdown.value <= 1) {
      if (timer) clearInterval(timer)
      timer = null
      countdown.value = 0
      return
    }
    countdown.value--
  }, 1000)
}

async function submit() {
  if (submitting.value) return
  if (!agreed.value) return toast('请先阅读并同意用户协议')
  if (!isValidPhone()) return toast('请输入正确的手机号')
  if (mode.value === 'sms') {
    toast('短信登录暂未开通，请使用账号密码登录')
    switchMode('pwd')
    return
  }
  if (password.value.length < 6) return toast('请输入至少 6 位密码')

  submitting.value = true
  try {
    await loginApi({ phone: phone.value, password: password.value })
    uni.showToast({ title: '登录成功', icon: 'success' })
    setTimeout(() => uni.switchTab({ url: '/pages/index/index' }), 600)
  } catch (err) {
    toast(err instanceof Error ? err.message : '登录失败，请稍后重试')
  } finally {
    submitting.value = false
  }
}

function thirdLogin(name: string) {
  toast(`${name} 登录开发中`)
}

function goBack() {
  uni.navigateBack({ fail: () => uni.switchTab({ url: '/pages/index/index' }) })
}

onUnmounted(() => {
  if (timer) clearInterval(timer)
})
</script>

<style lang="scss" scoped>
$primary: #2E7D32;
$primary-dark: #1B5E20;
$gold: #FFE082;
$on-surface: #1B1C1C;
$outline: #8A8578;
$hairline: #E8E3D6;

.page {
  display: flex;
  flex-direction: column;
  min-height: 100vh;
  background: var(--page-bg);
}

.login-nav {
  display: flex;
  align-items: center;
  padding: var(--safe-top) 30rpx 0;
}

.nav-back {
  width: 72rpx;
  height: 72rpx;
  border-radius: 50%;
  background: #ffffff;
  border: 1rpx solid $hairline;
  display: flex;
  align-items: center;
  justify-content: center;
}

.body {
  flex: 1;
  display: flex;
  flex-direction: column;
  padding: 40rpx 32rpx calc(40rpx + env(safe-area-inset-bottom));
}

/* 品牌 */
.brand {
  display: flex;
  flex-direction: column;
  align-items: center;
  padding: 24rpx 0 48rpx;
}
.crest {
  width: 128rpx;
  height: 128rpx;
  border-radius: 28rpx;
  padding: 2rpx;
  background: linear-gradient(135deg, $primary-dark 0%, $primary 55%, #2E7D32 100%);
  box-shadow: 0 12rpx 40rpx -4rpx rgba(46, 125, 50, 0.3);
}
.crest-inner {
  width: 100%;
  height: 100%;
  border-radius: 26rpx;
  border: 1rpx solid rgba(255, 224, 130, 0.6);
  background: rgba(46, 125, 50, 0.2);
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
}
.crest-text {
  margin-top: -2rpx;
  font-size: 20rpx;
  font-weight: 700;
  letter-spacing: 4rpx;
  color: $gold;
}
.brand-name {
  margin-top: 24rpx;
  font-family: 'Noto Serif SC', 'Songti SC', serif;
  font-size: 44rpx;
  font-weight: 700;
  letter-spacing: 4rpx;
  color: $on-surface;
}

/* 表单 */
.tabs {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 48rpx;
  padding-bottom: 24rpx;
  border-bottom: 1rpx solid rgba(232, 227, 214, 0.6);
}
.tab {
  position: relative;
  display: flex;
  align-items: center;
  gap: 8rpx;
  padding-bottom: 20rpx;
  font-size: 30rpx;
  font-weight: 500;
  color: rgba(64, 73, 61, 0.7);

  &.active {
    font-weight: 700;
    color: $primary;

    &::after {
      content: '';
      position: absolute;
      left: 50%;
      bottom: -24rpx;
      transform: translateX(-50%);
      width: 40rpx;
      height: 4rpx;
      border-radius: 4rpx;
      background: $primary;
    }
  }
}

.field {
  display: flex;
  align-items: center;
  gap: 16rpx;
  margin-top: 24rpx;
  padding: 0 30rpx;
  height: 104rpx;
  border-radius: 24rpx;
  background: rgba(255, 255, 255, 0.8);
  border-bottom: 1rpx solid rgba(232, 227, 214, 0.9);
}
.prefix {
  padding-right: 20rpx;
  border-right: 1rpx solid rgba(232, 227, 214, 0.9);
}
.prefix-text {
  font-size: 28rpx;
  font-weight: 600;
  color: $on-surface;
}
.input {
  flex: 1;
  min-width: 0;
  font-size: 30rpx;
  color: $on-surface;
  letter-spacing: 1rpx;
}
.code-input {
  letter-spacing: 4rpx;
}
.input-placeholder {
  color: rgba(138, 133, 120, 0.5);
}
.code-btn {
  flex-shrink: 0;
  padding: 12rpx 28rpx;
  border-radius: 100rpx;
  background: rgba(46, 125, 50, 0.1);
  font-size: 24rpx;
  font-weight: 600;
  color: $primary;

  &.disabled {
    opacity: 0.5;
  }
}
.eye {
  flex-shrink: 0;
  font-size: 24rpx;
  color: $outline;
}

.submit {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 12rpx;
  margin-top: 48rpx;
  height: 100rpx;
  border-radius: 100rpx;
  background: linear-gradient(90deg, #2E7D32 0%, #256B29 55%, $primary-dark 100%);
  box-shadow: 0 12rpx 40rpx -4rpx rgba(46, 125, 50, 0.3);
  font-size: 28rpx;
  font-weight: 600;
  letter-spacing: 2rpx;
  color: #ffffff;

  &.loading {
    opacity: 0.7;
  }
}

/* 其他登录方式 */
.alt {
  margin-top: 64rpx;
}
.divider {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 16rpx;
}
.divider-line {
  width: 80rpx;
  height: 1rpx;
  background: linear-gradient(90deg, rgba(215, 204, 200, 0), #D7CCC8);

  &.right {
    background: linear-gradient(90deg, #D7CCC8, rgba(215, 204, 200, 0));
  }
}
.divider-text {
  font-size: 22rpx;
  letter-spacing: 2rpx;
  color: $outline;
}
.alt-row {
  display: flex;
  justify-content: center;
  gap: 64rpx;
  margin-top: 24rpx;
}
.alt-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 8rpx;
}
.alt-circle {
  width: 88rpx;
  height: 88rpx;
  border-radius: 50%;
  background: #ffffff;
  border: 1rpx solid #E0D7C6;
  display: flex;
  align-items: center;
  justify-content: center;
}
.alt-label {
  font-size: 22rpx;
  color: $outline;
}

/* 协议 */
.agreement {
  margin-top: auto;
  padding-top: 48rpx;
}
.agree-row {
  display: flex;
  align-items: flex-start;
  justify-content: center;
  gap: 12rpx;
}
.checkbox {
  flex-shrink: 0;
  width: 30rpx;
  height: 30rpx;
  margin-top: 2rpx;
  border-radius: 6rpx;
  border: 1rpx solid #C9C4BA;
  box-sizing: border-box;
  display: flex;
  align-items: center;
  justify-content: center;
  background: #ffffff;

  &.checked {
    background: $primary;
    border-color: $primary;
  }
}
.agree-text {
  font-size: 22rpx;
  line-height: 1.5;
  color: $outline;
}
.link {
  color: $primary;
  font-weight: 500;
}
</style>
