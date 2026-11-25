<template>
  <view class="page">
    <!-- 顶部返回 -->
    <q-page-header-back title="登录" fallback="/pages/index/index" />

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
          <view class="tab active">
            <q-icon name="lock-on" size="17" color="#2E7D32" />
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

        <view class="field">
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
import { ref } from 'vue'
import { login as loginApi } from '@/api'
import { consumeLoginRedirectPath } from '@/utils/http'

const phone = ref('')
const password = ref('')
const showPassword = ref(false)
const agreed = ref(true)
const submitting = ref(false)

function toast(title: string) {
  uni.showToast({ title, icon: 'none' })
}

function isValidPhone() {
  return /^1[3-9]\d{9}$/.test(phone.value)
}

async function submit() {
  if (submitting.value) return
  if (!agreed.value) return toast('请先阅读并同意用户协议')
  if (!isValidPhone()) return toast('请输入正确的手机号')
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
