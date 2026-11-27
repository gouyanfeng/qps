<template>
  <q-page>
    <q-page-header-back title="编辑个人信息" />

    <view class="page-body">
      <view v-if="loading" class="loading-tip">加载中…</view>

      <template v-else>
        <!-- 头像 -->
        <view class="form-card avatar-card" @click="chooseAvatar">
          <view class="avatar-ring">
            <q-image class="avatar-img" :src="form.avatar || '/static/images/avatar-elder.jpg'" mode="aspectFill" />
          </view>
          <view class="avatar-info">
            <text class="form-label">头像</text>
            <text class="form-sub">点击更换头像</text>
          </view>
          <q-icon name="chevron-right" size="15" color="#6B6B6B" />
        </view>

        <view class="form-card">
          <!-- 昵称 -->
          <view class="form-row">
            <text class="form-label">昵称</text>
            <q-field
              v-model.trim="form.nickname"
              align="right"
              :maxlength="20"
              placeholder="请输入昵称"
            />
          </view>

          <view class="form-divider"></view>

          <!-- 手机号（只读） -->
          <view class="form-row">
            <text class="form-label">手机号</text>
            <text class="form-value-muted">{{ profile?.phone || '-' }}</text>
          </view>

          <view class="form-divider"></view>

          <!-- 性别 -->
          <picker mode="selector" :range="genderOptions" :value="genderIndex" @change="onGenderChange">
            <view class="form-row form-picker">
              <text class="form-label">性别</text>
              <view class="form-picker-right">
                <text :class="form.gender ? 'form-value' : 'form-placeholder'">
                  {{ form.gender || '请选择' }}
                </text>
                <q-icon name="chevron-right" size="15" color="#6B6B6B" />
              </view>
            </view>
          </picker>

          <view class="form-divider"></view>

          <!-- 生日 -->
          <picker mode="date" :value="form.birthday || ''" start="1900-01-01" :end="today" @change="onBirthdayChange">
            <view class="form-row form-picker">
              <text class="form-label">生日</text>
              <view class="form-picker-right">
                <text :class="form.birthday ? 'form-value' : 'form-placeholder'">
                  {{ form.birthday || '请选择' }}
                </text>
                <q-icon name="chevron-right" size="15" color="#6B6B6B" />
              </view>
            </view>
          </picker>
        </view>

        <button class="submit-button" :loading="saving" @click="submit">保存修改</button>
      </template>
    </view>
  </q-page>
</template>

<script setup lang="ts">
import { computed, reactive, ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import {
  getMemberProfile,
  updateMemberProfile,
  uploadAvatarImage
} from '@/api'
import type { MemberProfile } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const genderOptions = ['未设置', '男', '女']
const today = new Date().toISOString().slice(0, 10)

const profile = ref<MemberProfile | null>(null)
const loading = ref(true)
const saving = ref(false)
const uploading = ref(false)
const form = reactive({
  nickname: '',
  avatar: '',
  gender: '',
  birthday: ''
})

const genderIndex = computed(() => Math.max(0, genderOptions.indexOf(form.gender)))

/** 后端生日字段可能是带时间的 ISO 串，picker 只认 yyyy-MM-dd */
function normalizeBirthday(value?: string | null): string {
  return value ? value.slice(0, 10) : ''
}

async function loadProfile() {
  loading.value = true
  try {
    const member = await getMemberProfile()
    profile.value = member
    form.nickname = member.nickname || ''
    form.avatar = member.avatar || ''
    form.gender = member.gender || ''
    form.birthday = normalizeBirthday(member.birthday)
  } catch (error) {
    console.error('加载会员资料失败', error)
    if (error instanceof UnauthorizedError) return
    uni.showToast({ title: '会员信息加载失败', icon: 'none' })
  } finally {
    loading.value = false
  }
}

onShow(loadProfile)

function onGenderChange(e: { detail: { value: string } }) {
  const index = Number(e.detail.value)
  form.gender = index === 0 ? '' : genderOptions[index]
}

function onBirthdayChange(e: { detail: { value: string } }) {
  form.birthday = e.detail.value
}

function chooseAvatar() {
  // #ifdef H5
  // H5 端 chooseImage 直接返回本地临时路径，沿用现有评价/售后图的上传通道
  // #endif
  uni.chooseImage({
    count: 1,
    sizeType: ['compressed'],
    sourceType: ['album', 'camera'],
    success: async (res) => {
      const path = res.tempFilePaths?.[0]
      if (!path) return
      uploading.value = true
      uni.showLoading({ title: '上传中…' })
      try {
        form.avatar = await uploadAvatarImage(path)
      } catch (error) {
        console.error('上传头像失败', error)
        uni.showToast({ title: (error as Error).message || '头像上传失败', icon: 'none' })
      } finally {
        uni.hideLoading()
        uploading.value = false
      }
    }
  })
}

async function submit() {
  if (!form.nickname) {
    uni.showToast({ title: '请填写昵称', icon: 'none' })
    return
  }
  if (uploading.value) {
    uni.showToast({ title: '头像上传中，请稍候', icon: 'none' })
    return
  }
  if (saving.value) return

  saving.value = true
  try {
    const updated = await updateMemberProfile({
      nickname: form.nickname,
      avatar: form.avatar || undefined,
      gender: form.gender || null,
      birthday: form.birthday || null
    })
    profile.value = updated
    uni.showToast({ title: '保存成功', icon: 'success' })
    setTimeout(() => uni.navigateBack(), 600)
  } catch (error) {
    console.error('保存会员资料失败', error)
    uni.showToast({ title: (error as Error).message || '保存失败', icon: 'none' })
  } finally {
    saving.value = false
  }
}
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
