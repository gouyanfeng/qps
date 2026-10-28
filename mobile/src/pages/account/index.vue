<template>
  <view class="page">
    <q-page-header-back title="会员登录" subtitle="MEMBER ACCOUNT" />
    <view class="page-body">
      <view class="form-card">
        <text class="form-title">{{ isRegister ? '创建会员账号' : '欢迎回来' }}</text>
        <input v-model.trim="phone" class="form-input" type="number" maxlength="11" placeholder="手机号" />
        <input v-model="password" class="form-input" password placeholder="密码" />
        <input
          v-if="isRegister"
          v-model.trim="nickname"
          class="form-input"
          maxlength="30"
          placeholder="昵称（选填）"
        />
        <button class="submit-button" :loading="submitting" @click="submit">
          {{ isRegister ? '注册并登录' : '登录' }}
        </button>
        <text class="toggle-mode" @click="isRegister = !isRegister">
          {{ isRegister ? '已有账号？去登录' : '还没有账号？立即注册' }}
        </text>
      </view>
    </view>
  </view>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { login, register } from '@/api'

const phone = ref('')
const password = ref('')
const nickname = ref('')
const isRegister = ref(false)
const submitting = ref(false)

async function submit() {
  if (!/^1\d{10}$/.test(phone.value) || password.value.length < 6) {
    uni.showToast({ title: '请填写有效手机号和至少 6 位密码', icon: 'none' })
    return
  }
  submitting.value = true
  try {
    if (isRegister.value) {
      await register({ phone: phone.value, password: password.value, nickname: nickname.value || undefined })
    } else {
      await login({ phone: phone.value, password: password.value })
    }
    uni.showToast({ title: '登录成功', icon: 'success' })
    setTimeout(() => uni.navigateBack(), 300)
  } catch (error) {
    console.error('会员登录/注册失败', error)
    uni.showToast({ title: error instanceof Error ? error.message : '登录失败，请稍后重试', icon: 'none' })
  } finally {
    submitting.value = false
  }
}
</script>

<style lang="scss" scoped>
.page {
  min-height: 100vh;
  background: var(--page-bg);
}

.page-body {
  padding: 32rpx 24rpx;
}

.form-card {
  display: flex;
  flex-direction: column;
  gap: 20rpx;
  padding: 32rpx 28rpx;
  background: #ffffff;
  border-radius: 20rpx;
}

.form-title {
  margin-bottom: 8rpx;
  color: #1B1C1C;
  font-size: 34rpx;
  font-weight: 700;
}

.form-input {
  height: 88rpx;
  padding: 0 22rpx;
  background: #F7F6F2;
  border-radius: 12rpx;
  font-size: 26rpx;
}

.submit-button {
  margin-top: 12rpx;
  background: #2E7D32;
  color: #ffffff;
  border-radius: 44rpx;
  font-size: 28rpx;
}

.toggle-mode {
  align-self: center;
  color: #2E7D32;
  font-size: 24rpx;
}
</style>
