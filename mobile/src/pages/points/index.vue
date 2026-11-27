<template>
  <q-page
    refresh-enabled
    :refresher-triggered="refreshing"
    @refresh="onRefresh"
  >
    <q-page-header-back title="积分明细" />

    <view class="page-body">
      <view class="points-summary">
        <text class="summary-label">当前可用积分</text>
        <text class="summary-value">{{ availablePoints }}</text>
        <text class="summary-tip">积分可用于兑换优惠，详见活动规则</text>
      </view>

      <view class="section-title">积分明细</view>

      <view v-if="loading && !logs.length" class="state-tip">加载中…</view>
      <view v-else-if="!logs.length" class="state-tip">暂无积分变动记录</view>

      <view v-else class="log-list">
        <view v-for="log in logs" :key="log.id" class="log-row">
          <view class="log-main">
            <text class="log-type">{{ log.remark || log.changeType || '积分变动' }}</text>
            <text class="log-time">{{ formatTime(log.createdAt) }}</text>
          </view>
          <view class="log-amount-col">
            <text class="log-amount" :class="log.changeAmount >= 0 ? 'plus' : 'minus'">
              {{ log.changeAmount >= 0 ? '+' : '' }}{{ log.changeAmount }}
            </text>
            <text class="log-balance">余额 {{ log.balance }}</text>
          </view>
        </view>
        <view v-if="logs.length >= limit" class="state-tip">仅展示最近 {{ limit }} 条记录</view>
      </view>
    </view>
  </q-page>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { onShow } from '@dcloudio/uni-app'
import { getMemberPointLogs, getMemberProfile } from '@/api'
import type { MemberPointLog } from '@/api'
import { UnauthorizedError } from '@/utils/http'

const limit = 50
const logs = ref<MemberPointLog[]>([])
const availablePoints = ref(0)
const loading = ref(false)
const refreshing = ref(false)

async function loadData() {
  loading.value = true
  try {
    const [profile, pointLogs] = await Promise.all([
      getMemberProfile(),
      getMemberPointLogs(limit)
    ])
    availablePoints.value = profile.availablePoints ?? 0
    logs.value = pointLogs
  } catch (error) {
    console.error('加载积分明细失败', error)
    if (!(error instanceof UnauthorizedError)) {
      uni.showToast({ title: '积分明细加载失败', icon: 'none' })
    }
  } finally {
    loading.value = false
  }
}

async function onRefresh() {
  refreshing.value = true
  try {
    await loadData()
  } finally {
    refreshing.value = false
  }
}

function formatTime(value: string): string {
  if (!value) return ''
  return value.slice(0, 16).replace('T', ' ')
}

onShow(loadData)
</script>

<style lang="scss" scoped>
@import "./index.scss";
</style>
