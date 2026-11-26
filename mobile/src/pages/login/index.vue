<template>
  <q-page background="#F8F9F8">
    <!-- 顶部返回 -->
    <view class="login-header">
      <view class="nav-back" @tap="goBack">
        <q-icon name="arrow-left" size="34" color="#4A4A4A" />
      </view>
    </view>

    <!-- 品牌徽记 -->
    <view class="brand">
      <view class="crest">
        <view class="crest-inner">
          <q-icon name="tea" size="30" color="#F2D279" />
          <text class="crest-text">本草</text>
        </view>
      </view>
      <text class="brand-name">本草国医馆</text>
    </view>

    <!-- 登录表单 -->
    <view class="form">
      <view class="tabs">
        <view
          :class="['tab', { active: activeTab === 'code' }]"
          @tap="activeTab = 'code'"
        >
          <q-icon name="send" size="17" :color="activeTab === 'code' ? '#1E7A33' : '#9A9A9A'" />
          <text>手机验证码登录</text>
        </view>
        <view
          :class="['tab', { active: activeTab === 'password' }]"
          @tap="activeTab = 'password'"
        >
          <q-icon name="lock-on" size="17" :color="activeTab === 'password' ? '#1E7A33' : '#9A9A9A'" />
          <text>账号密码登录</text>
        </view>
      </view>

      <!-- 手机验证码 -->
      <view v-if="activeTab === 'code'" class="form-fields">
        <view class="field">
          <q-icon name="send" size="20" color="#1E7A33" />
          <text class="prefix-text">+86</text>
          <view class="prefix-divider"></view>
          <input
            v-model="phone"
            class="input"
            type="number"
            maxlength="11"
            placeholder="请输入手机号"
            placeholder-class="input-placeholder"
          />
        </view>
        <view class="field">
          <q-icon name="lock-on" size="20" color="#1E7A33" />
          <input
            v-model="code"
            class="input"
            type="number"
            maxlength="6"
            placeholder="请输入验证码"
            placeholder-class="input-placeholder"
          />
          <view class="code-btn" @tap="sendCode">
            <text>获取验证码</text>
          </view>
        </view>
      </view>

      <!-- 账号密码 -->
      <view v-else class="form-fields">
        <view class="field">
          <q-icon name="send" size="20" color="#1E7A33" />
          <text class="prefix-text">+86</text>
          <view class="prefix-divider"></view>
          <input
            v-model="phone"
            class="input"
            type="number"
            maxlength="11"
            placeholder="请输入手机号"
            placeholder-class="input-placeholder"
          />
        </view>
        <view class="field">
          <q-icon name="lock-on" size="20" color="#1E7A33" />
          <input
            v-model="password"
            class="input"
            :password="!showPassword"
            placeholder="请输入账号密码"
            placeholder-class="input-placeholder"
          />
          <text class="eye" @tap="showPassword = !showPassword">{{ showPassword ? '隐藏' : '显示' }}</text>
        </view>
      </view>

      <view :class="['submit', { loading: submitting }]" @tap="submit">
        <text>{{ submitting ? '登录中…' : '立即登录' }}</text>
        <q-icon name="chevron-right" size="20" color="#ffffff" />
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
        <view class="alt-item" @tap="thirdLogin('微信')">
          <view class="alt-circle">
            <q-icon name="chat" size="22" color="#07C160" />
          </view>
          <text class="alt-label">微信</text>
        </view>
        <view class="alt-item" @tap="thirdLogin('Apple')">
          <view class="alt-circle">
            <q-icon name="user-vip" size="22" color="#1B1C1C" />
          </view>
          <text class="alt-label">Apple</text>
        </view>
      </view>
    </view>

    <!-- 协议 -->
    <view class="agreement">
      <view class="agree-row" @tap="agreed = !agreed">
        <view :class="['checkbox', { checked: agreed }]">
          <q-icon v-if="agreed" name="check" size="11" color="#ffffff" />
        </view>
        <text class="agree-text">
          已阅读并同意<text class="link">《用户服务协议》</text>与<text class="link">《隐私政策》</text>
        </text>
      </view>
    </view>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { login as loginApi } from '@/api'
import { consumeLoginRedirectPath } from '@/utils/http'

type LoginTab = 'code' | 'password'

const activeTab = ref<LoginTab>('code')
const phone = ref('')
const code = ref('')
const password = ref('')
const showPassword = ref(false)
const agreed = ref(true)
const submitting = ref(false)

function toast(title: string) {
  uni.showToast({ title, icon: 'none' })
}

function goBack() {
  uni.navigateBack({
    fail: () => uni.switchTab({ url: '/pages/index/index' })
  })
}

function isValidPhone() {
  return /^1[3-9]\d{9}$/.test(phone.value)
}

function sendCode() {
  if (!isValidPhone()) return toast('请输入正确的手机号')
  // 后端暂无短信验证码接口，避免假接口；切换到可用登录方式
  toast('验证码登录暂未开放，请使用账号密码登录')
  activeTab.value = 'password'
}

async function submit() {
  if (submitting.value) return
  if (!agreed.value) return toast('请先阅读并同意用户协议')
  if (!isValidPhone()) return toast('请输入正确的手机号')

  if (activeTab.value === 'code') {
    if (!code.value) return toast('请输入验证码')
    return sendCode()
  }

  if (password.value.length < 6) return toast('请输入至少 6 位密码')

  submitting.value = true
  try {
    await loginApi({ phone: phone.value, password: password.value })
    uni.showToast({ title: '登录成功', icon: 'success' })
    const redirectPath = consumeLoginRedirectPath()
    setTimeout(() => {
      uni.reLaunch({
        url: redirectPath || '/pages/index/index',
        fail: (error) => {
          console.error('登录成功后跳转目标页面失败', error)
          toast('登录成功，但页面跳转失败')
        }
      })
    }, 300)
  } catch (err) {
    toast(err instanceof Error ? err.message : '登录失败，请稍后重试')
  } finally {
    submitting.value = false
  }
}

function thirdLogin(name: string) {
  toast(`${name} 登录开发中`)
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
