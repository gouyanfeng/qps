<template>
  <q-page>
    <q-page-header-back title="会员登录" />
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
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { login, register } from '@/api'
import { consumeLoginRedirectPath } from '@/utils/http'

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
    const redirectPath = consumeLoginRedirectPath()
    setTimeout(() => {
      uni.reLaunch({
        url: redirectPath || '/pages/index/index',
        fail: (error) => {
          console.error('会员登录成功后跳转目标页面失败', error)
          uni.showToast({ title: '登录成功，但页面跳转失败', icon: 'none' })
        }
      })
    }, 300)
  } catch (error) {
    console.error('会员登录/注册失败', error)
    uni.showToast({ title: error instanceof Error ? error.message : '登录失败，请稍后重试', icon: 'none' })
  } finally {
    submitting.value = false
  }
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
