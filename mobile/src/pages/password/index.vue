<template>
  <q-page>
    <q-page-header-back title="修改密码" />

    <view class="page-body">
      <view class="tips-card">为保障账号安全，请使用至少 6 位的新密码，修改成功后将保持登录状态。</view>

      <view class="form-card">
        <view class="form-row">
          <text class="form-label">原密码</text>
          <q-field
            v-model="form.oldPassword"
            password
            show-password-toggle
            :maxlength="32"
            placeholder="请输入当前密码"
          />
        </view>
        <view class="form-divider"></view>
        <view class="form-row">
          <text class="form-label">新密码</text>
          <q-field
            v-model="form.newPassword"
            password
            show-password-toggle
            :maxlength="32"
            placeholder="至少 6 位"
          />
        </view>
        <view class="form-divider"></view>
        <view class="form-row">
          <text class="form-label">确认密码</text>
          <q-field
            v-model="form.confirmPassword"
            password
            show-password-toggle
            :maxlength="32"
            placeholder="再次输入新密码"
          />
        </view>
      </view>

      <button class="submit-button" :loading="saving" @click="submit">确认修改</button>
    </view>
  </q-page>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { changeMemberPassword } from '@/api'

const saving = ref(false)
const form = reactive({
  oldPassword: '',
  newPassword: '',
  confirmPassword: ''
})

async function submit() {
  if (!form.oldPassword) {
    uni.showToast({ title: '请输入原密码', icon: 'none' })
    return
  }
  if (form.newPassword.length < 6) {
    uni.showToast({ title: '新密码至少 6 位', icon: 'none' })
    return
  }
  if (form.newPassword !== form.confirmPassword) {
    uni.showToast({ title: '两次输入的新密码不一致', icon: 'none' })
    return
  }
  if (saving.value) return

  saving.value = true
  uni.showLoading({ title: '修改中…' })
  try {
    await changeMemberPassword({
      oldPassword: form.oldPassword,
      newPassword: form.newPassword
    })
    uni.showToast({ title: '修改成功', icon: 'success' })
    form.oldPassword = ''
    form.newPassword = ''
    form.confirmPassword = ''
    setTimeout(() => uni.navigateBack(), 600)
  } catch (error) {
    console.error('修改密码失败', error)
    uni.showToast({ title: (error as Error).message || '修改失败', icon: 'none' })
  } finally {
    uni.hideLoading()
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
